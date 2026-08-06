---
kind: plan
title: HTML Report Design
state: shelved
created: 2025-11-30
updated: 2026-08-05
superseded_by: llm-native-output.md
provenance:
  source: project-migrate-skill:M-2026-05-15-1652
  migrated_at: 2026-05-15
---

# HTML Report Design

> **Shelved 2026-08-05.** The HTML report is being removed: scribe's consumer is
> now a model doing retrieval, not a person reading a page once. Superseded by
> `llm-native-output.md`, which replaces `transcript.html` with `meeting.md`.
> Kept for the interaction-design reasoning, which may be worth something if a
> viewer is ever wanted again.

**Outcome:** Core layout shipped in HtmlReportGenerator.cs + Templates/transcript-template.html. Two items remain unimplemented: (1) media controls — placeholder exists in header, disabled; (2) multi-timestamp "also discussed at" links in summary items.

## Sticky Header
- Meeting title
- One-liner summary (from AI)
- Dark mode toggle
- Media controls placeholder (disabled for now)

## Main Layout

### Desktop (≥768px)
Two-column split layout:
- **Left (40%)**: AI-Generated Summary (max-width: 600-700px)
- **Right (60%)**: Full Transcript (max-width: 600-700px)

### Mobile (<768px)
Stacked layout:
- AI-Generated Summary (top)
- Full Transcript (below)

## Left Panel: AI-Generated Summary

### Structure (in order):
1. **One-liner** - Already shown in header, optionally repeat here
2. **Overview** - 2-3 paragraph AI-generated overview
3. **Key Points** - List of main discussion points
   - Each item shows the point text
   - Click to jump to first turn where discussed
   - Future enhancement: Show additional timestamps if there are significant gaps (e.g., "also 3:42, 18:21")
   - Grounded via `turnIndices` array
4. **Action Items** - List of identified action items
   - Each item shows the action text
   - Click to jump to first relevant turn
   - Shows assignee if mentioned
   - Grounded via `turnIndices` array

### Visual Treatment:
- Sticky positioning so it stays visible while scrolling transcript
- Clean, scannable list format
- Clickable items with hover states
- Distinct styling for action items (e.g., checkmark icon)

## Right Panel: Full Transcript

### Structure:
- Chronological list of turns
- Each turn shows:
  - Speaker name (colored by speaker ID)
  - Start/end timestamps
  - Spoken text
- Turn IDs for linking from summary

## Linking Behavior
- Summary items link to transcript turns via `turnIndices`
- Currently: Jump to first turn in the array
- Future: Detect gaps and show "also discussed at" links

## Responsive Behavior
- Desktop: Side-by-side panels, both scrollable independently
- Tablet: May reduce summary width slightly
- Mobile: Stack vertically, summary first

## Max Width Constraints
- Summary panel: 600-700px max
- Transcript panel: 600-700px max
- Prevents excessively wide text that's hard to read
- Centered layout on ultra-wide screens
