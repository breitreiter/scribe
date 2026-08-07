namespace Scribe.Models.Configuration;

/// <summary>
/// Any OpenAI-compatible chat completions endpoint: llama.cpp, vLLM, Ollama,
/// LM Studio, a gateway, or api.openai.com itself.
/// </summary>
public class OpenAISettings
{
    /// <summary>Base URL including the version segment, e.g. "http://imp:8080/v1". Empty targets api.openai.com.</summary>
    public string? Endpoint { get; set; }

    /// <summary>Most local servers ignore this, but the SDK requires a non-empty value.</summary>
    public string? ApiKey { get; set; }

    public string Model { get; set; } = string.Empty;
}
