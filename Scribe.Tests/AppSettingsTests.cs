using Scribe.Models.Configuration;

namespace Scribe.Tests;

public class AppSettingsTests
{
    private static AppSettings ValidSettings() => new()
    {
        Transcription = new TranscriptionSettings
        {
            AzureSpeech = new AzureSpeechSettings
            {
                Endpoint = "https://example.cognitiveservices.azure.com",
                ApiKey = "test-key",
                Region = "eastus"
            }
        },
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
    public void MissingSpeechEndpoint_ReturnsError()
    {
        var settings = ValidSettings();
        settings.Transcription.AzureSpeech.Endpoint = "";

        settings.IsValid(out var errors);

        Assert.Contains(errors, e => e.Contains("Transcription.AzureSpeech.Endpoint"));
    }

    [Fact]
    public void MissingSpeechApiKey_ReturnsError()
    {
        var settings = ValidSettings();
        settings.Transcription.AzureSpeech.ApiKey = "";

        settings.IsValid(out var errors);

        Assert.Contains(errors, e => e.Contains("Transcription.AzureSpeech.ApiKey"));
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
    public void AllFieldsMissing_ReturnsMultipleErrors()
    {
        var settings = new AppSettings();

        settings.IsValid(out var errors);

        Assert.True(errors.Count >= 5);
    }

    [Fact]
    public void WhitespaceOnlyValues_TreatedAsMissing()
    {
        var settings = ValidSettings();
        settings.Transcription.AzureSpeech.ApiKey = "   ";

        settings.IsValid(out var errors);

        Assert.Contains(errors, e => e.Contains("Transcription.AzureSpeech.ApiKey"));
    }
}
