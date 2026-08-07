using Scribe.Models;

namespace Scribe.Services;

public class TranscriptFormatter
{
    private const double TurnSplitPauseSeconds = 2.0;
    public const string UnidentifiedSpeakerId = "0";

    public static Transcript FormatTranscript(RawTranscript raw)
    {
        // Display IDs come from order of first appearance, never from the producer's
        // label: WhisperX labels are strings, Azure's integers may have gaps, and
        // neither is guaranteed contiguous.
        var speakerIds = new Dictionary<string, int>();
        foreach (var segment in raw.Segments)
        {
            if (segment.Speaker != null && !speakerIds.ContainsKey(segment.Speaker))
                speakerIds[segment.Speaker] = speakerIds.Count + 1;
        }

        // Neutral labels only. A name here would be a factual claim that a person
        // attended and holds a view; names come from a human, in the naming loop.
        var speakers = speakerIds.Values.ToDictionary(
            id => id,
            id => new Speaker { Name = $"Speaker {id}", Identified = false });

        int IdOf(RawSegment segment) =>
            segment.Speaker != null ? speakerIds[segment.Speaker] : 0;

        // Unnamed labels stay distinct: collapsing them would claim several voices
        // are one person, which is the same fabrication as inventing a name.
        string NameOf(RawSegment segment) =>
            segment.Speaker != null
                ? speakers[speakerIds[segment.Speaker]].Name
                : "Unidentified speaker";

        var turns = new List<TranscriptTurn>();

        if (raw.Segments.Count > 0)
        {
            var currentTurn = NewTurn(raw.Segments[0]);

            for (int i = 1; i < raw.Segments.Count; i++)
            {
                var segment = raw.Segments[i];
                var previous = raw.Segments[i - 1];

                var pause = segment.StartSeconds - previous.EndSeconds;
                var sameSpeaker = segment.Speaker == previous.Speaker;

                if (sameSpeaker && pause <= TurnSplitPauseSeconds)
                {
                    currentTurn.Text += " " + segment.Text;
                }
                else
                {
                    currentTurn.EndTime = FormatTime(previous.EndSeconds);
                    turns.Add(currentTurn);
                    currentTurn = NewTurn(segment);
                }
            }

            currentTurn.EndTime = FormatTime(raw.Segments[^1].EndSeconds);
            turns.Add(currentTurn);
        }

        // Assigned once, in order, before any folding. Every citation resolves
        // through these, so nothing downstream may renumber them.
        for (int i = 0; i < turns.Count; i++)
            turns[i].Id = TurnId(i);

        turns = FoldBackchannels(turns);

        return new Transcript
        {
            Metadata = new TranscriptMetadata
            {
                DurationSeconds = raw.DurationSeconds,
                RecordingDate = DateTime.Now.ToString("yyyy-MM-dd"),
                SpeakerCount = speakerIds.Count,
                Speakers = speakers,
                SpeakersIdentified = SpeakerIdentification.None,
                SummaryStatus = SummaryStatus.Unavailable
            },
            Summary = new TranscriptSummary(),
            Topics = new List<TranscriptTopic>(),
            Turns = turns
        };

        TranscriptTurn NewTurn(RawSegment segment) => new()
        {
            Speaker = IdOf(segment),
            SpeakerName = NameOf(segment),
            StartTime = FormatTime(segment.StartSeconds),
            Text = segment.Text
        };
    }

    public static string TurnId(int index) => $"T{index:D3}";

    /// <summary>
    /// Content-free acknowledgements. Bare "yes"/"no" are deliberately absent: they are
    /// frequently the substantive answer to a question, and a turn line carries more
    /// weight than a fold marker.
    /// </summary>
    private static readonly HashSet<string> Backchannels = new(StringComparer.OrdinalIgnoreCase)
    {
        "yeah", "yep", "yup", "right", "okay", "ok", "mm-hmm", "mhm", "mmhmm", "uh-huh",
        "sure", "exactly", "got it", "i see", "true", "agreed", "of course", "makes sense",
        "gotcha", "fair enough", "hmm", "mm", "wow", "same", "totally", "absolutely",
        "yeah yeah", "right right", "okay okay", "oh okay", "oh right", "mm-hmm mm-hmm"
    };

    /// <summary>
    /// Moves content-free interjections out of the turn list and onto the turn they
    /// interrupt. Lossless: the text, speaker and original ID are all preserved on the
    /// fold, so a wrongly-classified interjection is a rendering wobble rather than
    /// deleted content — and every ID stays accounted for, as either a turn or a fold.
    /// Neighbouring turns are NOT merged: merging would consume the continuation's ID,
    /// and an ID that is neither a turn nor a fold is exactly the ambiguity IDs exist
    /// to prevent.
    /// </summary>
    private static List<TranscriptTurn> FoldBackchannels(List<TranscriptTurn> turns)
    {
        var folded = new List<TranscriptTurn>(turns.Count);

        foreach (var turn in turns)
        {
            // Nothing to attach to yet: the first turn stands even if it is "Okay."
            if (folded.Count > 0 && IsBackchannel(turn.Text) && folded[^1].Speaker != turn.Speaker)
            {
                folded[^1].FoldedBackchannels.Add(new FoldedBackchannel
                {
                    Id = turn.Id,
                    SpeakerName = turn.SpeakerName,
                    Text = turn.Text
                });
                continue;
            }

            folded.Add(turn);
        }

        return folded;
    }

    private static bool IsBackchannel(string text)
    {
        var normalized = new string(text.Where(c => !char.IsPunctuation(c) || c == '-').ToArray())
            .Trim()
            .ToLowerInvariant();

        return normalized.Length > 0 && Backchannels.Contains(normalized);
    }

    /// <summary>
    /// H:MM:SS. Timestamps are scrub targets for the source recording, so an hour
    /// has to roll over — "65:12" is not something a video player accepts.
    /// </summary>
    public static string FormatTime(double seconds)
    {
        var total = (long)seconds;
        return $"{total / 3600}:{total / 60 % 60:D2}:{total % 60:D2}";
    }
}
