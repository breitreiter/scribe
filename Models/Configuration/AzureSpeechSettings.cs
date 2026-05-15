namespace Scribe.Models.Configuration;

public class AzureSpeechSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Region { get; set; } = "eastus";
    public string Locale { get; set; } = "en-US";
}
