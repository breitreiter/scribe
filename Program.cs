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
    // Raw transcription filenames, newest convention first. The legacy name is
    // Azure-specific and only kept so pre-pivot meeting directories still open.
    private static readonly string[] RawTranscriptionNames =
    {
        "raw-transcription.json",
        "fast-transcription-raw.json"
    };

    static async Task<int> Main(string[] args)
    {
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

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(settingsFile, optional: false, reloadOnChange: false)
            .Build();

        var appSettings = new AppSettings();
        configuration.Bind(appSettings);

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

        string? inputPath = args.Length > 0 ? args[0] : null;

        if (string.IsNullOrWhiteSpace(inputPath))
        {
            inputPath = AnsiConsole.Ask<string>("Please enter the path to the meeting directory:");
        }

        if (!Directory.Exists(inputPath))
        {
            ConsoleHelper.WriteError($"Directory '{inputPath}' not found.");
            AnsiConsole.MarkupLine("Scribe enriches an existing transcription; it does not transcribe audio.");
            AnsiConsole.MarkupLine("See [cyan]docs/generating-transcripts.md[/] for how to produce the raw transcription first.");
            return 1;
        }

        var outputDirectory = Path.GetFullPath(inputPath);
        var logFilePath = Path.Combine(outputDirectory, "scribe.log");

        try
        {
            AnsiConsole.MarkupLine("[green]Scribe[/] - Meeting transcript enrichment");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"  Meeting directory: [cyan]{outputDirectory.EscapeMarkup()}[/]");

            var logLevel = configuration["Logging:LogLevel:Default"] ?? "Warning";
            var minimumLevel = logLevel switch
            {
                "Debug" => Serilog.Events.LogEventLevel.Debug,
                "Information" => Serilog.Events.LogEventLevel.Information,
                "Warning" => Serilog.Events.LogEventLevel.Warning,
                "Error" => Serilog.Events.LogEventLevel.Error,
                _ => Serilog.Events.LogEventLevel.Warning
            };

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(minimumLevel)
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Infinite)
                .CreateLogger();

            Log.Information("Scribe started for meeting directory: {Directory}", outputDirectory);

            var rawJsonPath = RawTranscriptionNames
                .Select(name => Path.Combine(outputDirectory, name))
                .FirstOrDefault(File.Exists);

            if (rawJsonPath == null)
            {
                ConsoleHelper.WriteError($"No raw transcription found in {outputDirectory}");
                AnsiConsole.MarkupLine($"Expected one of: [cyan]{string.Join("[/], [cyan]", RawTranscriptionNames)}[/]");
                AnsiConsole.MarkupLine("See [cyan]docs/generating-transcripts.md[/] for how to produce one.");
                return 1;
            }

            var transcriptPath = Path.Combine(outputDirectory, "transcript.json");
            Transcript? transcript = null;

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
            }

            if (transcript == null)
            {
                AnsiConsole.MarkupLine("Formatting transcript...");
                transcript = await LoadAndFormatRawAsync(rawJsonPath, transcriptPath);
                if (transcript == null) return 1;
            }

            if (transcript.Summary?.KeyPoints == null || transcript.Summary.KeyPoints.Count == 0)
            {
                AnsiConsole.MarkupLine("Generating AI summary...");
                try
                {
                    var summaryService = new SummaryService(appSettings.Completion.AzureOpenAI);
                    var summary = await summaryService.GenerateSummaryAsync(transcript);
                    transcript.Summary = summary;

                    if (!string.IsNullOrWhiteSpace(summary.OneLiner))
                    {
                        var title = summary.OneLiner.TrimEnd('.', '!', '?').Trim();
                        if (title.Length > 100) title = title[..100].TrimEnd();
                        transcript.Metadata.MeetingTitle = title;
                    }

                    await File.WriteAllTextAsync(transcriptPath,
                        JsonSerializer.Serialize(transcript, Json.Indented));
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

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]✓[/] Enrichment complete!");

            if (!string.IsNullOrEmpty(transcript.Metadata.MeetingTitle))
                AnsiConsole.MarkupLine($"  Meeting: [bold]{transcript.Metadata.MeetingTitle.EscapeMarkup()}[/]");

            AnsiConsole.MarkupLine($"  Duration: [cyan]{transcript.Metadata.DurationSeconds:F1}s[/]");
            AnsiConsole.MarkupLine($"  Speakers: [cyan]{transcript.Metadata.SpeakerCount}[/]");
            AnsiConsole.MarkupLine($"  Turns: [cyan]{transcript.Turns.Count}[/]");
            AnsiConsole.MarkupLine($"  Transcript: [cyan]{transcriptPath.EscapeMarkup()}[/]");
            AnsiConsole.WriteLine();

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

            if (File.Exists(logFilePath) && new FileInfo(logFilePath).Length == 0)
            {
                File.Delete(logFilePath);
            }
        }
    }

    // Returns null and prints an error if the raw JSON cannot be parsed.
    private static async Task<Transcript?> LoadAndFormatRawAsync(string rawJsonPath, string transcriptPath)
    {
        var rawJson = await File.ReadAllTextAsync(rawJsonPath);

        RawTranscript raw;
        try
        {
            raw = RawTranscriptReader.Read(rawJson);
        }
        catch (Exception ex) when (ex is InvalidDataException or JsonException)
        {
            ConsoleHelper.WriteError(ex.Message);
            return null;
        }

        Log.Information("Read {Count} segments from {Provider} transcription",
            raw.Segments.Count, raw.Provider);

        var transcript = TranscriptFormatter.FormatTranscript(raw);
        await File.WriteAllTextAsync(transcriptPath, JsonSerializer.Serialize(transcript, Json.Indented));
        Log.Information("Formatted transcript saved to: {TranscriptPath}", transcriptPath);
        return transcript;
    }
}
