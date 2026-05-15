namespace Scribe.Models;

/// <summary>
/// Unified transcription result that works for Azure Speech Fast Transcription
/// </summary>
public class TranscriptionResult
{
    public string Provider { get; set; } = string.Empty;
    public double DurationSeconds { get; set; }
    public string Language { get; set; } = string.Empty;
    public List<TranscriptionSegment> Segments { get; set; } = new();
    public string RawJsonPath { get; set; } = string.Empty;
    public string TranscriptPath { get; set; } = string.Empty;
    public string HtmlPath { get; set; } = string.Empty;
    public bool SummaryGenerated { get; set; }
    public string? MeetingTitle { get; set; }
}

public class TranscriptionSegment
{
    public int SpeakerId { get; set; } // 0 if no speaker diarization
    public double StartSeconds { get; set; }
    public double EndSeconds { get; set; }
    public string Text { get; set; } = string.Empty;
    public double Confidence { get; set; }
}
