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
    /// H:MM:SS. Timestamps are scrub targets for the source recording, so an hour
    /// has to roll over — "65:12" is not something a video player accepts.
    /// </summary>
    public static string FormatTime(double seconds)
    {
        var total = (long)seconds;
        return $"{total / 3600}:{total / 60 % 60:D2}:{total % 60:D2}";
    }
}
