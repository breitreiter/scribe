namespace Scribe.Models.Configuration;

public class CompletionSettings
{
    /// <summary>"AzureOpenAI" or "OpenAI" (any OpenAI-compatible endpoint).</summary>
    public string Provider { get; set; } = "AzureOpenAI";

    public AzureOpenAISettings AzureOpenAI { get; set; } = new();

    public OpenAISettings OpenAI { get; set; } = new();
}
