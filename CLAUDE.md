# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Scribe is a CLI tool for generating transcripts and summaries from meeting audio recordings. It uses Azure AI Speech (Fast Transcription) for transcription with speaker diarization, and Azure OpenAI (GPT-4o-mini) for generating summaries and processing transcripts.

**Tech Stack:**
- C# / .NET 8.0
- Spectre.Console (CLI UI)
- Serilog (logging to file)
- Azure.AI.OpenAI
- System.Net.Http.Json

## Common Development Commands

### Build and Run
```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run with audio file
dotnet run -- <path-to-audio-file>

# Run with existing directory (reprocessing mode)
dotnet run -- <path-to-directory>
```

### Run without arguments (interactive mode)
```bash
dotnet run
# Will prompt for audio file path
```

## Configuration

The project uses `appsettings.json` for configuration (gitignored). A template is provided:
- `appsettings.example.json` - Template file committed to repo
- Copy to `appsettings.json` and add Azure credentials

Required settings:
- **Transcription.AzureSpeech**: Endpoint, ApiKey, Region, Locale
- **Completion.AzureOpenAI**: Endpoint, ApiKey, DeploymentName, ModelName

Logging level is configurable via `Logging:LogLevel:Default` (defaults to Warning).

## Architecture

### Core Workflow

1. **Input handling** (Program.cs:51-80):
   - Accepts audio file path OR directory path
   - **File mode**: Normal transcription workflow
   - **Directory mode**: Reprocessing mode (skips transcription, reuses `fast-transcription-raw.json`)

2. **Transcription** (TranscriptionService.cs):
   - Validates audio file (format, size < 300MB, duration < 2 hours)
   - Calls AzureSpeechFastService with speaker diarization settings
   - Saves raw JSON response to `fast-transcription-raw.json`
   - Formats transcript using TranscriptFormatter
   - Generates HTML report via HtmlReportGenerator
   - Generates AI summary via SummaryService (optional)

3. **Output** (per-run directory):
   - `fast-transcription-raw.json` - Raw Azure Speech API response
   - `transcript.json` - Formatted transcript with metadata, turns, topics, summary
   - `transcript.html` - HTML report for viewing
   - `scribe.log` - Log file (auto-deleted if empty)
   - Copy of original audio file (in normal mode)

### Key Services

**TranscriptionService** (Services/TranscriptionService.cs):
- Orchestrates the transcription workflow
- Validates input files and formats
- Calls AzureSpeechFastService for transcription
- Formats results and generates outputs

**AzureSpeechFastService** (Services/AzureSpeechFastService.cs):
- Calls Azure AI Speech Fast Transcription API
- Handles speaker diarization parameters (minSpeakers, maxSpeakers)
- Returns FastTranscriptionResult (raw API response)

**TranscriptFormatter** (Services/TranscriptFormatter.cs):
- Converts raw API response to structured Transcript model
- Concatenates fragmented lines from same speaker
- Generates speaker names (e.g., "Speaker 1", "Speaker 2")
- Creates TranscriptTurn objects with formatted timestamps

**SummaryService** (Services/SummaryService.cs):
- Generates AI summaries using Azure OpenAI
- Creates oneLiner, overview, keyPoints, and actionItems
- Uses structured JSON output with grounding to transcript turns

**HtmlReportGenerator** (Services/HtmlReportGenerator.cs):
- Generates interactive HTML reports from Transcript model
- Uses template from Templates/transcript-template.html

### Data Models

**Transcript** (Models/Transcript.cs):
- Main structured output format
- Contains: metadata, summary, topics, and turns
- Metadata includes speaker mapping (int ID -> string name)
- Summary includes grounded keyPoints and actionItems with turn indices
- Turns represent individual speaker utterances with timestamps

**TranscriptionResult** (Models/TranscriptionResult.cs):
- Internal representation during processing
- Contains segments with speaker IDs, timestamps, text, confidence
- Tracks raw JSON path, transcript path, HTML path

**Configuration Models** (Models/Configuration/):
- AppSettings, TranscriptionSettings, CompletionSettings
- AzureSpeechSettings, AzureOpenAISettings
- Includes validation in AppSettings.IsValid()

### Logging

Serilog is configured to:
- Write to `scribe.log` in the output directory
- Default level: Warning (configurable)
- Log file is deleted if empty (no noise when everything works)
- Keep console clean for Spectre.Console output

## Design Decisions

### Input Flexibility (DESIGN.md)

The tool accepts two types of input:
1. **Audio file path**: Normal workflow with collision handling (creates meeting-name, meeting-name-2, etc.)
2. **Directory path**: Reprocessing mode - skips transcription, reuses existing `fast-transcription-raw.json`

This allows rapid iteration during development without re-running expensive transcription operations.

### Directory Collision Handling

When creating output directories based on meeting title:
- Sanitize title for filesystem (max 100 chars)
- Resolve collisions by appending numerical index (-2, -3, etc.)
- See REQUIREMENTS.md:53 for specification

## TODO / Not Yet Implemented

From REQUIREMENTS.md, these features are planned but not yet implemented:
- Interactive meeting title and purpose prompts (line 13-14)
- Interactive speaker name assignment workflow (line 19-27)
- AI-generated topic labels for conversation sections (line 32)
- AI-generated preamble at top of transcript (line 33)
- Configurable output directory location

Current workflow asks for:
- Number of speakers (for diarization)

See Program.cs:271-274 for TODO markers.

## Supported Audio Formats

Via Azure Speech Fast Transcription:
- FLAC, M4A, MP3, MP4, MPEG, MPGA, OGA, OGG, WAV, WebM, WMA, AAC, AMR, SPEEX

File constraints:
- Max size: 300 MB
- Max duration: 2 hours
- See TranscriptionService.cs:62-75 for validation logic

## Future Considerations

From REQUIREMENTS.md and DESIGN.md:
- HTML reports with interactive media controls (see REPORT_DESIGN.md for mockup)
- Using Microsoft.Extensions.AI for multi-provider support
- AI-powered paragraph breaking for long lines
- Video file support (extracting/downsampling audio)
- `--force-transcribe` flag to override reprocessing skip logic
- `--verbose` flag for debug-level console logging

## Project substrate (managed by `imp init`)

This repo uses a structured project-knowledge substrate split between
`imp/` (gnome-maintained) and root-level human-owned dirs.
Read substrate content before answering questions about design,
intent, or current behavior.

### What's where

**`imp/` — gnome territory** (imp writes directly under
`imp-gnome <noreply@imp.local>`):

- **`imp/concepts/<topic>.md`** — auto-generated narrative
  synthesis pages. Don't hand-edit; regenerated by `imp tidy`.
- **`imp/_index/`** — per-file/symbol/feature lookup pages.
  Read `imp/_index/by-file/<path>.md` before editing a
  source file for a digest of what to know first.
- **`imp/learnings/`** — discovered knowledge, why-decisions,
  gotchas. Authored by the gnome from notes.
- **`imp/reference/`** — archived external sources (URLs +
  local snippets). Authored by the gnome from notes.
- **`imp/note/inbox/`** — write target for `imp note`. The
  gnome processes captures here into structured entries on
  `imp tidy`.
- **`imp/log.md`** — append-only history.

**Repo root — human territory:**

- **`plans/`** — design intent, specs, in-flight work. Most new
  work starts as a plan in `state: exploring`.
- **`bugs/`** — bug reports.
- **`TODO.md`** — running list.
- **`rules/`** — hard project invariants. Substrate-shaped
  (frontmatter, drift tracking) but human-authored.

For drift semantics per kind, see `imp/_meta/conventions.md`.

### imp proposals

Imp writes its own dir directly. For changes touching root-level
human dirs (`rules/`, `plans/`, `bugs/`, `TODO.md`), imp produces
proposals at `scribe.imp-proposals/P-NNN-<slug>.md`. Review
and apply via `/imp-promote`. Auto-approval gradient when Claude
reviews on the user's behalf:

- **Always-safe** (auto-apply): `TODO.md` appends.
- **Claude-approvable**: plan edits and state-flips, new
  exploring plans.
- **Human-required**: any change to `rules/`, deletions, anything
  that loses information.