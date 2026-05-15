using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Scribe.Models.Configuration;
using Scribe.Utils;
using Serilog;

namespace Scribe.Services;

public class AzureSpeechFastService
{
    private static readonly HttpClient _httpClient = new();
    private readonly AzureSpeechSettings _settings;

    public AzureSpeechFastService(AzureSpeechSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<FastTranscriptionResult> TranscribeAsync(
        string audioFilePath,
        int maxSpeakers = 10,
        IProgress<string>? progress = null)
    {
        Log.Information("Starting Fast Transcription for file: {FilePath}", audioFilePath);
        progress?.Report("Uploading audio and starting transcription...");

        try
        {
            var endpoint = $"https://{_settings.Region}.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2024-11-15";
            Log.Information("Fast Transcription endpoint: {Endpoint}", endpoint);

            var audioBytes = await File.ReadAllBytesAsync(audioFilePath);
            Log.Information("Audio file size: {Size} bytes", audioBytes.Length);

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _settings.ApiKey);

            using var content = new MultipartFormDataContent();

            var audioContent = new ByteArrayContent(audioBytes);
            audioContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(audioContent, "audio", Path.GetFileName(audioFilePath));

            var definition = new
            {
                locales = new[] { _settings.Locale },
                diarization = new
                {
                    enabled = true,
                    maxSpeakers = maxSpeakers
                }
            };

            var definitionJson = JsonSerializer.Serialize(definition);
            Log.Debug("Transcription definition: {Definition}", definitionJson);

            var definitionContent = new StringContent(definitionJson, Encoding.UTF8, "application/json");
            content.Add(definitionContent, "definition");

            request.Content = content;

            progress?.Report("Transcribing audio...");
            Log.Information("Sending transcription request...");

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Log.Error("Fast Transcription failed with status {StatusCode}: {Response}",
                    response.StatusCode, responseBody);
                throw new HttpRequestException($"Fast Transcription failed with status {response.StatusCode}: {responseBody}");
            }

            Log.Information("Fast Transcription completed successfully");
            progress?.Report("Processing transcription results...");

            var result = JsonSerializer.Deserialize<FastTranscriptionResult>(responseBody, Json.CaseInsensitive);

            if (result == null)
                throw new InvalidOperationException("Failed to deserialize transcription result");

            Log.Information("Transcription duration: {Duration}ms, Phrases: {PhraseCount}",
                result.DurationMilliseconds, result.Phrases?.Length ?? 0);

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during Fast Transcription");
            throw;
        }
    }
}

// Response models for Fast Transcription API
public class FastTranscriptionResult
{
    [JsonPropertyName("durationMilliseconds")]
    public long DurationMilliseconds { get; set; }

    [JsonPropertyName("combinedPhrases")]
    public CombinedPhrase[]? CombinedPhrases { get; set; }

    [JsonPropertyName("phrases")]
    public Phrase[]? Phrases { get; set; }
}

public class CombinedPhrase
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("channel")]
    public int? Channel { get; set; }
}

public class Phrase
{
    [JsonPropertyName("offsetMilliseconds")]
    public long OffsetMilliseconds { get; set; }

    [JsonPropertyName("durationMilliseconds")]
    public long DurationMilliseconds { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("speaker")]
    public int? Speaker { get; set; }

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("words")]
    public Word[]? Words { get; set; }
}

public class Word
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("offsetMilliseconds")]
    public long OffsetMilliseconds { get; set; }

    [JsonPropertyName("durationMilliseconds")]
    public long DurationMilliseconds { get; set; }

    [JsonPropertyName("confidence")]
    public double? Confidence { get; set; }
}
