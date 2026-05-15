using System.Text.Json;
using Scribe.Models;
using Serilog;

namespace Scribe.Services;

public class HtmlReportGenerator
{
    private const string TemplatePath = "Templates/transcript-template.html";

    public static async Task<string> GenerateHtmlReport(Transcript transcript, string outputPath)
    {
        Log.Information("Generating HTML report...");

        // Load template
        if (!File.Exists(TemplatePath))
        {
            throw new FileNotFoundException($"HTML template not found: {TemplatePath}");
        }

        var template = await File.ReadAllTextAsync(TemplatePath);

        // Serialize transcript to JSON
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false // Minified for embedding
        };
        var transcriptJson = JsonSerializer.Serialize(transcript, jsonOptions);

        // Embed JSON in template
        var html = template.Replace("{{TRANSCRIPT_JSON}}", transcriptJson);

        // Save HTML file
        await File.WriteAllTextAsync(outputPath, html);

        Log.Information("HTML report saved to: {OutputPath}", outputPath);

        return outputPath;
    }
}
