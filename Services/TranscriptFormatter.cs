using Scribe.Models;

namespace Scribe.Services;

public class TranscriptFormatter
{
    private const double TurnSplitPauseSeconds = 2.0;

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
        // attended and holds a view; names come from a human, once identification lands.
        var speakers = speakerIds.Values.ToDictionary(id => id, id => $"Speaker {id}");

        int IdOf(RawSegment segment) =>
            segment.Speaker != null ? speakerIds[segment.Speaker] : 0;

        string NameOf(RawSegment segment) =>
            segment.Speaker != null ? speakers[speakerIds[segment.Speaker]] : "Unidentified speaker";

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

        return new Transcript
        {
            Metadata = new TranscriptMetadata
            {
                DurationSeconds = raw.DurationSeconds,
                RecordingDate = DateTime.Now.ToString("yyyy-MM-dd"),
                SpeakerCount = speakerIds.Count,
                Speakers = speakers,
            },
            Summary = new TranscriptSummary
            {
                ActionItems = new List<SummaryActionItem>()
            },
            Topics = new List<TranscriptTopic>
            {
                new TranscriptTopic
                {
                    Title = "Full Transcript",
                    StartTime = "00:00"
                }
            },
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

    private static string FormatTime(double seconds)
    {
        var totalSeconds = (long)seconds;
        return $"{totalSeconds / 60:D2}:{totalSeconds % 60:D2}";
    }
}
