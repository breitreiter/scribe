using Scribe.Models;
using Scribe.Services;

namespace Scribe.Tests;

public class MeetingMarkdownWriterTests
{
    private static Transcript Meeting(bool summarized = true)
    {
        var transcript = new Transcript
        {
            Metadata = new TranscriptMetadata
            {
                RecordingDate = "2026-07-14",
                MeetingTitle = "Card activation onboarding",
                MeetingPurpose = "Decide whether in-app activation ships this quarter.",
                MediaFile = "GMT20260714-140012_Recording.m4a",
                DurationSeconds = 824,
                SpeakerCount = 3,
                SpeakersIdentified = SpeakerIdentification.Partial,
                SummaryStatus = summarized ? SummaryStatus.Ok : SummaryStatus.Unavailable,
                Speakers = new Dictionary<int, Speaker>
                {
                    [1] = new() { Name = "Dana Okafor", Role = "researcher", Identified = true },
                    [2] = new() { Name = "Marcus Webb", Role = "product manager", Identified = true },
                    [3] = new() { Name = "Speaker 3", FlaggedMultipleVoices = true }
                }
            },
            Turns =
            [
                new TranscriptTurn
                {
                    Id = "T000", Speaker = 1, SpeakerName = "Dana Okafor",
                    StartTime = "0:00:00", Text = "Where does that leave the in-app path?",
                    FoldedBackchannels =
                    [
                        new FoldedBackchannel { Id = "T001", SpeakerName = "Marcus Webb", Text = "Mm-hmm." }
                    ]
                },
                new TranscriptTurn
                {
                    Id = "T002", Speaker = 2, SpeakerName = "Marcus Webb",
                    StartTime = "0:01:04", Text = "I'll scope it for this quarter."
                }
            ]
        };

        if (summarized)
        {
            transcript.Summary = new TranscriptSummary
            {
                OneLiner = "The team scoped in-app activation.",
                Abstract = "Dana Okafor asked where the in-app path stood and Marcus Webb committed to scoping it.",
                Decisions =
                [
                    new SummaryDecision
                    {
                        Decision = "In-app activation is scoped this quarter",
                        Rationale = "Marcus Webb accepted the shipped flow was unintended.",
                        TurnIds = ["T002"]
                    }
                ],
                ActionItems =
                [
                    new SummaryActionItem { Item = "Scope in-app activation", TurnIds = ["T002"], AssignedTo = "Marcus Webb" }
                ],
                OpenQuestions =
                [
                    new SummaryOpenQuestion { Question = "Who designs the screen?", TurnIds = ["T000"] }
                ],
                KeyPoints =
                [
                    new SummaryKeyPoint { Point = "Nobody could find the activation control", TurnIds = ["T000", "T002"] }
                ]
            };
        }

        return transcript;
    }

    // ── Frontmatter ─────────────────────────────────────────────────────────

    [Fact]
    public void FrontmatterSlug_MatchesTheFileName()
    {
        var transcript = Meeting();
        var md = MeetingMarkdownWriter.Write(transcript);

        Assert.Contains($"slug: {Path.GetFileNameWithoutExtension(MeetingMarkdownWriter.FileName(transcript))}", md);
    }

    [Fact]
    public void Frontmatter_CarriesTheRetrievalFilters()
    {
        var md = MeetingMarkdownWriter.Write(Meeting());

        Assert.Contains("date: 2026-07-14", md);
        Assert.Contains("duration: 0:13:44", md);
        Assert.Contains("media: GMT20260714-140012_Recording.m4a", md);
        Assert.Contains("speakers_identified: partial", md);
        Assert.Contains("summary_status: ok", md);
    }

    [Fact]
    public void Frontmatter_ListsParticipantsWithRoles()
    {
        var md = MeetingMarkdownWriter.Write(Meeting());

        Assert.Contains("  - name: Dana Okafor", md);
        Assert.Contains("    role: researcher", md);
    }

    [Fact]
    public void FlaggedLabel_IsListedRatherThanOmitted()
    {
        // A missing entry looks like a smaller meeting; the file must show someone is unaccounted for.
        var md = MeetingMarkdownWriter.Write(Meeting());

        Assert.Contains("  - name: Speaker 3", md);
        Assert.Contains("    flagged: may contain more than one voice", md);
        Assert.Contains("flagged as possibly holding more than one voice", md);
    }

    // ── Stamps ──────────────────────────────────────────────────────────────

    [Fact]
    public void EverySectionHeadingIsFollowedByAStamp()
    {
        var lines = MeetingMarkdownWriter.Write(Meeting()).Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith("## ")) continue;

            // Heading, blank, stamp.
            Assert.True(lines[i + 2].TrimStart().StartsWith("*("),
                $"Section '{lines[i]}' is not followed by a stamp; a chunk starting here would have no context.");
        }
    }

    [Fact]
    public void SubsectionsCarryACompactStamp()
    {
        var transcript = Meeting();
        transcript.Topics =
        [
            new TranscriptTopic
            {
                Title = "The missing activate button", StartTime = "0:00:00", EndTime = "0:13:44",
                Summary = "Nobody found it.", StartTurnId = "T000", EndTurnId = "T002"
            }
        ];

        var lines = MeetingMarkdownWriter.Write(transcript).Split('\n');

        foreach (var i in Enumerable.Range(0, lines.Length).Where(i => lines[i].StartsWith("### ")))
            Assert.True(lines[i + 2].TrimStart().StartsWith("*("),
                $"Subsection '{lines[i]}' has no stamp; chunkers split here.");
    }

    // ── Roles ride with the claim ───────────────────────────────────────────

    [Fact]
    public void FirstMentionInASectionGetsTheRole()
    {
        var md = MeetingMarkdownWriter.Write(Meeting());

        Assert.Contains("Dana Okafor (researcher) asked where the in-app path stood", md);
    }

    [Fact]
    public void RoleIsNotRepeatedWithinTheSameSection()
    {
        var transcript = Meeting();
        transcript.Summary.Abstract = "Marcus Webb said one thing. Marcus Webb said another.";

        var md = MeetingMarkdownWriter.Write(transcript);

        Assert.Equal(1, CountOccurrences(md, "Marcus Webb (product manager) said"));
    }

    [Fact]
    public void RoleIsReintroducedInEachSection()
    {
        // A chunk from one section cannot rely on another section's annotation.
        var md = MeetingMarkdownWriter.Write(Meeting());

        Assert.Contains("**Scope in-app activation** — Marcus Webb (product manager)", md);
        Assert.Contains("Marcus Webb (product manager) accepted", md);
    }

    // ── Explicit absence ────────────────────────────────────────────────────

    [Fact]
    public void EmptySections_SayNoneRatherThanBeingOmitted()
    {
        var transcript = Meeting();
        transcript.Summary.Decisions = [];
        transcript.Summary.ActionItems = [];
        transcript.Summary.OpenQuestions = [];

        var md = MeetingMarkdownWriter.Write(transcript);

        Assert.Contains("## Decisions", md);
        Assert.Contains("No decisions were taken in this meeting.", md);
        Assert.Contains("No action items were assigned in this meeting.", md);
        Assert.Contains("No questions were left open in this meeting.", md);
    }

    [Fact]
    public void NoSummarizer_OmitsTheSectionsAndSaysSo()
    {
        var md = MeetingMarkdownWriter.Write(Meeting(summarized: false));

        Assert.Contains("summary_status: unavailable", md);
        Assert.Contains("**No summary was produced for this meeting.**", md);
        Assert.Contains("nothing looked for them", md);

        // The distinction the whole rule exists for.
        Assert.DoesNotContain("## Decisions", md);
        Assert.DoesNotContain("No decisions were taken", md);

        Assert.Contains("## Transcript", md);
    }

    // ── Transcript ──────────────────────────────────────────────────────────

    [Fact]
    public void TurnLines_CarryIdTimestampAndSpeaker()
    {
        var md = MeetingMarkdownWriter.Write(Meeting());

        Assert.Contains("[T000 0:00:00] Dana Okafor (researcher): Where does that leave the in-app path?", md);
    }

    [Fact]
    public void FoldedBackchannels_RenderInlineWithTheirOwnId()
    {
        var md = MeetingMarkdownWriter.Write(Meeting());

        Assert.Contains("[T001 folded: Marcus Webb: Mm-hmm.]", md);
    }

    [Fact]
    public void EveryTurnIdAppears_SoNoCitationDanglesIntoNothing()
    {
        var transcript = Meeting();
        var md = MeetingMarkdownWriter.Write(transcript);

        foreach (var turn in transcript.Turns)
        {
            Assert.Contains($"[{turn.Id} ", md);
            foreach (var fold in turn.FoldedBackchannels)
                Assert.Contains($"[{fold.Id} folded:", md);
        }
    }

    // ── Decision identity ───────────────────────────────────────────────────

    [Fact]
    public void DecisionIds_DeriveFromTheirFirstCitedTurn()
    {
        var md = MeetingMarkdownWriter.Write(Meeting());

        Assert.Contains("**D-T002 — In-app activation is scoped this quarter**", md);
    }

    [Fact]
    public void DecisionsSharingAFirstTurn_AreDisambiguated()
    {
        var transcript = Meeting();
        transcript.Summary.Decisions =
        [
            new SummaryDecision { Decision = "First thing", TurnIds = ["T002"] },
            new SummaryDecision { Decision = "Second thing", TurnIds = ["T002"] }
        ];

        var md = MeetingMarkdownWriter.Write(transcript);

        Assert.Contains("**D-T002 — First thing**", md);
        Assert.Contains("**D-T002b — Second thing**", md);
    }

    // ── Naming ──────────────────────────────────────────────────────────────

    [Fact]
    public void FileName_IsDateAndSlug()
    {
        Assert.Equal("2026-07-14-card-activation-onboarding.md", MeetingMarkdownWriter.FileName(Meeting()));
    }

    [Fact]
    public void UntitledMeeting_StillGetsAResolvableName()
    {
        var transcript = Meeting();
        transcript.Metadata.MeetingTitle = null;

        // Not "2026-07-14-2026-07-14-meeting.md": the fallback display title already
        // contains the date, and the filename must not repeat it.
        Assert.Equal("2026-07-14-meeting.md", MeetingMarkdownWriter.FileName(transcript));
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        for (var i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
            count++;
        return count;
    }
}
