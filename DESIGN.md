---
superseded_by: plans/input-flexibility.md
---

# Scribe - Design Decisions

This document captures architectural and design decisions made during development.

## Input Flexibility: Audio File vs Directory

**Decision Date:** 2025-11-08

**Context:**
During development, we need to iterate on transcript processing without re-running expensive transcription operations. However, the requirements specify collision handling (line 53 of REQUIREMENTS.md) where re-running with the same audio file should create a new directory (meeting-name-2, meeting-name-3, etc.).

**Decision:**
Implement **Option A: Input Flexibility**

The input argument can be either:
1. **Audio file path** → Normal flow with collision handling, creates new directory
2. **Directory path** → Reprocessing mode, skip transcription, reuse existing raw transcript

**Usage Examples:**
```bash
# First run - creates meeting-name/
dotnet run -- audio.mp3

# Iteration - reprocess existing directory
dotnet run -- meeting-name/

# Another fresh run - creates meeting-name-2/
dotnet run -- audio.mp3
```

**Implementation Details:**
- When input is a file: Normal workflow with collision handling as specified
- When input is a directory: Look for `whisper-raw.json`, skip transcription if found, proceed with post-processing
- Audio file won't be present in directory-reprocessing mode (acceptable tradeoff)
- If future features require audio file, we can prompt for it or document the limitation

**Development Automation Strategy:**
For rapid iteration during development, use post-build actions to delete specific artifacts:
- Delete final output → regenerate only final step
- Delete intermediate artifacts → regenerate from that point forward
- Smart skip logic: detect which artifacts exist and skip corresponding steps

**Benefits:**
- Clean conceptual model (file = new, directory = reprocess)
- Preserves collision handling semantics
- No complex CLI flags needed
- Developer-friendly for iteration
- Production-friendly for normal use

**Future Considerations:**
- May add `--force-transcribe` flag to override skip logic if needed
- Could enhance with artifact-specific regeneration flags
- Directory mode could store metadata about original audio file path for reference
