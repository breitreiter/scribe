using System.Text.Json;
using Scribe.Models;
using Scribe.Utils;

namespace Scribe.Services;

/// <summary>
/// Reads a raw transcription file, detecting which tool produced it.
/// </summary>
public static class RawTranscriptReader
{
    public static RawTranscript Read(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Raw transcription is not a JSON object.");

        if (root.TryGetProperty("segments", out _))
            return ReadWhisperX(json);

        if (root.TryGetProperty("phrases", out _) || root.TryGetProperty("durationMilliseconds", out _))
            return ReadAzureFastTranscription(json);

        throw new InvalidDataException(
            "Unrecognized raw transcription format: expected a WhisperX result (top-level \"segments\") " +
            "or an Azure Speech fast transcription response (top-level \"phrases\"). " +
            "See docs/generating-transcripts.md.");
    }

    /// <summary>
    /// WhisperX writes its result dict verbatim: segments with float seconds, string speaker
    /// labels, and no duration. Verified against whisperx 3.8.6.
    /// </summary>
    private static RawTranscript ReadWhisperX(string json)
    {
        var result = JsonSerializer.Deserialize<WhisperXResult>(json, Json.CaseInsensitive)
                     ?? throw new InvalidDataException("Failed to parse WhisperX transcription JSON.");

        var segments = (result.Segments ?? new List<WhisperXSegment>())
            .Where(s => !string.IsNullOrWhiteSpace(s.Text))
            .Select(s => new RawSegment
            {
                // Absent on segments where diarization found no overlapping speaker turn.
                Speaker = s.Speaker,
                StartSeconds = s.Start,
                EndSeconds = s.End,
                Text = s.Text!.Trim()
            })
            .ToList();

        return new RawTranscript
        {
            Provider = "whisperx",
            Language = result.Language,
            // WhisperX emits no duration; the last segment's end is the best available answer.
            DurationSeconds = segments.Count > 0 ? segments[^1].EndSeconds : 0,
            Segments = segments
        };
    }

    private static RawTranscript ReadAzureFastTranscription(string json)
    {
        var result = JsonSerializer.Deserialize<FastTranscriptionResult>(json, Json.CaseInsensitive)
                     ?? throw new InvalidDataException("Failed to parse Azure fast transcription JSON.");

        var segments = (result.Phrases ?? Array.Empty<Phrase>())
            .Where(p => !string.IsNullOrWhiteSpace(p.Text))
            .Select(p => new RawSegment
            {
                Speaker = p.Speaker?.ToString(),
                StartSeconds = p.OffsetMilliseconds / 1000.0,
                EndSeconds = (p.OffsetMilliseconds + p.DurationMilliseconds) / 1000.0,
                Text = p.Text!.Trim()
            })
            .ToList();

        return new RawTranscript
        {
            Provider = "azure-speech-fast",
            Language = result.Phrases?.FirstOrDefault()?.Locale,
            DurationSeconds = result.DurationMilliseconds / 1000.0,
            Segments = segments
        };
    }
}
