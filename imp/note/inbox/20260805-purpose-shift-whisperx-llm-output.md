---
captured: 2026-08-05
repo: scribe
source: human
git-head: 3212a80
---

Scribe's purpose shifted. Original problem: "oops, a Zoom recording with no
transcript" — output was a nice HTML page for a human. New problem: the artifact
has to be readable by Claude (retrieval over past meetings), and paying Azure for
ASR stopped making sense once several strix halo boxes were available.

Two plans written: `plans/pluggable-transcription.md` (WhisperX behind a provider
seam, Azure demoted to one provider, local subprocess + remote HTTP transports
because home runs WhisperX on a different box and the work laptop runs it
locally) and `plans/llm-native-output.md` (delete the HTML report and template,
emit a single `meeting.md` optimized to survive RAG chunking).

This supersedes the conclusion in `20260515-182500-local-diarization.md` that
local diarization wasn't worth it. That note compared a bespoke diarization stack
against Azure; WhisperX bundles diarization with the ASR we want anyway, so it's
no longer a separate cost. The note's *technical* content (Sherpa-ONNX vs
pyannote, CPU throughput) is still accurate — only the recommendation flipped.

Unverified risk worth carrying forward: WhisperX's ASR backend is faster-whisper
(CTranslate2), whose GPU support has been CUDA-only. Strix halo is ROCm. If that
still holds, WhisperX on a strix box silently runs ASR on CPU. Needs a spike
before committing to the WhisperX path; fallback is whisper.cpp (Vulkan) for ASR
plus pyannote for diarization, behind the same seam.
