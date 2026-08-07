# Scribe

A CLI tool that turns a raw meeting transcription into an enriched, retrieval-ready meeting record.

## Overview

**Scribe does not transcribe audio.** You produce the raw transcription yourself
with WhisperX (see [docs/generating-transcripts.md](docs/generating-transcripts.md)),
and Scribe enriches it:

- Merges diarized fragments into clean speaker turns with timestamps
- Generates an AI summary with key points and action items, grounded in specific turns
- Emits a structured meeting record built to be read by a model doing retrieval

Keeping transcription out of the tool is deliberate. It means the audio never has
to leave the machine it was recorded on, ASR can run wherever the GPU is, and
Scribe has no opinion about which ASR you used — it reads the JSON.

## Prerequisites

- .NET 8.0 or later
- A raw transcription produced by WhisperX (or a legacy Azure Speech Fast Transcription response)
- A chat model for the summary pass: Azure OpenAI, or any OpenAI-compatible
  endpoint (llama.cpp, vLLM, Ollama, LM Studio, a router, or api.openai.com)

## Setup

1. **Install dependencies**
   ```bash
   dotnet restore
   ```

2. **Configure settings**
   ```bash
   cp appsettings.example.json appsettings.json
   ```
   Edit `appsettings.json` and configure the summariser. `Completion.Provider`
   selects between two paths, and only the selected one is validated — a machine
   using a local model needs no Azure credentials:

   ```jsonc
   // Azure OpenAI (uses the Responses API, so reasoning models work)
   "Completion": {
     "Provider": "AzureOpenAI",
     "AzureOpenAI": { "Endpoint": "...", "ApiKey": "...", "DeploymentName": "gpt-4o-mini" }
   }

   // Any OpenAI-compatible endpoint. Endpoint includes the version segment;
   // omit it entirely to talk to api.openai.com. ApiKey may be blank for
   // local servers that ignore it.
   "Completion": {
     "Provider": "OpenAI",
     "OpenAI": { "Endpoint": "http://localhost:8080/v1", "ApiKey": "", "Model": "my-model" }
   }
   ```

3. **Build**
   ```bash
   dotnet build
   ```

## Usage

First, produce a transcription — see [docs/generating-transcripts.md](docs/generating-transcripts.md).
Put its JSON in a directory for the meeting, then:

```bash
dotnet run -- <path-to-meeting-directory>
```

Scribe looks for `raw-transcription.json` in that directory (falling back to
`fast-transcription-raw.json` for meetings transcribed before the pivot). If you
don't provide a path, Scribe prompts for one.

Re-running is cheap and idempotent: an existing summary is reused rather than
regenerated, so you can iterate on the output format without paying for the AI pass.

## Output

Scribe writes into the meeting directory:

- **`transcript.json`** — structured data (metadata, summary, turns)
- `scribe.log` — log file (only if warnings/errors occurred)

## Features

- ✅ Speaker turn formatting from diarized ASR output
- ✅ AI-generated summaries with grounded key points and action items
- ✅ Summarisation on a local model or Azure OpenAI, selected by config
- ✅ Idempotent re-runs for fast iteration on output format
- ⏳ Meeting markdown record (`<date>-<slug>.md`) built for RAG — see `plans/llm-native-output.md`
- ⏳ Interactive speaker name assignment
- ⏳ AI-generated topic segmentation

## Development

Built with:
- C# / .NET 8.0
- [Spectre.Console](https://spectreconsole.net/) for CLI UI
- [Serilog](https://serilog.net/) for logging
- [Azure.AI.OpenAI](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/openai/Azure.AI.OpenAI) for AI services

## License

_License information to be added_
