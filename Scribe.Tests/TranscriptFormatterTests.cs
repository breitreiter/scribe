using Scribe.Services;

namespace Scribe.Tests;

public class TranscriptFormatterTests
{
    private static Phrase Phrase(int speaker, long offsetMs, long durationMs, string text) => new()
    {
        Speaker = speaker,
        OffsetMilliseconds = offsetMs,
        DurationMilliseconds = durationMs,
        Text = text,
        Confidence = 1.0
    };

    // ── Null / empty input ──────────────────────────────────────────────────

    [Fact]
    public void NullPhrases_ProducesNoTurnsAndZeroSpeakers()
    {
        var result = TranscriptFormatter.FormatTranscript(new FastTranscriptionResult { Phrases = null });

        Assert.Empty(result.Turns);
        Assert.Equal(0, result.Metadata.SpeakerCount);
    }

    [Fact]
    public void EmptyPhrases_ProducesNoTurns()
    {
        var result = TranscriptFormatter.FormatTranscript(new FastTranscriptionResult { Phrases = [] });

        Assert.Empty(result.Turns);
    }

    // ── Single phrase ───────────────────────────────────────────────────────

    [Fact]
    public void SinglePhrase_ProducesSingleTurn()
    {
        var result = TranscriptFormatter.FormatTranscript(new FastTranscriptionResult
        {
            DurationMilliseconds = 5000,
            Phrases = [Phrase(1, 0, 5000, "Hello world")]
        });

        Assert.Single(result.Turns);
        Assert.Equal("Hello world", result.Turns[0].Text);
    }

    [Fact]
    public void SinglePhrase_EndTimeEqualsOffsetPlusDuration()
    {
        var result = TranscriptFormatter.FormatTranscript(new FastTranscriptionResult
        {
            Phrases = [Phrase(1, 65000, 10000, "Hi")]
        });

        // StartTime = 65s → 01:05, EndTime = 75s → 01:15
        Assert.Equal("01:05", result.Turns[0].StartTime);
        Assert.Equal("01:15", result.Turns[0].EndTime);
    }

    // ── Turn splitting ──────────────────────────────────────────────────────

    [Fact]
    public void SameSpeaker_PauseWithin2Seconds_ConcatenatesIntoOneTurn()
    {
        var phrases = new[]
        {
            Phrase(1, 0,    1000, "Hello"),
            Phrase(1, 2999, 1000, "world") // pause = 2999 - (0+1000) = 1999ms ≤ 2000
        };

        var result = TranscriptFormatter.FormatTranscript(new FastTranscriptionResult { Phrases = phrases });

        Assert.Single(result.Turns);
        Assert.Equal("Hello world", result.Turns[0].Text);
    }

    [Fact]
    public void SameSpeaker_PauseExactly2Seconds_ConcatenatesIntoOneTurn()
    {
        var phrases = new[]
        {
            Phrase(1, 0,    1000, "Hello"),
            Phrase(1, 3000, 1000, "world") // pause = 3000 - 1000 = 2000ms → boundary, ≤ 2000
        };

        var result = TranscriptFormatter.FormatTranscript(new FastTranscriptionResult { Phrases = phrases });

        Assert.Single(result.Turns);
    }

    [Fact]
    public void SameSpeaker_PauseOver2Seconds_SplitsIntoTwoTurns()
    {
        var phrases = new[]
        {
            Phrase(1, 0,    1000, "Hello"),
            Phrase(1, 3001, 1000, "world") // pause = 3001 - 1000 = 2001ms > 2000
        };

        var result = TranscriptFormatter.FormatTranscript(new FastTranscriptionResult { Phrases = phrases });

        Assert.Equal(2, result.Turns.Count);
        Assert.Equal("Hello", result.Turns[0].Text);
        Assert.Equal("world", result.Turns[1].Text);
    }

    [Fact]
    public void DifferentSpeakers_ShortPause_StillSplitsIntoTwoTurns()
    {
        var phrases = new[]
        {
            Phrase(1, 0,    500, "I think"),
            Phrase(2, 600, 500, "I agree") // pause = 100ms, but different speaker
        };

        var result = TranscriptFormatter.FormatTranscript(new FastTranscriptionResult { Phrases = phrases });

        Assert.Equal(2, result.Turns.Count);
        Assert.Equal(1, result.Turns[0].Speaker);
        Assert.Equal(2, result.Turns[1].Speaker);
    }

    [Fact]
    public void LastTurnEndTime_IsCorrect()
    {
        var phrases = new[]
        {
            Phrase(1, 0,    2000, "First"),
            Phrase(2, 5000, 3000, "Second") // ends at 8000ms = 00:08
        };

        var result = TranscriptFormatter.FormatTranscript(new FastTranscriptionResult { Phrases = phrases });

        Assert.Equal("00:08", result.Turns[^1].EndTime);
    }

    // ── Speaker naming ──────────────────────────────────────────────────────

    [Fact]
    public void SpeakerIds_MappedToNamesList()
    {
        var phrases = new[]
        {
            Phrase(1, 0,    1000, "A"),
            Phrase(2, 5000, 1000, "B"),
            Phrase(3, 10000, 1000, "C")
        };

        var result = TranscriptFormatter.FormatTranscript(new FastTranscriptionResult { Phrases = phrases });

        Assert.Equal("Alice",   result.Turns[0].SpeakerName);
        Assert.Equal("Bob",     result.Turns[1].SpeakerName);
        Assert.Equal("Charles", result.Turns[2].SpeakerName);
    }

    [Fact]
    public void SpeakerIdBeyondNamesList_FallsBackToGenericName()
    {
        // Generate 11 distinct speakers (names list only has 10)
        var phrases = Enumerable.Range(1, 11)
            .Select(i => Phrase(i, i * 5000L, 1000, $"Text {i}"))
            .ToArray();

        var result = TranscriptFormatter.FormatTranscript(new FastTranscriptionResult { Phrases = phrases });

        Assert.Equal("Speaker 11", result.Turns[10].SpeakerName);
    }

    [Fact]
    public void SpeakerCount_ReflectsDistinctSpeakers()
    {
        var phrases = new[]
        {
            Phrase(1, 0,     1000, "A"),
            Phrase(2, 5000,  1000, "B"),
            Phrase(1, 10000, 1000, "A again")
        };

        var result = TranscriptFormatter.FormatTranscript(new FastTranscriptionResult { Phrases = phrases });

        Assert.Equal(2, result.Metadata.SpeakerCount);
    }

    // ── Metadata ────────────────────────────────────────────────────────────

    [Fact]
    public void DurationInMetadata_MatchesDurationMilliseconds()
    {
        var result = TranscriptFormatter.FormatTranscript(new FastTranscriptionResult
        {
            DurationMilliseconds = 90000,
            Phrases = [Phrase(1, 0, 1000, "Hi")]
        });

        Assert.Equal(90.0, result.Metadata.DurationSeconds);
    }

    [Fact]
    public void RecordingDate_IsToday()
    {
        var result = TranscriptFormatter.FormatTranscript(new FastTranscriptionResult
        {
            Phrases = [Phrase(1, 0, 1000, "Hi")]
        });

        Assert.Equal(DateTime.Now.ToString("yyyy-MM-dd"), result.Metadata.RecordingDate);
    }

    // ── Time formatting ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(0,       "00:00")]
    [InlineData(1000,    "00:01")]
    [InlineData(60000,   "01:00")]
    [InlineData(65000,   "01:05")]
    [InlineData(3661000, "61:01")] // > 1 hour, no hours segment
    public void TimeFormatting_CorrectOutput(long offsetMs, string expected)
    {
        var result = TranscriptFormatter.FormatTranscript(new FastTranscriptionResult
        {
            Phrases = [Phrase(1, offsetMs, 1000, "Hi")]
        });

        Assert.Equal(expected, result.Turns[0].StartTime);
    }
}
