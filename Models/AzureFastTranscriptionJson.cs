using System.Text.Json.Serialization;

namespace Scribe.Models;

// Wire format of Azure AI Speech Fast Transcription output. Scribe no longer calls
// that API — these types exist only to READ raw JSON that some other tool produced,
// so that meeting directories transcribed before the pivot still reprocess.
// New transcripts come from WhisperX; see docs/generating-transcripts.md.

public class FastTranscriptionResult
{
    [JsonPropertyName("durationMilliseconds")]
    public long DurationMilliseconds { get; set; }

    [JsonPropertyName("combinedPhrases")]
    public CombinedPhrase[]? CombinedPhrases { get; set; }

    [JsonPropertyName("phrases")]
    public Phrase[]? Phrases { get; set; }
}

public class CombinedPhrase
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("channel")]
    public int? Channel { get; set; }
}

public class Phrase
{
    [JsonPropertyName("offsetMilliseconds")]
    public long OffsetMilliseconds { get; set; }

    [JsonPropertyName("durationMilliseconds")]
    public long DurationMilliseconds { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("speaker")]
    public int? Speaker { get; set; }

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("words")]
    public Word[]? Words { get; set; }
}

public class Word
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("offsetMilliseconds")]
    public long OffsetMilliseconds { get; set; }

    [JsonPropertyName("durationMilliseconds")]
    public long DurationMilliseconds { get; set; }

    [JsonPropertyName("confidence")]
    public double? Confidence { get; set; }
}
