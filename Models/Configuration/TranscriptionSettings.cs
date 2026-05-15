namespace Scribe.Models.Configuration;

public class TranscriptionSettings
{
    public AzureSpeechSettings AzureSpeech { get; set; } = new();
}
