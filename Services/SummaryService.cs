using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Scribe.Models;
using Scribe.Models.Configuration;
using Scribe.Utils;
using Serilog;

namespace Scribe.Services;

public class SummaryService
{
    private const int MaxOutputTokens = 16000;

    private readonly IChatClient _chatClient;
    private readonly ChatResponseFormat _responseFormat;

    public SummaryService(CompletionSettings settings)
        : this(ChatClientFactory.Create(settings))
    {
    }

    public SummaryService(CompletionClient completion)
        : this(completion.Client, completion.ResponseFormat)
    {
    }

    public SummaryService(IChatClient chatClient, ChatResponseFormat? responseFormat = null)
    {
        _chatClient = chatClient;
        _responseFormat = responseFormat ?? ChatResponseFormat.Json;
    }

    public async Task<TranscriptSummary> GenerateSummaryAsync(Transcript transcript)
    {
        Log.Information("Generating grounded summary for transcript with {TurnCount} turns", transcript.Turns.Count);

        var prompt = BuildSummaryPrompt(transcript);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, GetSystemPrompt()),
            new(ChatRole.User, prompt)
        };

        var chatOptions = new ChatOptions
        {
            // Left at the model default: o4-mini accepts only temperature 1. The ceiling is
            // high because reasoning models spend tokens internally before emitting output.
            MaxOutputTokens = MaxOutputTokens,
            ResponseFormat = _responseFormat
        };

        try
        {
            var response = await _chatClient.GetResponseAsync(messages, chatOptions);

            Log.Debug("Response received with {MessageCount} messages", response.Messages.Count);

            // A truncated response deserializes as a JsonException far from the cause.
            // Reasoning models spend from this same allowance, so the ceiling is reached
            // sooner than the visible output length suggests.
            if (response.FinishReason == ChatFinishReason.Length)
            {
                throw new InvalidOperationException(
                    $"The model hit the {MaxOutputTokens}-token output limit before finishing the summary. " +
                    "If it is a reasoning model, reasoning tokens come out of the same allowance — " +
                    "run the server with reasoning off, or raise MaxOutputTokens.");
            }

            var assistantMessage = response.Messages.LastOrDefault();
            if (assistantMessage == null)
            {
                throw new InvalidOperationException("No messages in response");
            }

            var content = assistantMessage.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(content))
            {
                Log.Error("Empty response content received. Full response: {Response}", JsonSerializer.Serialize(response));
                throw new InvalidOperationException("Empty response content from AI");
            }

            Log.Debug("Received summary response ({Length} chars): {Response}", content.Length, content);

            var summary = JsonSerializer.Deserialize<TranscriptSummary>(Unwrap(content), Json.CaseInsensitive);

            if (summary == null)
            {
                throw new InvalidOperationException("Failed to deserialize summary response");
            }

            // Clean up any turn markers that slipped through (safety net)
            CleanupTurnMarkers(summary);

            DropUnresolvableCitations(summary, transcript);

            Log.Information("Grounded summary: {KeyPoints} key points, {Decisions} decisions, " +
                            "{Actions} action items, {Questions} open questions",
                summary.KeyPoints?.Count ?? 0, summary.Decisions.Count,
                summary.ActionItems.Count, summary.OpenQuestions.Count);

            return summary;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating summary");
            throw;
        }
    }

    private static string GetSystemPrompt() =>
        """
        You are an expert meeting summarizer. Your output is read by a retrieval system one
        section at a time, not by a person reading the document top to bottom.

        CRITICAL REQUIREMENTS:
        1. Every statement MUST be grounded in the transcript. Do not infer or add anything
           that is not stated in it.
        2. Cite turns by their ID exactly as it appears in the transcript (e.g. "T017"), in the
           turnIds arrays. Never invent an ID, and never cite one that is not in the transcript.
        3. Put citations ONLY in the turnIds arrays, never in the prose text.
        4. Name people explicitly in every section. Do not write "as mentioned above", "the
           former", or a bare "he/she/they/it" referring to something in another section. Each
           section is read in isolation, so a cross-section reference resolves to nothing.
        5. Return an empty array for decisions, actionItems or openQuestions if the meeting had
           none. Never omit the field.
        6. A decision is something the meeting SETTLED. An open question was raised and left
           unresolved. Something discussed but neither settled nor left explicitly open is a key
           point, not a decision.
        7. Refer to speakers exactly as the transcript names them. If a speaker is called
           "Speaker 3" or "Unidentified speaker", use that — do not guess at who they are.

        OUTPUT FORMAT — a JSON object:
        {
          "oneLiner": "A single sentence (max 20 words) summarizing the entire meeting",
          "abstract": "One dense paragraph: what happened and what came of it",
          "decisions": [
            { "decision": "What was settled", "rationale": "Why, if stated; else null", "turnIds": ["T017"] }
          ],
          "actionItems": [
            { "item": "What will be done", "turnIds": ["T017"], "assignedTo": "Person if named, else null" }
          ],
          "openQuestions": [
            { "question": "What was left unresolved", "turnIds": ["T019"] }
          ],
          "keyPoints": [
            { "point": "A substantive point discussed", "turnIds": ["T001", "T004"] }
          ]
        }

        Be precise and factual. Turn references belong ONLY in turnIds arrays.
        """;

    // Last resort for a server that accepts response_format and does not honour it.
    // The fix for that is a schema (see ChatClientFactory); this only keeps a
    // non-conforming server from costing a full generation, and says so loudly.
    private static string Unwrap(string content)
    {
        var trimmed = content.TrimStart();
        if (!trimmed.StartsWith('`')) return content;

        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        if (start < 0 || end <= start) return content;

        Log.Warning("Model fenced its JSON despite a constrained response format — " +
                    "the endpoint is not enforcing it. Unwrapping; see bugs/local-model-json-fence.md");
        return content[start..(end + 1)];
    }

    /// <summary>
    /// Removes cited turn IDs that do not exist in this transcript. A hallucinated
    /// citation is worse than a missing one: it looks resolvable and silently is not.
    /// Dropping the citation keeps the claim, which is still grounded by the ones
    /// that resolved; a claim left with no citations at all is logged.
    /// </summary>
    private static void DropUnresolvableCitations(TranscriptSummary summary, Transcript transcript)
    {
        var known = transcript.Turns.Select(t => t.Id).ToHashSet();
        var dropped = 0;

        void Clean(List<string> ids, string claim)
        {
            var removed = ids.RemoveAll(id => !known.Contains(id));
            if (removed == 0) return;

            dropped += removed;
            if (ids.Count == 0)
                Log.Warning("Summary claim has no resolvable citations after cleanup: {Claim}", claim);
        }

        foreach (var keyPoint in summary.KeyPoints ?? []) Clean(keyPoint.TurnIds, keyPoint.Point);
        foreach (var item in summary.ActionItems) Clean(item.TurnIds, item.Item);
        foreach (var decision in summary.Decisions) Clean(decision.TurnIds, decision.Decision);
        foreach (var question in summary.OpenQuestions) Clean(question.TurnIds, question.Question);

        if (dropped > 0)
            Log.Warning("Dropped {Count} citation(s) referring to turns not in the transcript", dropped);
    }

    /// <summary>
    /// Strips turn references out of the PROSE. Citations are wanted — in the turnIds
    /// arrays, which the writer renders as bracketed IDs. A model that also writes
    /// "(turn 5)" or "[T017]" into the text produces a doubled, and often wrong,
    /// citation next to the correct one.
    /// </summary>
    private static void CleanupTurnMarkers(TranscriptSummary summary)
    {
        var patterns = new[]
        {
            @"\s*\(turns?\s+[\dT\s,–-]+\)",   // (turn 5), (turns T1-T4)
            @"\s*\[turns?\s+[\dT\s,–-]+\]",   // [turn 5]
            @"\s*\[T\d+(\s*,\s*T\d+)*\]",    // [T017] or [T017, T019]
            @"\s*\bturns?\s+\d+\b"             // turn 5
        };

        foreach (var pattern in patterns)
        {
            var regex = new System.Text.RegularExpressions.Regex(pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (!string.IsNullOrEmpty(summary.Abstract))
                summary.Abstract = regex.Replace(summary.Abstract, "").Trim();

            foreach (var keyPoint in summary.KeyPoints ?? [])
                keyPoint.Point = regex.Replace(keyPoint.Point, "").Trim();

            foreach (var actionItem in summary.ActionItems)
                actionItem.Item = regex.Replace(actionItem.Item, "").Trim();

            foreach (var decision in summary.Decisions)
            {
                decision.Decision = regex.Replace(decision.Decision, "").Trim();
                if (!string.IsNullOrEmpty(decision.Rationale))
                    decision.Rationale = regex.Replace(decision.Rationale, "").Trim();
            }

            foreach (var question in summary.OpenQuestions)
                question.Question = regex.Replace(question.Question, "").Trim();
        }
    }

    private static string DescribeSpeaker(Speaker speaker) =>
        string.IsNullOrWhiteSpace(speaker.Role) ? speaker.Name : $"{speaker.Name} ({speaker.Role})";

    private string BuildSummaryPrompt(Transcript transcript)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Please analyze the following meeting transcript and generate a grounded summary.");
        sb.AppendLine();
        sb.AppendLine("MEETING METADATA:");
        sb.AppendLine($"Duration: {transcript.Metadata.DurationSeconds:F1} seconds");
        sb.AppendLine($"Speakers: {string.Join(", ", transcript.Metadata.Speakers.Values.Select(DescribeSpeaker))}");
        if (!string.IsNullOrWhiteSpace(transcript.Metadata.MeetingTitle))
        {
            sb.AppendLine($"Title: {transcript.Metadata.MeetingTitle}");
        }
        if (!string.IsNullOrWhiteSpace(transcript.Metadata.MeetingPurpose))
        {
            sb.AppendLine($"Purpose: {transcript.Metadata.MeetingPurpose}");
        }
        sb.AppendLine();
        sb.AppendLine("TRANSCRIPT:");
        sb.AppendLine();

        for (int i = 0; i < transcript.Turns.Count; i++)
        {
            var turn = transcript.Turns[i];
            sb.AppendLine($"[{turn.Id}] {turn.SpeakerName} ({turn.StartTime}-{turn.EndTime}):");
            sb.AppendLine(turn.Text);
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
