namespace Scribe.Models.Configuration;

public class AppSettings
{
    public TranscriptionSettings Transcription { get; set; } = new();
    public CompletionSettings Completion { get; set; } = new();

    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        // Validate Transcription settings (Azure Speech Fast Transcription)
        if (string.IsNullOrWhiteSpace(Transcription.AzureSpeech.Endpoint))
            errors.Add("Transcription.AzureSpeech.Endpoint is required");

        if (string.IsNullOrWhiteSpace(Transcription.AzureSpeech.ApiKey))
            errors.Add("Transcription.AzureSpeech.ApiKey is required");

        // Validate Completion settings
        if (string.IsNullOrWhiteSpace(Completion.AzureOpenAI.Endpoint))
            errors.Add("Completion.AzureOpenAI.Endpoint is required");

        if (string.IsNullOrWhiteSpace(Completion.AzureOpenAI.ApiKey))
            errors.Add("Completion.AzureOpenAI.ApiKey is required");

        if (string.IsNullOrWhiteSpace(Completion.AzureOpenAI.DeploymentName))
            errors.Add("Completion.AzureOpenAI.DeploymentName is required");

        return errors.Count == 0;
    }
}
