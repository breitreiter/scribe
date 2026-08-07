using System.Text.Json.Serialization;

namespace Scribe.Models;

/// <summary>
/// Complete transcript with metadata, summary, topics, and turns
/// </summary>
public class Transcript
{
    [JsonPropertyName("metadata")]
    public TranscriptMetadata Metadata { get; set; } = new();

    [JsonPropertyName("summary")]
    public TranscriptSummary Summary { get; set; } = new();

    [JsonPropertyName("topics")]
    public List<TranscriptTopic> Topics { get; set; } = new();

    [JsonPropertyName("turns")]
    public List<TranscriptTurn> Turns { get; set; } = new();
}

/// <summary>How much of the speaker identification a human actually completed.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SpeakerIdentification>))]
public enum SpeakerIdentification
{
    // Serialized lowercase; see Json.Indented's naming policy.
    None,
    Partial,
    All
}

/// <summary>
/// Whether the AI pass ran. "unavailable" is not "empty": a file whose summarizer
/// never ran must not be readable as a meeting where nothing was decided.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SummaryStatus>))]
public enum SummaryStatus
{
    Ok,
    Unavailable
}

/// <summary>
/// Metadata about the meeting/recording
/// </summary>
public class TranscriptMetadata
{
    [JsonPropertyName("durationSeconds")]
    public double DurationSeconds { get; set; }

    [JsonPropertyName("recordingDate")]
    public string RecordingDate { get; set; } = string.Empty;

    [JsonPropertyName("speakerCount")]
    public int SpeakerCount { get; set; }

    [JsonPropertyName("speakers")]
    public Dictionary<int, Speaker> Speakers { get; set; } = new();

    [JsonPropertyName("speakersIdentified")]
    public SpeakerIdentification SpeakersIdentified { get; set; } = SpeakerIdentification.None;

    [JsonPropertyName("summaryStatus")]
    public SummaryStatus SummaryStatus { get; set; } = SummaryStatus.Unavailable;

    [JsonPropertyName("meetingTitle")]
    public string? MeetingTitle { get; set; }

    [JsonPropertyName("meetingPurpose")]
    public string? MeetingPurpose { get; set; }

    /// <summary>
    /// True once a human confirmed date, title and purpose. Keeps a reprocess from
    /// re-asking, and marks the difference between a guessed date and a known one.
    /// </summary>
    [JsonPropertyName("identityConfirmed")]
    public bool IdentityConfirmed { get; set; }

    /// <summary>Filename of the recording, so a timestamp in the output stays resolvable.</summary>
    [JsonPropertyName("mediaFile")]
    public string? MediaFile { get; set; }
}

/// <summary>
/// One diarized voice. A name is only ever set by a human — see design rule 7.
/// </summary>
public class Speaker
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    /// <summary>False while <see cref="Name"/> is still a neutral "Speaker N" label.</summary>
    [JsonPropertyName("identified")]
    public bool Identified { get; set; }

    /// <summary>The human judged that diarization put more than one person under this label.</summary>
    [JsonPropertyName("flaggedMultipleVoices")]
    public bool FlaggedMultipleVoices { get; set; }
}

/// <summary>
/// AI-generated summary with grounding to transcript turns
/// </summary>
public class TranscriptSummary
{
    [JsonPropertyName("oneLiner")]
    public string? OneLiner { get; set; }

    /// <summary>
    /// One dense paragraph. Named "abstract" rather than "overview" deliberately:
    /// an overview invites two or three loose paragraphs.
    /// </summary>
    [JsonPropertyName("abstract")]
    public string? Abstract { get; set; }

    [JsonPropertyName("decisions")]
    public List<SummaryDecision> Decisions { get; set; } = new();

    [JsonPropertyName("actionItems")]
    public List<SummaryActionItem> ActionItems { get; set; } = new();

    [JsonPropertyName("openQuestions")]
    public List<SummaryOpenQuestion> OpenQuestions { get; set; } = new();

    [JsonPropertyName("keyPoints")]
    public List<SummaryKeyPoint>? KeyPoints { get; set; }
}

/// <summary>
/// Something the meeting settled. Identity derives from the evidence rather than
/// from position: ordinal IDs renumber silently when a meeting is re-summarized,
/// which now happens on every speaker rename.
/// </summary>
public class SummaryDecision
{
    [JsonPropertyName("decision")]
    public string Decision { get; set; } = string.Empty;

    [JsonPropertyName("rationale")]
    public string? Rationale { get; set; }

    [JsonPropertyName("turnIds")]
    public List<string> TurnIds { get; set; } = new();

    /// <summary>
    /// "D-T017". Not unique on its own — two decisions can cite the same first turn;
    /// the writer suffixes collisions, since only it sees the whole list.
    /// </summary>
    [JsonIgnore]
    public string BaseId => TurnIds.Count > 0 ? $"D-{TurnIds[0]}" : "D-unknown";
}

public class SummaryOpenQuestion
{
    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    [JsonPropertyName("turnIds")]
    public List<string> TurnIds { get; set; } = new();
}

/// <summary>
/// A key point from the summary, grounded in specific transcript turns
/// </summary>
public class SummaryKeyPoint
{
    [JsonPropertyName("point")]
    public string Point { get; set; } = string.Empty;

    [JsonPropertyName("turnIds")]
    public List<string> TurnIds { get; set; } = new();
}

/// <summary>
/// An action item from the summary, grounded in specific transcript turns
/// </summary>
public class SummaryActionItem
{
    [JsonPropertyName("item")]
    public string Item { get; set; } = string.Empty;

    [JsonPropertyName("turnIds")]
    public List<string> TurnIds { get; set; } = new();

    [JsonPropertyName("assignedTo")]
    public string? AssignedTo { get; set; }
}

/// <summary>
/// A topic or section within the transcript. Topic boundaries are where chunk
/// boundaries end up falling, so they are load-bearing rather than decorative.
/// </summary>
public class TranscriptTopic
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public string StartTime { get; set; } = string.Empty;

    [JsonPropertyName("endTime")]
    public string EndTime { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("startTurnId")]
    public string StartTurnId { get; set; } = string.Empty;

    [JsonPropertyName("endTurnId")]
    public string EndTurnId { get; set; } = string.Empty;
}

/// <summary>
/// A single turn (utterance) by a speaker
/// </summary>
public class TranscriptTurn
{
    /// <summary>"T042". Assigned once, pre-fold, and never renumbered — every citation resolves through it.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("speaker")]
    public int Speaker { get; set; }

    [JsonPropertyName("speakerName")]
    public string SpeakerName { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public string StartTime { get; set; } = string.Empty;

    [JsonPropertyName("endTime")]
    public string EndTime { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Acknowledgements absorbed into this turn, keeping their own IDs so the fold
    /// stays reversible and no ID goes missing.
    /// </summary>
    [JsonPropertyName("foldedBackchannels")]
    public List<FoldedBackchannel> FoldedBackchannels { get; set; } = new();
}

public class FoldedBackchannel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("speakerName")]
    public string SpeakerName { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
