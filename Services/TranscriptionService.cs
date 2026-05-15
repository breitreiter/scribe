using Scribe.Models;
using Scribe.Models.Configuration;
using Scribe.Utils;
using Serilog;
using System.Text.Json;

namespace Scribe.Services;

public class TranscriptionService
{
    private readonly TranscriptionSettings _settings;
    private readonly CompletionSettings? _completionSettings;
    private readonly AzureSpeechFastService _azureSpeechFastService;
    private readonly SummaryService? _summaryService;

    public TranscriptionService(TranscriptionSettings settings, CompletionSettings? completionSettings = null)
    {
        _settings = settings;
        _completionSettings = completionSettings;

        _azureSpeechFastService = new AzureSpeechFastService(_settings.AzureSpeech);

        // Initialize summary service if completion settings are provided
        if (_completionSettings != null)
        {
            _summaryService = new SummaryService(_completionSettings.AzureOpenAI);
            Log.Information("SummaryService initialized");
        }

        Log.Information("TranscriptionService initialized with Azure Speech Fast Transcription");
        Log.Information("  Endpoint: {Endpoint}", _settings.AzureSpeech.Endpoint);
        Log.Information("  Region: {Region}", _settings.AzureSpeech.Region);
        Log.Information("  Locale: {Locale}", _settings.AzureSpeech.Locale);
    }

    public async Task<TranscriptionResult> TranscribeAsync(
        string audioFilePath,
        string outputDirectory,
        int maxSpeakers = 5,
        Action<string, TimeSpan>? progressCallback = null)
    {
        Log.Information("Starting fast transcription for: {AudioFile}", audioFilePath);

        // Validate file exists and has content
        var fileInfo = new FileInfo(audioFilePath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException($"Audio file not found: {audioFilePath}");
        }

        if (fileInfo.Length == 0)
        {
            throw new ArgumentException($"Audio file is empty (0 bytes): {audioFilePath}. Please provide a valid audio file.");
        }

        // Azure Speech Fast Transcription has a 300MB limit
        const long maxFileSizeBytes = 300 * 1024 * 1024; // 300 MB
        if (fileInfo.Length > maxFileSizeBytes)
        {
            throw new ArgumentException($"Audio file is too large ({fileInfo.Length / 1024.0 / 1024.0:F2} MB). Maximum size is 300 MB.");
        }

        // Validate file format
        var supportedFormats = new[] { ".flac", ".m4a", ".mp3", ".mp4", ".mpeg", ".mpga", ".oga", ".ogg", ".wav", ".webm", ".wma", ".aac", ".amr", ".speex" };
        var fileExtension = Path.GetExtension(audioFilePath).ToLowerInvariant();

        if (!supportedFormats.Contains(fileExtension))
        {
            throw new ArgumentException($"Unsupported audio format: {fileExtension}. Supported formats: {string.Join(", ", supportedFormats)}");
        }

        var fileName = Path.GetFileName(audioFilePath);
        var fileSizeMB = fileInfo.Length / 1024.0 / 1024.0;

        Log.Information("File details:");
        Log.Information("  File name: {FileName}", fileName);
        Log.Information("  File size: {Size:F2} MB", fileSizeMB);
        Log.Information("  File extension: {Extension}", fileExtension);
        Log.Information("  Max speakers: {Max}", maxSpeakers);

        // Create progress reporter
        IProgress<string>? progress = null;
        if (progressCallback != null)
        {
            progress = new Progress<string>(status => progressCallback(status, TimeSpan.Zero));
        }

        // Call Fast Transcription API
        Log.Information("Calling Fast Transcription API...");
        var fastResult = await _azureSpeechFastService.TranscribeAsync(
            audioFilePath,
            maxSpeakers,
            progress);

        // Save raw transcription result
        var rawJsonPath = Path.Combine(outputDirectory, "fast-transcription-raw.json");
        await SaveFastTranscriptionAsync(fastResult, rawJsonPath);
        Log.Information("Raw transcription saved to: {RawJsonPath}", rawJsonPath);

        // Format and save human/LLM-friendly transcript
        var transcript = TranscriptFormatter.FormatTranscript(fastResult);
        var transcriptPath = Path.Combine(outputDirectory, "transcript.json");
        await SaveTranscriptAsync(transcript, transcriptPath);
        Log.Information("Formatted transcript saved to: {TranscriptPath}", transcriptPath);

        // Generate HTML report
        var htmlPath = Path.Combine(outputDirectory, "transcript.html");
        await HtmlReportGenerator.GenerateHtmlReport(transcript, htmlPath);

        // Generate AI summary if summary service is available
        bool summaryGenerated = false;
        string? meetingTitle = null;
        if (_summaryService != null)
        {
            Log.Information("Generating AI summary...");
            try
            {
                var summary = await _summaryService.GenerateSummaryAsync(transcript);
                transcript.Summary = summary;

                if (!string.IsNullOrWhiteSpace(summary.OneLiner))
                {
                    meetingTitle = summary.OneLiner.TrimEnd('.', '!', '?').Trim();
                    if (meetingTitle.Length > 100) meetingTitle = meetingTitle[..100].TrimEnd();
                    transcript.Metadata.MeetingTitle = meetingTitle;
                }

                await SaveTranscriptAsync(transcript, transcriptPath);
                await HtmlReportGenerator.GenerateHtmlReport(transcript, htmlPath);
                Log.Information("AI summary generated and saved");
                summaryGenerated = true;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to generate AI summary, continuing without it");
            }
        }

        // Convert to unified transcription result
        var result = ConvertFastResultToTranscriptionResult(fastResult, rawJsonPath);
        result.TranscriptPath = transcriptPath;
        result.HtmlPath = htmlPath;
        result.SummaryGenerated = summaryGenerated;
        result.MeetingTitle = meetingTitle;

        Log.Information("Transcription complete!");
        Log.Information("  Duration: {Duration:F1}s", result.DurationSeconds);
        Log.Information("  Segments: {Count}", result.Segments.Count);
        Log.Information("  Unique speakers: {Speakers}", result.Segments.Select(s => s.SpeakerId).Distinct().Count());

        return result;
    }

    private static async Task SaveFastTranscriptionAsync(FastTranscriptionResult result, string filePath)
    {
        var json = JsonSerializer.Serialize(result, Json.IndentedCamelCase);
        await File.WriteAllTextAsync(filePath, json);
    }

    private static async Task SaveTranscriptAsync(Transcript transcript, string filePath)
    {
        var json = JsonSerializer.Serialize(transcript, Json.Indented);
        await File.WriteAllTextAsync(filePath, json);
    }

    private TranscriptionResult ConvertFastResultToTranscriptionResult(FastTranscriptionResult fastResult, string rawJsonPath)
    {
        var durationSeconds = fastResult.DurationMilliseconds / 1000.0;

        var segments = (fastResult.Phrases ?? Array.Empty<Phrase>())
            .Where(p => !string.IsNullOrWhiteSpace(p.Text))
            .Select(p => new TranscriptionSegment
            {
                SpeakerId = p.Speaker ?? 0,
                StartSeconds = p.OffsetMilliseconds / 1000.0,
                EndSeconds = (p.OffsetMilliseconds + p.DurationMilliseconds) / 1000.0,
                Text = p.Text ?? string.Empty,
                Confidence = p.Confidence
            })
            .ToList();

        return new TranscriptionResult
        {
            Provider = "AzureSpeechFast",
            DurationSeconds = durationSeconds,
            Language = _settings.AzureSpeech.Locale,
            Segments = segments,
            RawJsonPath = rawJsonPath
        };
    }
}
