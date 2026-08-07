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

`TranscriptFormatter` assigns neutral `Speaker N` labels by order of first
appearance (the Alice/Bob placeholder list was deleted in `7c7fe9d` — a name is a
factual claim about who attended). `Transcript.Metadata.Speakers` maps display ID
to label; per-turn `SpeakerName` is populated. Segments diarization could not
attribute render as "Unidentified speaker" with ID 0 and no speaker-map entry.
The interactive loop is not implemented.

## Proposed workflow

Runs in `Program.cs` after formatting and before the meeting file is written.
Console I/O stays out of the service layer.

1. Report how many distinct labels diarization produced, and warn when that
   disagrees with the number of people the user expected.
2. For each label, in order of first appearance:
   - Show enough of that speaker's lines to recognise them — collect until ≥20
     words or 3 turns, whichever comes first, each with its timestamp, truncated
     to `AnsiConsole.Profile.Width - 4`.
   - Prompt for a **name** and a **role**, both blank-able. Blank keeps
     `Speaker N` and leaves `speakers_identified: false`.
   - Offer **merge**: "this is the same person as <already-named speaker>" —
     reassigns every turn of this label and drops it from the speaker map.
   - Offer **flag as multiple**: this label is more than one person. Recorded on
     the speaker, surfaced by the writer; not correctable without re-diarization.
3. Set `speakers_identified: true` only if every surviving label got a name.
4. Save, then write the meeting file.

## Key implementation notes

- Merge must renumber nothing: display IDs are already assigned, and rewriting
  them would invalidate turn IDs the summary cites. Merging label B into A
  rewrites B's turns to A and removes B from the map, leaving a gap in the ID
  sequence. Gaps are fine; unstable citations are not.
- Run the loop *before* summarization where possible, so the summary can use real
  names and roles. On a reprocess with a cached summary, renaming means the
  cached summary's speaker references go stale — either re-summarize or leave the
  cache alone and say so.
- Reprocess mode: names already in `.scribe/summary.json` are reused and not
  re-asked. A dedicated re-identify command is out of scope.

## Out of scope

- AI-assisted speaker identification (matching voice to known participants list).
- True re-diarization of a split label — that needs the audio, which scribe no
  longer touches. Flagging is the honest ceiling.
- Retroactively renaming speakers in an existing transcript via a dedicated command.
