using Scribe.Services;

namespace Scribe.Models.Configuration;

public class AppSettings
{
    public CompletionSettings Completion { get; set; } = new();

    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        // Only the selected provider is validated; a machine using a local endpoint
        // has no reason to hold Azure credentials.
        switch (Completion.Provider)
        {
            case ChatClientFactory.AzureOpenAI:
                if (string.IsNullOrWhiteSpace(Completion.AzureOpenAI.Endpoint))
                    errors.Add("Completion.AzureOpenAI.Endpoint is required");

                if (string.IsNullOrWhiteSpace(Completion.AzureOpenAI.ApiKey))
                    errors.Add("Completion.AzureOpenAI.ApiKey is required");

                if (string.IsNullOrWhiteSpace(Completion.AzureOpenAI.DeploymentName))
                    errors.Add("Completion.AzureOpenAI.DeploymentName is required");
                break;

            case ChatClientFactory.OpenAI:
                if (string.IsNullOrWhiteSpace(Completion.OpenAI.Model))
                    errors.Add("Completion.OpenAI.Model is required");
                break;

            default:
                errors.Add($"Completion.Provider must be '{ChatClientFactory.AzureOpenAI}' or '{ChatClientFactory.OpenAI}' (was '{Completion.Provider}')");
                break;
        }

        return errors.Count == 0;
    }
}
