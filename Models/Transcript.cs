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
    public Dictionary<int, string> Speakers { get; set; } = new();

    [JsonPropertyName("meetingTitle")]
    public string? MeetingTitle { get; set; }

    [JsonPropertyName("meetingPurpose")]
    public string? MeetingPurpose { get; set; }
}

/// <summary>
/// AI-generated summary with grounding to transcript turns
/// </summary>
public class TranscriptSummary
{
    [JsonPropertyName("oneLiner")]
    public string? OneLiner { get; set; }

    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("keyPoints")]
    public List<SummaryKeyPoint>? KeyPoints { get; set; }

    [JsonPropertyName("actionItems")]
    public List<SummaryActionItem> ActionItems { get; set; } = new();
}

/// <summary>
/// A key point from the summary, grounded in specific transcript turns
/// </summary>
public class SummaryKeyPoint
{
    [JsonPropertyName("point")]
    public string Point { get; set; } = string.Empty;

    [JsonPropertyName("turnIndices")]
    public List<int> TurnIndices { get; set; } = new();
}

/// <summary>
/// An action item from the summary, grounded in specific transcript turns
/// </summary>
public class SummaryActionItem
{
    [JsonPropertyName("item")]
    public string Item { get; set; } = string.Empty;

    [JsonPropertyName("turnIndices")]
    public List<int> TurnIndices { get; set; } = new();

    [JsonPropertyName("assignedTo")]
    public string? AssignedTo { get; set; }
}

/// <summary>
/// A topic or section within the transcript
/// </summary>
public class TranscriptTopic
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public string StartTime { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}

/// <summary>
/// A single turn (utterance) by a speaker
/// </summary>
public class TranscriptTurn
{
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
}
