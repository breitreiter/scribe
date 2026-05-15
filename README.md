# Scribe

A CLI tool for generating transcripts and summaries from meeting audio recordings.

## Overview

Scribe processes audio files from meetings and generates:
- Clean, formatted transcripts with speaker identification and timestamps
- AI-generated summaries with grounded key points and action items
- Interactive HTML reports with clickable links from summary to transcript

## Quick Start

**Try the sample output first:** Open `samples/generative-ui-meeting/transcript.html` in your browser to see what Scribe produces, then read the [sample README](samples/generative-ui-meeting/README.md) for details.

## Prerequisites

- .NET 8.0 or later
- Azure account with access to:
  - **Azure AI Speech** (Fast Transcription with speaker diarization)
  - **Azure OpenAI** (o4-mini for AI summaries)

## Setup

1. **Clone the repository** (or navigate to the project directory)

2. **Install dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure settings**

   Copy the example configuration file:
   ```bash
   cp appsettings.example.json appsettings.json
   ```

   Edit `appsettings.json` and add your Azure credentials:
   - **Transcription.AzureSpeech**: Endpoint, API Key, Region, Locale
   - **Completion.AzureOpenAI**: Endpoint, API Key, DeploymentName (o4-mini)

4. **Build the project**
   ```bash
   dotnet build
   ```

## Usage

### Transcribe a new audio file:
```bash
dotnet run -- <path-to-audio-file>
```

### Reprocess an existing transcription:
```bash
dotnet run -- <path-to-output-directory>
```
This regenerates the HTML and summary without re-running the expensive transcription API call.

If you don't provide a path, Scribe will prompt you for one.

### Supported Audio Formats

Scribe supports the following audio formats (via Azure Speech Fast Transcription):
- FLAC, M4A, MP3, MP4, MPEG, MPGA, OGA, OGG, WAV, WebM, WMA, AAC, AMR, SPEEX

**Constraints:**
- Max file size: 300 MB
- Max duration: 2 hours

## Configuration

Configuration is managed through `appsettings.json`. The file contains:

- **Logging**: Log level configuration (defaults to Warning)
- **Transcription**: Settings for the transcription service (Whisper)
- **Completion**: Settings for the completion service (GPT-4o-mini)

See `appsettings.example.json` for the full structure.

## Output

Scribe creates a directory for each meeting transcript, containing:
- **`transcript.html`** - Interactive HTML report with summary and transcript
- **`transcript.json`** - Structured data (metadata, summary, turns)
- **`fast-transcription-raw.json`** - Raw Azure Speech API response
- Copy of the original audio file
- `scribe.log` - Log file (only if warnings/errors occurred)

## Features

- ✅ Audio file transcription with speaker diarization
- ✅ AI-generated summaries with grounded key points and action items
- ✅ Interactive HTML reports with clickable transcript links
- ✅ Reprocessing mode for fast iteration on layout/summary
- ⏳ Interactive speaker name assignment
- ⏳ AI-generated topic labels
- ⏳ Configurable output directory

## CLI Options

_CLI options will be documented here as they are implemented._

## Development

Built with:
- C# / .NET 8.0
- [Spectre.Console](https://spectreconsole.net/) for CLI UI
- [Serilog](https://serilog.net/) for logging
- [Azure.AI.OpenAI](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/openai/Azure.AI.OpenAI) for AI services

## License

_License information to be added_
