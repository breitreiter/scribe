namespace Scribe.Models.Configuration;

public class AppSettings
{
    public CompletionSettings Completion { get; set; } = new();

    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Completion.AzureOpenAI.Endpoint))
            errors.Add("Completion.AzureOpenAI.Endpoint is required");

        if (string.IsNullOrWhiteSpace(Completion.AzureOpenAI.ApiKey))
            errors.Add("Completion.AzureOpenAI.ApiKey is required");

        if (string.IsNullOrWhiteSpace(Completion.AzureOpenAI.DeploymentName))
            errors.Add("Completion.AzureOpenAI.DeploymentName is required");

        return errors.Count == 0;
    }
}
