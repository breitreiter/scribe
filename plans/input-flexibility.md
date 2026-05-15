---
kind: plan
title: Input Flexibility — Audio File vs Directory Reprocessing
state: shipped
created: 2025-11-08
updated: 2026-05-15
provenance:
  source: project-migrate-skill:M-2026-05-15-1652
  migrated_at: 2026-05-15
---

# Input Flexibility — Audio File vs Directory Reprocessing

## Decision

The CLI input argument accepts either an audio file path or a directory path.

- **Audio file path** → normal flow with collision handling; creates a new output directory (`meeting-name/`, `meeting-name-2/`, …)
- **Directory path** → reprocessing mode; skips transcription and reuses the existing raw transcript file

This was chosen over adding complex CLI flags to keep the conceptual model simple: file = new run, directory = reprocess.

> **Staleness note:** The original design doc referenced `whisper-raw.json` as the artifact to detect in directory mode. The codebase now uses `fast-transcription-raw.json` — the lookup filename was renamed during implementation.

## Context

During development we need to iterate on transcript processing without re-running expensive Azure Speech transcription calls. The requirements also specify collision handling (REQUIREMENTS.md line 53) where re-running with the same audio file must create a new directory rather than overwrite. Option A (input flexibility) satisfies both constraints without a `--reprocess` flag.

## Implementation details

- File input: normal workflow with filesystem-safe title sanitisation (max 100 chars) and numerical collision suffixes.
- Directory input: look for `fast-transcription-raw.json`, skip transcription if found, proceed with post-processing.
- Audio file is not present in directory-reprocessing mode (accepted tradeoff).
- If a future feature requires the audio file in reprocessing mode, prompt for it or document the limitation.

## Development automation note

For rapid iteration, post-build actions can delete specific artifacts to force regeneration from a chosen point:
- Delete final output → regenerate only the final step.
- Delete intermediate artifacts → regenerate from that step forward.
- Smart skip logic detects which artifacts exist and skips corresponding steps.

## Future considerations

- `--force-transcribe` flag to override skip logic.
- Artifact-specific regeneration flags.
- Store original audio file path as metadata in directory mode for reference.
