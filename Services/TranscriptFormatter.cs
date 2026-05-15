using Scribe.Models;

namespace Scribe.Services;

public class TranscriptFormatter
{
    private static readonly string[] SpeakerNames = new[]
    {
        "Alice", "Bob", "Charles", "Dave", "Edward", "Francis",
        "George", "Henrietta", "Ivan", "Jane"
    };

    public static Transcript FormatTranscript(FastTranscriptionResult fastResult)
    {
        var speakerCount = fastResult.Phrases?
            .Select(p => p.Speaker ?? 0)
            .Distinct()
            .Count() ?? 0;

        // Build speaker name mapping
        var speakers = new Dictionary<int, string>();
        for (int i = 1; i <= speakerCount; i++)
        {
            speakers[i] = i <= SpeakerNames.Length
                ? SpeakerNames[i - 1]
                : $"Speaker {i}";
        }

        // Group phrases into turns (split on >2 second pauses)
        var turns = new List<TranscriptTurn>();

        if (fastResult.Phrases != null && fastResult.Phrases.Length > 0)
        {
            var currentTurn = new TranscriptTurn
            {
                Speaker = fastResult.Phrases[0].Speaker ?? 0,
                SpeakerName = speakers.GetValueOrDefault(fastResult.Phrases[0].Speaker ?? 0, "Unknown"),
                StartTime = FormatTime(fastResult.Phrases[0].OffsetMilliseconds),
                Text = fastResult.Phrases[0].Text ?? string.Empty
            };

            for (int i = 1; i < fastResult.Phrases.Length; i++)
            {
                var phrase = fastResult.Phrases[i];
                var prevPhrase = fastResult.Phrases[i - 1];

                var pauseDuration = phrase.OffsetMilliseconds -
                    (prevPhrase.OffsetMilliseconds + prevPhrase.DurationMilliseconds);

                var sameSpeaker = phrase.Speaker == currentTurn.Speaker;
                var shortPause = pauseDuration <= 2000; // 2 seconds or less

                if (sameSpeaker && shortPause)
                {
                    // Continue current turn
                    currentTurn.Text += " " + (phrase.Text ?? string.Empty);
                }
                else
                {
                    // End current turn, start new one
                    currentTurn.EndTime = FormatTime(
                        prevPhrase.OffsetMilliseconds + prevPhrase.DurationMilliseconds);
                    turns.Add(currentTurn);

                    currentTurn = new TranscriptTurn
                    {
                        Speaker = phrase.Speaker ?? 0,
                        SpeakerName = speakers.GetValueOrDefault(phrase.Speaker ?? 0, "Unknown"),
                        StartTime = FormatTime(phrase.OffsetMilliseconds),
                        Text = phrase.Text ?? string.Empty
                    };
                }
            }

            // Add the last turn
            var lastPhrase = fastResult.Phrases[^1];
            currentTurn.EndTime = FormatTime(
                lastPhrase.OffsetMilliseconds + lastPhrase.DurationMilliseconds);
            turns.Add(currentTurn);
        }

        return new Transcript
        {
            Metadata = new TranscriptMetadata
            {
                DurationSeconds = fastResult.DurationMilliseconds / 1000.0,
                RecordingDate = DateTime.Now.ToString("yyyy-MM-dd"),
                SpeakerCount = speakerCount,
                Speakers = speakers,
                MeetingTitle = "[Placeholder] Meeting Title",
                MeetingPurpose = null
            },
            Summary = new TranscriptSummary
            {
                OneLiner = "[Coming soon] AI-generated summary will appear here",
                Overview = null,
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
    }

    private static string FormatTime(long milliseconds)
    {
        var totalSeconds = milliseconds / 1000;
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        return $"{minutes:D2}:{seconds:D2}";
    }
}
