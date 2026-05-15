---
captured: 2026-05-15
repo: scribe
source: human
git-head: 981780c
---

On-device speaker diarization is feasible but probably not worth it unless a user asks.

Two options: Sherpa-ONNX (C# native, ONNX models, WeSpeaker ResNet34 embeddings — fits the .NET stack, thin docs) or pyannote.audio v3.0 Python sidecar (better accuracy, more moving parts). CPU-only is ~0.5-1x real-time on pyannote v3.0; v3.1 regressed on CPU speed. Sherpa-ONNX is slower but no Python dependency.

Current cloud path (Azure Speech Fast Transcription) is faster, cheaper per-run, and less setup. Local mode only makes sense for offline use or cost-sensitive high-volume scenarios.
