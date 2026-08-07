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

            Log.Information("Successfully generated grounded summary with {KeyPointCount} key points and {ActionItemCount} action items",
                summary.KeyPoints?.Count ?? 0, summary.ActionItems.Count);

            return summary;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating summary");
            throw;
        }
    }

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

    private string GetSystemPrompt()
    {
        return @"You are an expert meeting summarizer. Your task is to analyze meeting transcripts and create comprehensive, grounded summaries.

CRITICAL REQUIREMENTS:
1. Every statement in your summary MUST be grounded in the actual transcript
2. For each key point and action item, you MUST provide the turn indices (0-based) where that information appears
3. Multiple turn indices should be provided when information spans multiple turns
4. Turn indices must be accurate - they are the only way a reader can resolve a claim back to what was said
5. Do not infer or add information that is not explicitly stated in the transcript
6. DO NOT include turn references in the text itself (e.g., ""(turns 5-10)"", ""(turn 23)""). Use ONLY the turnIndices array for grounding.

OUTPUT FORMAT:
You must return a JSON object with the following structure:
{
  ""oneLiner"": ""A single sentence (max 20 words) summarizing the entire meeting"",
  ""overview"": ""A 2-3 paragraph overview of the meeting discussion"",
  ""keyPoints"": [
    {
      ""point"": ""A specific key point or topic discussed (NO turn markers in text)"",
      ""turnIndices"": [0, 5, 12]
    }
  ],
  ""actionItems"": [
    {
      ""item"": ""A specific action item or next step (NO turn markers in text)"",
      ""turnIndices"": [23, 24],
      ""assignedTo"": ""Person's name if mentioned, otherwise null""
    }
  ]
}

Be precise, factual, and always ground your summary in the actual transcript content. Remember: turn references belong ONLY in the turnIndices arrays, never in the text fields.";
    }

    private static void CleanupTurnMarkers(TranscriptSummary summary)
    {
        // Regex patterns to match common turn marker formats:
        // (turns 5-10), (turn 23), (turns 1, 2, 5), etc.
        var patterns = new[]
        {
            @"\s*\(turns?\s+[\d\s,–-]+\)",  // (turn 5) or (turns 5-10, 12-15)
            @"\s*\[turns?\s+[\d\s,–-]+\]",  // [turn 5] or [turns 5-10]
            @"\s*\bturn\s+\d+\b",            // turn 5 (without parens)
            @"\s*\bturns\s+[\d\s,–-]+\b"    // turns 5-10 (without parens)
        };

        foreach (var pattern in patterns)
        {
            var regex = new System.Text.RegularExpressions.Regex(pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Clean overview
            if (!string.IsNullOrEmpty(summary.Overview))
            {
                summary.Overview = regex.Replace(summary.Overview, "").Trim();
            }

            // Clean key points
            if (summary.KeyPoints != null)
            {
                foreach (var keyPoint in summary.KeyPoints)
                {
                    keyPoint.Point = regex.Replace(keyPoint.Point, "").Trim();
                }
            }

            // Clean action items
            if (summary.ActionItems != null)
            {
                foreach (var actionItem in summary.ActionItems)
                {
                    actionItem.Item = regex.Replace(actionItem.Item, "").Trim();
                }
            }
        }
    }

    private string BuildSummaryPrompt(Transcript transcript)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Please analyze the following meeting transcript and generate a grounded summary.");
        sb.AppendLine();
        sb.AppendLine("MEETING METADATA:");
        sb.AppendLine($"Duration: {transcript.Metadata.DurationSeconds:F1} seconds");
        sb.AppendLine($"Speakers: {string.Join(", ", transcript.Metadata.Speakers.Values)}");
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
            sb.AppendLine($"[Turn {i}] {turn.SpeakerName} ({turn.StartTime}-{turn.EndTime}):");
            sb.AppendLine(turn.Text);
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
