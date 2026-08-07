using Scribe.Services;

namespace Scribe.Tests;

public class MeetingDefaultsTests
{
    // ── Date guessing ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("GMT20260714-140012_Recording.m4a", "2026-07-14")]  // Zoom
    [InlineData("gmt20260714-140012_recording.m4a", "2026-07-14")]
    [InlineData("2026-07-14_standup.m4a", "2026-07-14")]
    [InlineData("standup 2026-07-14.mp3", "2026-07-14")]
    [InlineData("20260714_research.wav", "2026-07-14")]
    [InlineData("2026-07-14-card-activation-onboarding", "2026-07-14")]  // directory name
    public void RecognisedFilenamePatterns_YieldTheirDate(string name, string expected)
    {
        Assert.True(MeetingDefaults.TryParseDate(name, out var date));
        Assert.Equal(expected, date);
    }

    [Theory]
    [InlineData("recording.m4a")]
    [InlineData("meeting-notes.wav")]
    [InlineData("20261345_impossible.wav")]  // month 13, day 45
    [InlineData("20260230_nonexistent.wav")] // February 30th
    public void UnparseableOrImpossibleDates_AreRejected(string name)
    {
        Assert.False(MeetingDefaults.TryParseDate(name, out _));
    }

    [Fact]
    public void MediaFilename_BeatsDirectoryName()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"scribe-test-{Guid.NewGuid():N}", "2026-01-01-old-name");
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "GMT20260714-140012_Recording.m4a"), "");

            // The recording is closer to the truth than a directory someone may have renamed.
            Assert.Equal("2026-07-14", MeetingDefaults.GuessDate(dir));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(dir)!, recursive: true);
        }
    }

    [Fact]
    public void NoDateAnywhere_FallsBackToTheFileTimestamp_NotToday()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"scribe-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var recording = Path.Combine(dir, "recording.m4a");
            File.WriteAllText(recording, "");
            File.SetLastWriteTime(recording, new DateTime(2026, 3, 9, 10, 0, 0));

            // A March meeting processed in August must not claim August.
            Assert.Equal("2026-03-09", MeetingDefaults.GuessDate(dir));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    // ── Media discovery ─────────────────────────────────────────────────────

    [Fact]
    public void MediaFile_IsFoundAndNonMediaIgnored()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"scribe-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "raw-transcription.json"), "{}");
            File.WriteAllText(Path.Combine(dir, "notes.txt"), "");
            File.WriteAllText(Path.Combine(dir, "session.mp4"), "");

            Assert.Equal("session.mp4", MeetingDefaults.FindMediaFile(dir));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void NoMedia_ReturnsNull()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"scribe-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "raw-transcription.json"), "{}");

            Assert.Null(MeetingDefaults.FindMediaFile(dir));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    // ── Title ───────────────────────────────────────────────────────────────

    [Fact]
    public void LongOneLiner_IsCutToSomethingThatReadsAsATitle()
    {
        // The raw one-liner produced a 94-character filename and an eight-times-repeated stamp.
        var title = MeetingDefaults.TitleFrom(
            "The team discussed issues with the activation flow and agreed on a design update", "2026-08-06");

        Assert.True(title.Length <= 60, $"'{title}' is {title.Length} chars");
        Assert.DoesNotContain("  ", title);
        Assert.False(title.EndsWith(' '));
    }

    [Fact]
    public void TruncatedTitle_DoesNotDangleOnAFunctionWord()
    {
        // "…and agreed on a" is a word boundary and still reads as broken text.
        var title = MeetingDefaults.TitleFrom(
            "The team discussed activation flow issues and agreed on a design update", "2026-08-06");

        var lastWord = title.Split(' ')[^1];
        Assert.DoesNotContain(lastWord.ToLowerInvariant(), new[] { "a", "an", "the", "and", "on", "of", "to", "with" });
        Assert.Equal("The team discussed activation flow issues", title);
    }

    [Fact]
    public void ClauseBoundary_EndsTheTitleEarly()
    {
        var title = MeetingDefaults.TitleFrom(
            "Card activation onboarding, session three, with two customers", "2026-08-06");

        Assert.Equal("Card activation onboarding", title);
    }

    [Fact]
    public void ShortOneLiner_IsKeptWholeWithoutTrailingPunctuation()
    {
        Assert.Equal("Weekly design review", MeetingDefaults.TitleFrom("Weekly design review.", "2026-08-06"));
    }

    [Fact]
    public void NoOneLiner_FallsBackToADatedTitle()
    {
        Assert.Equal("2026-08-06 meeting", MeetingDefaults.TitleFrom(null, "2026-08-06"));
        Assert.Equal("2026-08-06 meeting", MeetingDefaults.TitleFrom("   ", "2026-08-06"));
    }
}
