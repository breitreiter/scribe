using System.Text.Json;
using Scribe.Models;
using Scribe.Services;

namespace Scribe.Tests;

public class RawTranscriptReaderTests
{
    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", name));

    private static RawTranscript WhisperX() => RawTranscriptReader.Read(Fixture("whisperx-raw.json"));
    private static RawTranscript Azure() => RawTranscriptReader.Read(Fixture("azure-raw.json"));

    // ── Format detection ────────────────────────────────────────────────────

    [Fact]
    public void WhisperXFixture_DetectedAsWhisperX()
    {
        Assert.Equal("whisperx", WhisperX().Provider);
    }

    [Fact]
    public void AzureFixture_DetectedAsAzure()
    {
        Assert.Equal("azure-speech-fast", Azure().Provider);
    }

    [Fact]
    public void UnrecognizedShape_ThrowsWithGuidance()
    {
        var ex = Assert.Throws<InvalidDataException>(() => RawTranscriptReader.Read("""{"foo": 1}"""));

        Assert.Contains("docs/generating-transcripts.md", ex.Message);
    }

    [Fact]
    public void MalformedJson_Throws()
    {
        Assert.ThrowsAny<JsonException>(() => RawTranscriptReader.Read("not json"));
    }

    // ── WhisperX specifics ──────────────────────────────────────────────────

    [Fact]
    public void WhisperX_SecondsAreReadAsSeconds()
    {
        var first = WhisperX().Segments[0];

        Assert.Equal(0.309, first.StartSeconds, 3);
        Assert.Equal(4.72, first.EndSeconds, 3);
    }

    [Fact]
    public void WhisperX_LeadingSpaceIsTrimmed()
    {
        // WhisperX segment text conventionally starts with a space.
        Assert.StartsWith("Thanks everyone", WhisperX().Segments[0].Text);
    }

    [Fact]
    public void WhisperX_AbsentSpeakerKey_BecomesNullNotACrash()
    {
        var backchannel = WhisperX().Segments.Single(s => s.Text == "Mm-hmm.");

        Assert.Null(backchannel.Speaker);
    }

    [Fact]
    public void WhisperX_DurationDerivedFromLastSegmentEnd()
    {
        // WhisperX emits no top-level duration field.
        Assert.Equal(60.0, WhisperX().DurationSeconds, 3);
    }

    [Fact]
    public void WhisperX_LanguageIsRead()
    {
        Assert.Equal("en", WhisperX().Language);
    }

    // ── Azure specifics ─────────────────────────────────────────────────────

    [Fact]
    public void Azure_MillisecondsConvertedToSeconds()
    {
        var first = Azure().Segments[0];

        Assert.Equal(0.309, first.StartSeconds, 3);
        Assert.Equal(4.72, first.EndSeconds, 3);
    }

    [Fact]
    public void Azure_NullSpeaker_StaysNull()
    {
        var backchannel = Azure().Segments.Single(s => s.Text == "Mm-hmm.");

        Assert.Null(backchannel.Speaker);
    }

    [Fact]
    public void Azure_DurationFromDurationMilliseconds()
    {
        Assert.Equal(60.0, Azure().DurationSeconds, 3);
    }

    // ── The load-bearing assertion ──────────────────────────────────────────

    [Fact]
    public void BothProviders_ProduceIdenticalTranscript()
    {
        // Same six-speaker conversation in both shapes. The producers' labels differ
        // deliberately (SPEAKER_03 vs 4, and neither set is contiguous or zero-based),
        // so this passes only if display IDs come from order of first appearance.
        var fromWhisperX = TranscriptFormatter.FormatTranscript(WhisperX());
        var fromAzure = TranscriptFormatter.FormatTranscript(Azure());

        Assert.Equal(
            JsonSerializer.Serialize(fromWhisperX),
            JsonSerializer.Serialize(fromAzure));
    }

    [Fact]
    public void SixSpeakerFixture_YieldsSixSpeakers()
    {
        var transcript = TranscriptFormatter.FormatTranscript(WhisperX());

        Assert.Equal(6, transcript.Metadata.SpeakerCount);
        Assert.Equal("Speaker 1", transcript.Metadata.Speakers[1].Name);
        Assert.Equal("Speaker 6", transcript.Metadata.Speakers[6].Name);
    }

    [Fact]
    public void UndiarizedSegment_IsNotAttributedToAnyone()
    {
        var transcript = TranscriptFormatter.FormatTranscript(WhisperX());

        // "Mm-hmm." is an acknowledgement, so it folds — but it still must not be
        // attributed to whoever happened to speak around it.
        var fold = transcript.Turns
            .SelectMany(t => t.FoldedBackchannels)
            .Single(f => f.Text == "Mm-hmm.");

        Assert.Equal("Unidentified speaker", fold.SpeakerName);
        Assert.DoesNotContain(0, transcript.Metadata.Speakers.Keys);
    }
}
