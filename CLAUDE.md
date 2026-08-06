# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Scribe is a CLI tool that **enriches an existing meeting transcription**. It does
not transcribe audio and has no ASR dependency: the user runs WhisperX themselves
(see `docs/generating-transcripts.md`) and hands Scribe the resulting JSON. Scribe
merges diarized fragments into speaker turns and generates a grounded AI summary
via Azure OpenAI.

Transcription was removed 2026-08-06 (pre-removal commit `3212a80`). Audio stays on
the machine that recorded it, ASR runs wherever the GPU is, and Scribe stays
provider-agnostic by reading JSON rather than calling an API. If you find yourself
adding an ASR client here, that is the old purpose reasserting itself — don't.

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

# Enrich a meeting directory containing a raw transcription
dotnet run -- <path-to-meeting-directory>

# Run tests
DOTNET_ROLL_FORWARD=LatestMajor dotnet test
```

The roll-forward variable is needed on machines that only have the .NET 10
runtime installed; the project targets net8.0 and the test host will otherwise
refuse to start.

### Run without arguments (interactive mode)
```bash
dotnet run
# Will prompt for the meeting directory path
```

## Configuration

The project uses `appsettings.json` for configuration (gitignored). A template is provided:
- `appsettings.example.json` - Template file committed to repo
- Copy to `appsettings.json` and add Azure credentials

Required settings:
- **Completion.AzureOpenAI**: Endpoint, ApiKey, DeploymentName, ModelName

Logging level is configurable via `Logging:LogLevel:Default` (defaults to Warning).

## Architecture

### Core Workflow

1. **Input handling** (Program.cs): takes a meeting directory. Locates the raw
   transcription by trying `raw-transcription.json`, then `fast-transcription-raw.json`
   (legacy Azure name, kept so pre-pivot meeting directories still open).

2. **Formatting** (TranscriptFormatter): raw JSON -> structured `Transcript` with
   merged speaker turns.

3. **Summary** (SummaryService): Azure OpenAI pass producing oneLiner, overview,
   grounded keyPoints and actionItems. Skipped if `transcript.json` already has
   one, which makes re-runs free.

4. **Output** (written into the meeting directory):
   - `transcript.json` - formatted transcript with metadata, turns, topics, summary
   - `scribe.log` - log file (auto-deleted if empty)

### Key Services

**TranscriptFormatter** (Services/TranscriptFormatter.cs):
- Converts raw transcription JSON to the structured Transcript model
- Concatenates fragmented lines from the same speaker (splits on >2s pauses)
- Creates TranscriptTurn objects with formatted timestamps

**SummaryService** (Services/SummaryService.cs):
- Generates AI summaries using Azure OpenAI
- Creates oneLiner, overview, keyPoints, and actionItems
- Uses structured JSON output with grounding to transcript turns

### Data Models

**Transcript** (Models/Transcript.cs):
- Main structured output format
- Contains: metadata, summary, topics, and turns
- Metadata includes speaker mapping (int ID -> string name)
- Summary includes grounded keyPoints and actionItems with turn indices
- Turns represent individual speaker utterances with timestamps

**FastTranscriptionResult et al.** (Models/AzureFastTranscriptionJson.cs):
- Read-only DTOs for the Azure Speech Fast Transcription wire format
- Scribe never calls that API; these only parse JSON someone else produced

**Configuration Models** (Models/Configuration/):
- AppSettings, CompletionSettings, AzureOpenAISettings
- Includes validation in AppSettings.IsValid()

### Logging

Serilog is configured to:
- Write to `scribe.log` in the output directory
- Default level: Warning (configurable)
- Log file is deleted if empty (no noise when everything works)
- Keep console clean for Spectre.Console output

## Design Decisions

### Enrichment only

The tool takes one input: a meeting directory holding a raw transcription. What was
once "reprocessing mode" is now the only mode. Runs are idempotent — an existing
summary is reused — so iterating on output format costs nothing.

## TODO / Not Yet Implemented

Tracked in `plans/`, not here:
- Meeting markdown record built for retrieval (`plans/llm-native-output.md`)
- Interactive speaker name assignment (`plans/speaker-identification.md`)
- Interactive meeting date, title and purpose prompts
- AI-generated topic segmentation
- Configurable output directory location

## Future Considerations

- `--verbose` flag for debug-level console logging
- Multi-provider completion via Microsoft.Extensions.AI (a local OpenAI-compatible
  endpoint is the intended default; see `plans/llm-native-output.md`)

Explicitly **not** planned: any form of audio ingestion, ASR client, or HTML
output. Both were removed in the 2026-08-06 pivot. Transcription lives in
`docs/generating-transcripts.md` as a documented human step.

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