using Scribe.Models.Configuration;

namespace Scribe.Tests;

public class AppSettingsTests
{
    private static AppSettings ValidSettings() => new()
    {
        Completion = new CompletionSettings
        {
            AzureOpenAI = new AzureOpenAISettings
            {
                Endpoint = "https://example.openai.azure.com",
                ApiKey = "test-key",
                DeploymentName = "gpt-4o-mini"
            }
        }
    };

    [Fact]
    public void ValidSettings_IsValid_ReturnsTrue()
    {
        var settings = ValidSettings();

        var result = settings.IsValid(out var errors);

        Assert.True(result);
        Assert.Empty(errors);
    }

    [Fact]
    public void MissingOpenAIEndpoint_ReturnsError()
    {
        var settings = ValidSettings();
        settings.Completion.AzureOpenAI.Endpoint = "";

        settings.IsValid(out var errors);

        Assert.Contains(errors, e => e.Contains("Completion.AzureOpenAI.Endpoint"));
    }

    [Fact]
    public void MissingOpenAIApiKey_ReturnsError()
    {
        var settings = ValidSettings();
        settings.Completion.AzureOpenAI.ApiKey = "";

        settings.IsValid(out var errors);

        Assert.Contains(errors, e => e.Contains("Completion.AzureOpenAI.ApiKey"));
    }

    [Fact]
    public void MissingDeploymentName_ReturnsError()
    {
        var settings = ValidSettings();
        settings.Completion.AzureOpenAI.DeploymentName = "";

        settings.IsValid(out var errors);

        Assert.Contains(errors, e => e.Contains("Completion.AzureOpenAI.DeploymentName"));
    }

    [Fact]
    public void OpenAIProvider_DoesNotRequireAzureCredentials()
    {
        var settings = new AppSettings
        {
            Completion = new CompletionSettings
            {
                Provider = "OpenAI",
                OpenAI = new OpenAISettings { Endpoint = "http://imp:8080/v1", Model = "glm-4.6" }
            }
        };

        var result = settings.IsValid(out var errors);

        Assert.True(result);
        Assert.Empty(errors);
    }

    [Fact]
    public void OpenAIProvider_MissingModel_ReturnsError()
    {
        var settings = new AppSettings
        {
            Completion = new CompletionSettings { Provider = "OpenAI" }
        };

        settings.IsValid(out var errors);

        Assert.Contains(errors, e => e.Contains("Completion.OpenAI.Model"));
    }

    [Fact]
    public void OpenAIProvider_NeedsNoEndpoint_DefaultsToOpenAIProper()
    {
        var settings = new AppSettings
        {
            Completion = new CompletionSettings
            {
                Provider = "OpenAI",
                OpenAI = new OpenAISettings { Model = "gpt-4o-mini" }
            }
        };

        Assert.True(settings.IsValid(out _));
    }

    [Fact]
    public void UnknownProvider_ReturnsError()
    {
        var settings = ValidSettings();
        settings.Completion.Provider = "Ollama";

        settings.IsValid(out var errors);

        Assert.Contains(errors, e => e.Contains("Completion.Provider"));
    }

    [Fact]
    public void AllFieldsMissing_ReturnsMultipleErrors()
    {
        var settings = new AppSettings();

        settings.IsValid(out var errors);

        Assert.True(errors.Count >= 3);
    }

    [Fact]
    public void WhitespaceOnlyValues_TreatedAsMissing()
    {
        var settings = ValidSettings();
        settings.Completion.AzureOpenAI.ApiKey = "   ";

        settings.IsValid(out var errors);

        Assert.Contains(errors, e => e.Contains("Completion.AzureOpenAI.ApiKey"));
    }
}
