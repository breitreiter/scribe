namespace Scribe.Models;

/// <summary>
/// A transcription as scribe consumes it, normalized away from whichever tool produced it.
/// </summary>
public class RawTranscript
{
    public string Provider { get; init; } = string.Empty;
    public string? Model { get; init; }
    public string? Language { get; init; }
    public double DurationSeconds { get; init; }
    public List<RawSegment> Segments { get; init; } = new();
}

/// <summary>
/// One diarized utterance. Speaker is the producer's own label (WhisperX "SPEAKER_00",
/// Azure "2") and is null when diarization assigned none — display IDs are derived from
/// order of first appearance, never from the label itself.
/// </summary>
public class RawSegment
{
    public string? Speaker { get; init; }
    public double StartSeconds { get; init; }
    public double EndSeconds { get; init; }
    public string Text { get; init; } = string.Empty;
}
