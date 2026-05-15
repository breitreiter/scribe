namespace Scribe.Models.Configuration;

public class CompletionSettings
{
    public string Provider { get; set; } = "AzureOpenAI";
    public AzureOpenAISettings AzureOpenAI { get; set; } = new();
}
