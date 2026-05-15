using Microsoft.Extensions.Configuration;
using Scribe.Models;
using Scribe.Models.Configuration;
using Scribe.Services;
using Scribe.Utils;
using Serilog;
using Spectre.Console;
using System.Text.Json;

namespace Scribe;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Check if appsettings.json exists
        const string settingsFile = "appsettings.json";
        const string exampleFile = "appsettings.example.json";

        if (!File.Exists(settingsFile))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Configuration file '[yellow]appsettings.json[/]' not found.");
            AnsiConsole.MarkupLine($"Please copy '[yellow]{exampleFile}[/]' to '[yellow]{settingsFile}[/]' and configure your settings.");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("You can do this by running:");
            AnsiConsole.MarkupLine($"  [cyan]cp {exampleFile} {settingsFile}[/]");
            return 1;
        }

        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(settingsFile, optional: false, reloadOnChange: false)
            .Build();

        var appSettings = new AppSettings();
        configuration.Bind(appSettings);

        // Validate configuration
        if (!appSettings.IsValid(out var errors))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Configuration is invalid:");
            foreach (var error in errors)
            {
                AnsiConsole.MarkupLine($"  [red]•[/] {error}");
            }
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"Please check your '[yellow]{settingsFile}[/]' file.");
            return 1;
        }

        // Get input path (audio file or directory for reprocessing)
        string? inputPath = args.Length > 0 ? args[0] : null;

        if (string.IsNullOrWhiteSpace(inputPath))
        {
            inputPath = AnsiConsole.Ask<string>("Please enter the path to the audio file (or directory to reprocess):");
        }

        // Determine if this is a file or directory input
        bool isDirectoryInput = Directory.Exists(inputPath);
        bool isFileInput = File.Exists(inputPath);

        if (!isDirectoryInput && !isFileInput)
        {
            ConsoleHelper.WriteError($"Path '{inputPath}' not found.");
            return 1;
        }

        string audioFilePath = string.Empty;
        string? existingDirectory = null;

        if (isDirectoryInput)
        {
            existingDirectory = Path.GetFullPath(inputPath);
            AnsiConsole.MarkupLine("[yellow]Directory mode:[/] Reprocessing existing transcription");
            AnsiConsole.MarkupLine($"  Directory: [cyan]{existingDirectory.EscapeMarkup()}[/]");
        }
        else
        {
            audioFilePath = inputPath;
        }

        // Output directory will be created based on meeting title
        // For now, we'll defer logger initialization until we have the actual directory
        string? outputDirectory = null;
        string? logFilePath = null;

        try
        {
            AnsiConsole.MarkupLine("[green]Scribe[/] - Meeting transcription and summary tool");
            AnsiConsole.WriteLine();

            if (!isDirectoryInput)
            {
                AnsiConsole.Write("Audio file: ");
                AnsiConsole.MarkupLine($"[cyan]{audioFilePath.EscapeMarkup()}[/]");
                AnsiConsole.WriteLine();

                // TODO: Get meeting title from user and create output directory
                // TODO: Initialize logger once we have the output directory
                // For now, create a temporary output directory for testing
                var sanitizedTitle = "temp-meeting"; // Will be replaced with actual sanitized title
                outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), sanitizedTitle);
                Directory.CreateDirectory(outputDirectory);
            }
            else
            {
                outputDirectory = existingDirectory!;
            }

            // Initialize Serilog with configuration from appsettings
            var logLevel = configuration["Logging:LogLevel:Default"] ?? "Warning";
            var minimumLevel = logLevel switch
            {
                "Debug" => Serilog.Events.LogEventLevel.Debug,
                "Information" => Serilog.Events.LogEventLevel.Information,
                "Warning" => Serilog.Events.LogEventLevel.Warning,
                "Error" => Serilog.Events.LogEventLevel.Error,
                _ => Serilog.Events.LogEventLevel.Warning
            };

            logFilePath = Path.Combine(outputDirectory, "scribe.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(minimumLevel)
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Infinite)
                .CreateLogger();

            Scribe.Models.TranscriptionResult transcription;

            if (isDirectoryInput)
            {
                // Directory mode: Load and reformat existing raw JSON
                Log.Information("Scribe started in reprocessing mode for directory: {Directory}", outputDirectory);

                var rawJsonPath = Path.Combine(outputDirectory, "fast-transcription-raw.json");
                if (!File.Exists(rawJsonPath))
                {
                    ConsoleHelper.WriteError($"Raw transcription file not found: {rawJsonPath}");
                    AnsiConsole.MarkupLine("Expected to find '[cyan]fast-transcription-raw.json[/]' in the directory.");
                    return 1;
                }

                AnsiConsole.MarkupLine("Loading existing transcription...");

                var transcriptPath = Path.Combine(outputDirectory, "transcript.json");
                Transcript? transcript;

                if (File.Exists(transcriptPath))
                {
                    var existingTranscript = JsonSerializer.Deserialize<Transcript>(
                        await File.ReadAllTextAsync(transcriptPath), Json.CaseInsensitive);

                    if (existingTranscript?.Summary?.KeyPoints?.Count > 0)
                    {
                        transcript = existingTranscript;
                        AnsiConsole.MarkupLine("[cyan]ℹ[/] Using existing formatted transcript with summary");
                        Log.Information("Loaded existing transcript with {KeyPointCount} key points", transcript.Summary.KeyPoints.Count);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("Formatting transcript...");
                        transcript = await LoadAndFormatRawAsync(rawJsonPath, transcriptPath);
                        if (transcript == null) return 1;
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("Formatting transcript...");
                    transcript = await LoadAndFormatRawAsync(rawJsonPath, transcriptPath);
                    if (transcript == null) return 1;
                }

                // Generate AI summary (only if not already present)
                if (transcript.Summary?.KeyPoints == null || transcript.Summary.KeyPoints.Count == 0)
                {
                    AnsiConsole.MarkupLine("Generating AI summary...");
                    try
                    {
                        var summaryService = new SummaryService(appSettings.Completion.AzureOpenAI);
                        var summary = await summaryService.GenerateSummaryAsync(transcript);
                        transcript.Summary = summary;

                        // Re-save the transcript with the updated summary
                        var updatedJson = System.Text.Json.JsonSerializer.Serialize(transcript,
                            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        await File.WriteAllTextAsync(transcriptPath, updatedJson);

                        Log.Information("AI summary generated and saved");
                        AnsiConsole.MarkupLine("[green]✓[/] AI summary generated");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to generate AI summary");
                        AnsiConsole.MarkupLine("[yellow]![/] Could not generate AI summary (continuing anyway)");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[cyan]ℹ[/] Using existing AI summary");
                    Log.Information("Skipping summary generation - already exists with {KeyPointCount} key points",
                        transcript.Summary.KeyPoints.Count);
                }

                // Generate HTML report (after summary is added)
                AnsiConsole.MarkupLine("Generating HTML report...");
                var htmlPath = Path.Combine(outputDirectory, "transcript.html");
                await HtmlReportGenerator.GenerateHtmlReport(transcript, htmlPath);

                // Create a simple TranscriptionResult for display
                transcription = new Scribe.Models.TranscriptionResult
                {
                    Provider = "AzureSpeechFast",
                    DurationSeconds = transcript.Metadata.DurationSeconds,
                    Language = appSettings.Transcription.AzureSpeech.Locale,
                    Segments = new List<Scribe.Models.TranscriptionSegment>(), // Not needed for display
                    RawJsonPath = rawJsonPath,
                    TranscriptPath = transcriptPath,
                    HtmlPath = htmlPath
                };
            }
            else
            {
                // File mode: Full transcription workflow
                Log.Information("Scribe started for audio file: {AudioFile}", audioFilePath);

                // Ask for number of speakers
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold yellow]Speaker Diarization Setup[/]");
                var maxSpeakers = AnsiConsole.Prompt(
                    new TextPrompt<int>("How many [cyan]speakers[/] do you expect in this recording?")
                        .DefaultValue(5)
                        .ValidationErrorMessage("[red]Please enter a valid number between 1 and 10[/]")
                        .Validate(n => n >= 1 && n <= 10
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[red]Speaker count must be between 1 and 10[/]")));

                Log.Information("Speaker diarization configured: maxSpeakers={MaxSpeakers}", maxSpeakers);
                AnsiConsole.WriteLine();

                // Transcribe audio file using Azure Speech Fast Transcription
                var transcriptionService = new TranscriptionService(
                    appSettings.Transcription,
                    appSettings.Completion);

                transcription = await AnsiConsole.Status()
                    .StartAsync("Processing transcription...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        ctx.SpinnerStyle(Style.Parse("green"));

                        return await transcriptionService.TranscribeAsync(
                            audioFilePath,
                            outputDirectory,
                            maxSpeakers,
                            (status, elapsed) =>
                            {
                                ctx.Status($"Transcription status: [cyan]{status}[/]");
                            });
                    });
            }

            if (isDirectoryInput)
            {
                AnsiConsole.MarkupLine("[green]✓[/] Reprocessing complete!");
            }
            else
            {
                AnsiConsole.MarkupLine("[green]✓[/] Transcription complete!");
                if (!transcription.SummaryGenerated)
                    AnsiConsole.MarkupLine("[yellow]![/] AI summary could not be generated — check scribe.log for details.");
            }

            AnsiConsole.MarkupLine($"  Duration: [cyan]{transcription.DurationSeconds:F1}s[/]");
            AnsiConsole.MarkupLine($"  Language: [cyan]{transcription.Language}[/]");

            if (!isDirectoryInput)
            {
                AnsiConsole.MarkupLine($"  Segments: [cyan]{transcription.Segments.Count}[/]");
                var uniqueSpeakers = transcription.Segments.Select(s => s.SpeakerId).Distinct().Count();
                AnsiConsole.MarkupLine($"  Speakers identified: [cyan]{uniqueSpeakers}[/]");
            }

            AnsiConsole.MarkupLine($"  Transcript JSON: [cyan]{transcription.TranscriptPath}[/]");
            AnsiConsole.MarkupLine($"  HTML Report: [cyan]{transcription.HtmlPath}[/]");
            AnsiConsole.WriteLine();

            // TODO: Ask for meeting title and purpose
            // TODO: Walk user through speaker name assignment
            // TODO: Generate formatted transcript
            // TODO: Generate summary

            AnsiConsole.MarkupLine("[yellow]Note:[/] Additional workflow steps not yet implemented.");
            AnsiConsole.Write("Raw transcription saved to: ");
            AnsiConsole.MarkupLine($"[cyan]{transcription.RawJsonPath.EscapeMarkup()}[/]");

            return 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during execution");
            ConsoleHelper.WriteError(ex.Message);
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();

            // Delete log file if empty (per requirements)
            if (logFilePath != null && File.Exists(logFilePath) && new FileInfo(logFilePath).Length == 0)
            {
                File.Delete(logFilePath);
            }
        }
    }

    // Returns null and prints an error if the raw JSON cannot be parsed.
    private static async Task<Transcript?> LoadAndFormatRawAsync(string rawJsonPath, string transcriptPath)
    {
        var rawJson = await File.ReadAllTextAsync(rawJsonPath);
        var fastResult = JsonSerializer.Deserialize<FastTranscriptionResult>(rawJson, Json.CaseInsensitive);

        if (fastResult == null)
        {
            ConsoleHelper.WriteError("Failed to parse raw transcription JSON");
            return null;
        }

        var transcript = TranscriptFormatter.FormatTranscript(fastResult);
        await File.WriteAllTextAsync(transcriptPath, JsonSerializer.Serialize(transcript, Json.Indented));
        Log.Information("Formatted transcript saved to: {TranscriptPath}", transcriptPath);
        return transcript;
    }
}