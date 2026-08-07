---
kind: plan
title: Interactive Speaker Identification
state: exploring
created: 2026-05-15
updated: 2026-08-06
---

# Interactive Speaker Identification

Walk the user through identifying each detected speaker — and correcting
diarization's mistakes — before the meeting file is written.

> **Revised 2026-08-06.** Written for a 2-speaker recording; the real workload is
> a six-person research session. Two additions, settled with the user:
>
> - **Capture a role alongside the name** ("Speaker 3 → Dana Okafor, researcher").
>   Roles are the field that makes a retrieved chunk actionable — "the customer
>   said X" versus "Speaker 4 said X". Never inferred from the transcript; a
>   wrong guess is a fabricated attribution.
> - **Merge and split, not just rename.** Six voices on a single-channel
>   recording mis-cluster: one person arrives as two labels, or two quiet people
>   collapse into one. Rename-only cannot express either. Merge reassigns every
>   turn of label B to label A. Split cannot be done properly without
>   re-diarization, so it *flags* the label as containing multiple speakers,
>   which the writer surfaces rather than asserting clean attribution.

## Current state

`TranscriptFormatter` auto-assigns placeholder names (Alice, Bob, Charles…). The `transcript.Metadata.Speakers` dictionary and per-turn `speakerName` fields are already in the data model. The interactive prompt loop is not implemented.

## Proposed workflow

After transcription completes and before HTML/transcript are saved, in `Program.cs` (file mode only):

1. Announce how many distinct speakers were detected.
2. For each speaker ID (ordered by first appearance):
   - Collect lines from that speaker until ≥20 words or 3 turns, whichever comes first.
   - Display each line with its timestamp, truncated to console width − 4 with `…`.
   - Prompt: `Name for this speaker? [leave blank to keep "Alice"]`
   - If the user provides a name, update `transcript.Metadata.Speakers[id]` and all matching `turn.SpeakerName` fields.
3. Re-save `transcript.json` and generate HTML with the updated names.

## Key implementation notes

- This goes between the `TranscribeAsync` call and the directory rename in `Program.cs` — names must be set before the transcript is saved and HTML generated. Currently `TranscriptionService` saves both; the speaker-assignment step either needs to happen inside the service (awkward — it's I/O bound to console) or `TranscriptionService` needs to defer final save/HTML-gen so `Program.cs` can inject names first.
- Cleanest split: `TranscriptionService.TranscribeAsync` returns the `Transcript` object alongside the result, or exposes a second method to finalize (save + generate HTML) that `Program.cs` calls after the name loop. Avoids mixing console I/O into the service layer.
- Console width truncation: `AnsiConsole.Profile.Width` gives the current terminal width.
- Directory mode: skip the prompt (names are already saved in `transcript.json`); let the user edit the JSON directly if they want to rename speakers on a reprocess run.

## Out of scope

- AI-assisted speaker identification (matching voice to known participants list).
- True re-diarization of a split label — that needs the audio, which scribe no
  longer touches. Flagging is the honest ceiling.
- Retroactively renaming speakers in an existing transcript via a dedicated command.
