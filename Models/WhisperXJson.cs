using System.Text.Json.Serialization;

namespace Scribe.Models;

// Wire format written by whisperx --output_format json. WriteJSON dumps the result
// dict verbatim, so this mirrors AlignedTranscriptionResult plus the diarization
// fields added by assign_word_speakers. Verified against whisperx 3.8.6.

public class WhisperXResult
{
    [JsonPropertyName("segments")]
    public List<WhisperXSegment>? Segments { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }
}

public class WhisperXSegment
{
    [JsonPropertyName("start")]
    public double Start { get; set; }

    [JsonPropertyName("end")]
    public double End { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Label such as "SPEAKER_00". The key is absent — not null — on segments where
    /// diarization found no overlapping speaker turn, so this stays nullable.
    /// </summary>
    [JsonPropertyName("speaker")]
    public string? Speaker { get; set; }
}

// word_segments and per-word timings are deliberately not modeled: nothing consumes
// them yet, and WhisperX emits them only when alignment ran.
