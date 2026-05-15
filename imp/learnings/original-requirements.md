---
kind: learning
title: Original requirements and design intent
created: 2026-05-15
updated: 2026-05-15
status: current
topics: [design-intent, feature-scope, original-spec]
provenance:
  author: human
  migrated-from: REQUIREMENTS.md
---

# Original requirements and design intent

The original REQUIREMENTS.md captures the founding vision for Scribe. Preserved here as a
historical awareness signal — useful for understanding scope decisions and what hasn't been
built yet.

## What's implemented vs. not

**Implemented:**
- Audio file input with directory collision handling
- Directory-mode reprocessing (skip transcription, reuse raw JSON)
- Azure Speech Fast Transcription (replaced Whisper)
- Speaker diarization with placeholder names (Alice, Bob…)
- Transcript formatting (interleaved turns, concatenated speech blocks)
- AI summary: one-liner, overview, key points, action items (grounded)
- HTML report with summary panel, transcript, dark mode
- Auto-naming: meeting title derived from AI one-liner, output dir renamed
- Azure OpenAI via Responses API (o4-mini)
- Serilog to file, deleted if empty

**Not yet implemented:**
- Ask user for meeting title and purpose upfront
- Interactive speaker name assignment workflow (→ see `plans/speaker-identification.md`)
- AI-generated topic labels for conversation sections
- AI-generated preamble at top of transcript using title + purpose
- Copy original audio file to output directory

## Original workflow spec (verbatim key steps)

> - Ask the user for the number of speakers
> - Ask the user to provide a title for the meeting
> - Ask the user to provide the purpose or structure of the meeting (presentation, daily stand-up, etc)
> - Walk the user through assigning a nice name to each of the speakers
>   - Show first line spoken by that speaker; if short, show more until ≥20 words or 3 lines
>   - Truncate lines that overflow console width
>   - Ask for the name; blank = keep generated name
> - Use an AI to add labels to mark changes in topic
> - Use an AI to add a short preamble using the meeting title and purpose
> - Use an AI to generate a concise (but complete) summary

## Original technical intent

- Multiple provider support anticipated from the start (settings structured per-service)
- `appsettings.json` gitignored; `appsettings.example.json` as committed template
- Output dir: sanitized title, max 100 chars, collisions via `-2`, `-3` suffix

## Open questions from original doc (still open)

- Does Microsoft.Extensions.AI support transcription? (Would enable multi-provider transcription)
- Can an AI break mega-giant lines into nice paragraphs?
- Can Whisper/Speech handle video files directly?

## Future ideas from original doc

- HTML reports with scripted interactions (→ largely shipped)
- ffmpeg wrapper to process video (rip audio, snip frames for report)
