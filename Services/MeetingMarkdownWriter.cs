using System.Text;
using Scribe.Models;

namespace Scribe.Services;

/// <summary>
/// Renders a <see cref="Transcript"/> as the meeting markdown file — the deliverable.
///
/// The format's rules exist because the file is read by a model in chunks, one section
/// at a time. See plans/llm-native-output.md and its worked example; the non-obvious
/// commitments are restated at the point they are implemented below.
/// </summary>
public static class MeetingMarkdownWriter
{
    public static string Write(Transcript transcript)
    {
        var sb = new StringBuilder();
        var summarized = transcript.Metadata.SummaryStatus == SummaryStatus.Ok;

        WriteFrontmatter(sb, transcript);
        WriteHeader(sb, transcript);

        if (summarized)
        {
            WriteAbstract(sb, transcript);
            WriteDecisions(sb, transcript);
            WriteActionItems(sb, transcript);
            WriteOpenQuestions(sb, transcript);
            WriteKeyPoints(sb, transcript);
            WriteTopics(sb, transcript);
        }

        WriteTranscript(sb, transcript);

        return sb.ToString();
    }

    public static string FileName(Transcript transcript) => $"{SlugLine(transcript)}.md";

    /// <summary>
    /// "2026-07-14-card-activation-onboarding". Derived from the title alone, never from
    /// the fallback display title — that is "&lt;date&gt; meeting", which would put the date in
    /// twice.
    /// </summary>
    private static string SlugLine(Transcript transcript) =>
        $"{Date(transcript)}-{Slug(transcript.Metadata.MeetingTitle ?? "meeting")}";

    // ── Frontmatter ─────────────────────────────────────────────────────────
    // The fields a retrieval layer filters on before it embeds anything, plus the
    // two that tell a consumer how much to trust the rest.

    private static void WriteFrontmatter(StringBuilder sb, Transcript transcript)
    {
        var meta = transcript.Metadata;

        sb.AppendLine("---");
        sb.AppendLine($"date: {Date(transcript)}");
        sb.AppendLine($"title: {Quote(Title(transcript))}");
        sb.AppendLine($"slug: {SlugLine(transcript)}");
        if (!string.IsNullOrWhiteSpace(meta.MeetingPurpose))
            sb.AppendLine($"purpose: {Quote(meta.MeetingPurpose)}");
        sb.AppendLine($"duration: {TranscriptFormatter.FormatTime(meta.DurationSeconds)}");
        if (!string.IsNullOrWhiteSpace(meta.MediaFile))
            sb.AppendLine($"media: {meta.MediaFile}");
        sb.AppendLine($"speakers_identified: {meta.SpeakersIdentified.ToString().ToLowerInvariant()}");
        sb.AppendLine($"summary_status: {meta.SummaryStatus.ToString().ToLowerInvariant()}");

        sb.AppendLine("participants:");
        foreach (var (_, speaker) in meta.Speakers.OrderBy(s => s.Key))
        {
            sb.AppendLine($"  - name: {Quote(speaker.Name)}");
            sb.AppendLine($"    role: {Quote(speaker.Role ?? "unknown")}");
            if (speaker.FlaggedMultipleVoices)
                sb.AppendLine("    flagged: may contain more than one voice");
        }

        if (transcript.Topics.Count > 0)
        {
            sb.AppendLine("topics:");
            foreach (var topic in transcript.Topics)
                sb.AppendLine($"  - {Quote(topic.Title)}");
        }

        sb.AppendLine("---");
        sb.AppendLine();
    }

    private static void WriteHeader(StringBuilder sb, Transcript transcript)
    {
        var meta = transcript.Metadata;

        sb.AppendLine($"# {Title(transcript)}");
        sb.AppendLine();

        var context = new StringBuilder(
            $"Meeting on {Date(transcript)}, {meta.Speakers.Count} participants, " +
            $"{TranscriptFormatter.FormatTime(meta.DurationSeconds)}");
        if (!string.IsNullOrWhiteSpace(meta.MediaFile))
            context.Append($", recording `{meta.MediaFile}`");
        context.Append('.');
        sb.AppendLine(context.ToString());
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(meta.MeetingPurpose))
        {
            sb.AppendLine(meta.MeetingPurpose);
            sb.AppendLine();
        }

        // Rule 7: say plainly whether a human vouched for these names, because every
        // named speaker is otherwise read as an established fact.
        sb.AppendLine(IdentificationNote(transcript));
        sb.AppendLine();

        // Rule 4's exception: "never produced" and "empty" must not be confusable.
        if (meta.SummaryStatus != SummaryStatus.Ok)
        {
            sb.AppendLine("**No summary was produced for this meeting.** The summarizer was " +
                          "unreachable when this file was written, so it contains the transcript " +
                          "only. The absence of decisions, action items and open questions is not " +
                          "a finding: nothing looked for them.");
            sb.AppendLine();
        }
    }

    private static string IdentificationNote(Transcript transcript)
    {
        var meta = transcript.Metadata;
        var flagged = meta.Speakers.Values.Count(s => s.FlaggedMultipleVoices);

        var note = meta.SpeakersIdentified switch
        {
            SpeakerIdentification.All =>
                "Speakers were identified by a human.",
            SpeakerIdentification.Partial =>
                $"{meta.Speakers.Values.Count(s => s.Identified)} of {meta.Speakers.Count} speakers " +
                "were identified by a human. The rest are diarization output — labels, not people, " +
                "and no identity is claimed for them.",
            _ =>
                "**Speakers were not identified.** The labels below are diarization output: they " +
                "distinguish voices but name nobody, and no claim is made about who they are."
        };

        if (flagged > 0)
            note += $" {flagged} label(s) were flagged as possibly holding more than one voice.";

        return note;
    }

    // ── AI-derived sections ─────────────────────────────────────────────────

    private static void WriteAbstract(StringBuilder sb, Transcript transcript)
    {
        Section(sb, "Abstract", transcript);
        var roles = new RoleAnnotator(transcript);
        sb.AppendLine(string.IsNullOrWhiteSpace(transcript.Summary.Abstract)
            ? "No abstract was produced for this meeting."
            : roles.Annotate(transcript.Summary.Abstract));
        sb.AppendLine();
    }

    private static void WriteDecisions(StringBuilder sb, Transcript transcript)
    {
        Section(sb, "Decisions", transcript);

        var decisions = transcript.Summary.Decisions;
        if (decisions.Count == 0)
        {
            // Rule 4: an omitted section is indistinguishable from one that fell
            // outside the retrieved chunk, and a model will guess.
            sb.AppendLine("No decisions were taken in this meeting.");
            sb.AppendLine();
            return;
        }

        var roles = new RoleAnnotator(transcript);
        foreach (var (decision, id) in WithUniqueIds(decisions))
        {
            sb.AppendLine($"**{id} — {roles.Annotate(decision.Decision)}** {Cite(decision.TurnIds)}");
            if (!string.IsNullOrWhiteSpace(decision.Rationale))
                sb.AppendLine(roles.Annotate(decision.Rationale));
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Decision IDs derive from the first cited turn, so they survive re-summarization.
    /// Two decisions can cite the same first turn; only here is the whole list visible,
    /// so this is where collisions are broken.
    /// </summary>
    private static IEnumerable<(SummaryDecision Decision, string Id)> WithUniqueIds(List<SummaryDecision> decisions)
    {
        var seen = new Dictionary<string, int>();

        foreach (var decision in decisions)
        {
            var baseId = decision.BaseId;
            var count = seen.TryGetValue(baseId, out var n) ? n : 0;
            seen[baseId] = count + 1;

            yield return (decision, count == 0 ? baseId : $"{baseId}{(char)('a' + count)}");
        }
    }

    private static void WriteActionItems(StringBuilder sb, Transcript transcript)
    {
        Section(sb, "Action items", transcript);

        var items = transcript.Summary.ActionItems;
        if (items.Count == 0)
        {
            sb.AppendLine("No action items were assigned in this meeting.");
            sb.AppendLine();
            return;
        }

        var roles = new RoleAnnotator(transcript);
        foreach (var item in items)
        {
            var assignee = string.IsNullOrWhiteSpace(item.AssignedTo)
                ? "unassigned"
                : roles.Annotate(item.AssignedTo);
            sb.AppendLine($"- **{roles.Annotate(item.Item)}** — {assignee} {Cite(item.TurnIds)}");
        }

        sb.AppendLine();
    }

    private static void WriteOpenQuestions(StringBuilder sb, Transcript transcript)
    {
        Section(sb, "Open questions", transcript);

        var questions = transcript.Summary.OpenQuestions;
        if (questions.Count == 0)
        {
            sb.AppendLine("No questions were left open in this meeting.");
            sb.AppendLine();
            return;
        }

        var roles = new RoleAnnotator(transcript);
        foreach (var question in questions)
            sb.AppendLine($"- {roles.Annotate(question.Question)} {Cite(question.TurnIds)}");

        sb.AppendLine();
    }

    private static void WriteKeyPoints(StringBuilder sb, Transcript transcript)
    {
        Section(sb, "Key points", transcript);

        var points = transcript.Summary.KeyPoints ?? [];
        if (points.Count == 0)
        {
            sb.AppendLine("No key points were recorded for this meeting.");
            sb.AppendLine();
            return;
        }

        var roles = new RoleAnnotator(transcript);
        foreach (var point in points)
            sb.AppendLine($"- {roles.Annotate(point.Point)} {Cite(point.TurnIds)}");

        sb.AppendLine();
    }

    private static void WriteTopics(StringBuilder sb, Transcript transcript)
    {
        Section(sb, "Topics", transcript);

        if (transcript.Topics.Count == 0)
        {
            sb.AppendLine("No topic segmentation was produced for this meeting.");
            sb.AppendLine();
            return;
        }

        foreach (var topic in transcript.Topics)
        {
            var roles = new RoleAnnotator(transcript);
            sb.AppendLine($"### {TopicHeading(topic)}");
            sb.AppendLine();
            sb.AppendLine(CompactStamp(transcript));
            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(topic.Summary))
            {
                sb.AppendLine(roles.Annotate(topic.Summary));
                sb.AppendLine();
            }
        }
    }

    // ── Transcript ──────────────────────────────────────────────────────────

    private static void WriteTranscript(StringBuilder sb, Transcript transcript)
    {
        sb.AppendLine("## Transcript");
        sb.AppendLine();
        sb.AppendLine($"*({MeetingStamp(transcript)} Complete and verbatim. Short acknowledgements " +
                      "are folded onto the end of the turn they interrupt, keeping their own ID and " +
                      "speaker, so every ID is accounted for as either a turn or a fold.)*");
        sb.AppendLine();

        if (transcript.Topics.Count == 0)
        {
            // Chunks still need their context; without segmentation there are no
            // subsections to carry it, so the turns follow the section stamp directly.
            var roles = new RoleAnnotator(transcript);
            foreach (var turn in transcript.Turns)
                WriteTurn(sb, turn, roles);
            return;
        }

        // Topic headings are repeated here so transcript chunks inherit topic context
        // rather than arriving as an unlabelled wall of turns.
        foreach (var topic in transcript.Topics)
        {
            sb.AppendLine($"### {TopicHeading(topic)}");
            sb.AppendLine();
            sb.AppendLine(CompactStamp(transcript));
            sb.AppendLine();

            var roles = new RoleAnnotator(transcript);
            foreach (var turn in TurnsOf(transcript, topic))
                WriteTurn(sb, turn, roles);
        }
    }

    private static IEnumerable<TranscriptTurn> TurnsOf(Transcript transcript, TranscriptTopic topic)
    {
        if (string.IsNullOrEmpty(topic.StartTurnId))
            return transcript.Turns;

        return transcript.Turns.Where(t =>
            string.CompareOrdinal(t.Id, topic.StartTurnId) >= 0 &&
            (string.IsNullOrEmpty(topic.EndTurnId) || string.CompareOrdinal(t.Id, topic.EndTurnId) <= 0));
    }

    private static void WriteTurn(StringBuilder sb, TranscriptTurn turn, RoleAnnotator roles)
    {
        var line = new StringBuilder($"[{turn.Id} {turn.StartTime}] {roles.Annotate(turn.SpeakerName)}: {turn.Text}");

        foreach (var fold in turn.FoldedBackchannels)
            line.Append($" [{fold.Id} folded: {fold.SpeakerName}: {fold.Text}]");

        sb.AppendLine(line.ToString());
        sb.AppendLine();
    }

    // ── Stamps ──────────────────────────────────────────────────────────────

    private static void Section(StringBuilder sb, string heading, Transcript transcript)
    {
        sb.AppendLine($"## {heading}");
        sb.AppendLine();
        sb.AppendLine($"*({MeetingStamp(transcript)})*");
        sb.AppendLine();
    }

    /// <summary>
    /// Rule 1. Repetitive on purpose, and only for a reader who has the whole file:
    /// a chunk saying "we decided to defer it" and nothing else is confidently misleading.
    /// </summary>
    private static string MeetingStamp(Transcript transcript) =>
        $"{Title(transcript)}, {Date(transcript)}, {transcript.Metadata.Speakers.Count} participants.";

    private static string CompactStamp(Transcript transcript) =>
        $"*({Title(transcript)}, {Date(transcript)}.)*";

    private static string TopicHeading(TranscriptTopic topic) =>
        string.IsNullOrEmpty(topic.EndTime)
            ? $"{topic.StartTime} — {topic.Title}"
            : $"{topic.StartTime}–{topic.EndTime} — {topic.Title}";

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static string Cite(List<string> turnIds) =>
        turnIds.Count == 0 ? string.Empty : $"[{string.Join(", ", turnIds)}]";

    private static string Title(Transcript transcript) =>
        string.IsNullOrWhiteSpace(transcript.Metadata.MeetingTitle)
            ? $"{Date(transcript)} meeting"
            : transcript.Metadata.MeetingTitle;

    private static string Date(Transcript transcript) =>
        string.IsNullOrWhiteSpace(transcript.Metadata.RecordingDate)
            ? "undated"
            : transcript.Metadata.RecordingDate;

    private static string Quote(string value) =>
        value.Contains(':') || value.Contains('#') ? $"\"{value.Replace("\"", "'")}\"" : value;

    public static string Slug(string title)
    {
        var sb = new StringBuilder();
        foreach (var c in title.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (c is ' ' or '-' or '_') sb.Append('-');
        }

        var slug = sb.ToString().Trim('-');
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        if (slug.Length > 80) slug = slug[..80].TrimEnd('-');
        return string.IsNullOrEmpty(slug) ? "meeting" : slug;
    }

    /// <summary>
    /// Gives each person their role the first time they appear in a section, and only
    /// then. The role has to ride with the claim rather than sit in a header, because
    /// the chunk that carries "Marcus Webb committed to X" may not carry the header —
    /// and "the product manager committed" is the retrievable version of that claim.
    /// Done here rather than asked of the summarizer: the writer knows the roles, and
    /// this cannot be got wrong by a model having an off day.
    /// </summary>
    private sealed class RoleAnnotator(Transcript transcript)
    {
        private readonly HashSet<string> _annotated = [];

        private readonly List<Speaker> _speakers = transcript.Metadata.Speakers.Values
            .Where(s => !string.IsNullOrWhiteSpace(s.Role) && !string.IsNullOrWhiteSpace(s.Name))
            .OrderByDescending(s => s.Name.Length)   // "Dana Okafor Jr" before "Dana Okafor"
            .ToList();

        public string Annotate(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            foreach (var speaker in _speakers)
            {
                if (_annotated.Contains(speaker.Name)) continue;

                var at = text.IndexOf(speaker.Name, StringComparison.Ordinal);
                if (at < 0) continue;

                // Don't double-annotate text that already names the role.
                var after = at + speaker.Name.Length;
                if (after < text.Length && text[after] == ' ' &&
                    text[after..].StartsWith($" ({speaker.Role})", StringComparison.Ordinal))
                {
                    _annotated.Add(speaker.Name);
                    continue;
                }

                text = text[..after] + $" ({speaker.Role})" + text[after..];
                _annotated.Add(speaker.Name);
            }

            return text;
        }
    }
}
