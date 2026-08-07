using System.ClientModel;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;
using Scribe.Models.Configuration;
using Serilog;

namespace Scribe.Services;

public static class ChatClientFactory
{
    public const string AzureOpenAI = "AzureOpenAI";
    public const string OpenAI = "OpenAI";

    public static IChatClient Create(CompletionSettings settings) =>
        settings.Provider switch
        {
            AzureOpenAI => CreateAzure(settings.AzureOpenAI),
            OpenAI => CreateOpenAICompatible(settings.OpenAI),
            _ => throw new InvalidOperationException(
                $"Unknown completion provider '{settings.Provider}'. Expected '{AzureOpenAI}' or '{OpenAI}'.")
        };

    private static IChatClient CreateAzure(AzureOpenAISettings settings)
    {
        var parsed = new Uri(settings.Endpoint);
        var baseUri = new Uri($"{parsed.Scheme}://{parsed.Authority}/");

        var clientOptions = new AzureOpenAIClientOptions(AzureOpenAIClientOptions.ServiceVersion.V2025_03_01_Preview);
        var azureClient = new AzureOpenAIClient(baseUri, new AzureKeyCredential(settings.ApiKey), clientOptions);

        var model = settings.ModelName ?? settings.DeploymentName;
        Log.Information("Completion provider: Azure OpenAI, model {Model}", model);

        // Responses API rather than chat completions: o4-mini spends reasoning tokens here.
        return azureClient.GetResponsesClient().AsIChatClient(model);
    }

    private static IChatClient CreateOpenAICompatible(OpenAISettings settings)
    {
        // Local servers generally ignore the key, but the SDK rejects an empty one.
        var apiKey = string.IsNullOrWhiteSpace(settings.ApiKey) ? "local" : settings.ApiKey;

        var chatClient = string.IsNullOrWhiteSpace(settings.Endpoint)
            ? new OpenAI.Chat.ChatClient(settings.Model, new ApiKeyCredential(apiKey))
            : new OpenAI.Chat.ChatClient(settings.Model, new ApiKeyCredential(apiKey),
                new OpenAIClientOptions { Endpoint = new Uri(settings.Endpoint) });

        Log.Information("Completion provider: OpenAI-compatible at {Endpoint}, model {Model}",
            settings.Endpoint ?? "api.openai.com", settings.Model);

        return chatClient.AsIChatClient();
    }
}
