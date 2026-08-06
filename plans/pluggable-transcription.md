---
kind: plan
title: Pluggable transcription — WhisperX primary, Azure demoted
state: shelved
created: 2026-08-05
updated: 2026-08-06
superseded_by: ../docs/generating-transcripts.md
---

# Pluggable transcription — WhisperX primary, Azure demoted

> **Shelved 2026-08-06 — solved by deletion instead.** Rather than putting
> transcription behind a provider seam, transcription was removed from scribe
> entirely (commit `cd2fcb3`; pre-removal state at `3212a80`). The user runs
> WhisperX themselves and hands scribe the JSON.
>
> That deletes this plan's hardest problems rather than solving them: the
> CT2/ROCm decision gate, the two transports, the HTTP server and its upload
> timeouts, per-provider config validation, and the Arch-vs-Ubuntu install
> split. It also means privileged customer audio never has to leave the machine
> that recorded it — the constraint that actually forced the decision.
>
> What survived, in `docs/generating-transcripts.md`: the WhisperX invocation,
> the HuggingFace gated-model trap, and the ROCm caveat (still unverified —
> now a note for whoever runs it, not a blocker for scribe).
>
> What survived in code (commit `7c7fe9d`): the normalized `RawTranscript`, the
> speaker-label fix, and the two-fixture equivalence test — all of it without
> the interface, since there is no longer anything to abstract over. The
> `HttpClient` timeout bug went away with the client it lived in.

Move transcription + diarization behind a seam so WhisperX (local or on a strix
halo box) is the default path and Azure Speech Fast Transcription becomes one
provider among several rather than the shape of the whole pipeline.

## Why now

Azure works and is paid for, so it stays — but it will not be the path we use.
The driver is cost and hardware availability: several strix halo boxes exist,
and WhisperX bundles ASR + word-level alignment + pyannote diarization, which is
the whole of what Azure Speech was buying us.

This supersedes the conclusion in
`imp/note/inbox/20260515-182500-local-diarization.md` ("local diarization
probably not worth it unless a user asks"). That note weighed a *bespoke*
diarization stack against Azure. WhisperX changes the arithmetic: diarization
arrives bundled with the ASR we want anyway, so it is no longer a separate cost.

## Deployment reality (from the user, 2026-08-05)

Two environments, both must work:

- **Home:** WhisperX runs on a *different* box (strix halo). Scribe runs on the
  workstation.
- **Work laptop:** WhisperX runs on the *same* box as scribe.

So the seam needs two transports, not one. This is not a hypothetical extension
point — it is two real deployments today, which is what justifies the interface
at all.

## ⚠️ Blocking risk — verify before committing to WhisperX on strix halo

**WhisperX's ASR backend is faster-whisper, which is CTranslate2. CTranslate2's
GPU support is CUDA-only.** Strix halo is AMD/ROCm. If this is still true, then
on a strix box WhisperX will silently fall back to CPU for the ASR pass and you
will have bought a slow local path with an expensive machine sitting idle.

I have not verified the current state of CT2/ROCm and will not assert it.
**Spike this first — it can invalidate the transport design below.** On a strix box:

```bash
python -c "import ctranslate2; print(ctranslate2.get_cuda_device_count())"
whisperx sample.wav --model large-v3 --compute_type float16 --diarize   # watch device selection + wall time vs. audio length
```

Decision gate on the result:

| Outcome | Path |
|---|---|
| CT2 uses the GPU | Plan as written. WhisperX end to end. |
| CT2 is CPU-only, CPU speed acceptable (≳1× realtime, int8, many cores) | Plan as written, CPU compute type. Strix halo's CPU is strong; this may just be fine. |
| CT2 is CPU-only and too slow | Split the stack: **whisper.cpp** (Vulkan/ROCm) for ASR + **pyannote** (torch-ROCm) for diarization, merged into the same normalized segment list. The seam below is unchanged — only the provider implementation differs. |

The seam is designed so this outcome does not change anything above the
provider layer. That is the point of doing the seam first.

## The seam

The good news from reading the code: the normalized type nearly exists already.
`TranscriptionSegment` (`Models/TranscriptionResult.cs`) is provider-neutral —
speaker, start, end, text, confidence. The coupling is that
`TranscriptFormatter` and `Program.cs` reach past it into Azure's raw type.

Introduce:

```csharp
public interface ITranscriptionProvider
{
    string Name { get; }                       // "whisperx-local", "whisperx-remote", "azure-speech-fast"
    Task<RawTranscript> TranscribeAsync(string audioPath, TranscribeOptions options, IProgress<string>? progress);
}

public record TranscribeOptions(int? MaxSpeakers, string Locale);

public class RawTranscript
{
    public string Provider { get; init; }
    public string ModelId { get; init; }        // "large-v3", "azure-fast-2024-11-15" — goes in meeting.md frontmatter
    public double DurationSeconds { get; init; }
    public string Language { get; init; }
    public List<TranscriptionSegment> Segments { get; init; }
}
```

Three implementations:

- `AzureSpeechProvider` — wraps existing `AzureSpeechFastService`, keeps its
  own 300 MB / 2 h validation (those are *Azure's* limits, not scribe's).
- `LocalWhisperXProvider` — subprocess.
- `RemoteWhisperXProvider` — HTTP to a strix box.

### Coupling points to cut

| Location | Current | Change |
|---|---|---|
| `Services/TranscriptFormatter.cs:13` | `FormatTranscript(FastTranscriptionResult)` | Take `RawTranscript`. This is the central cut. |
| `Services/TranscriptionService.cs:64-70` | Azure's supported-format list | Move into `AzureSpeechProvider`. WhisperX takes anything ffmpeg reads. |
| `Services/TranscriptionService.cs:57` | 300 MB cap | Move into `AzureSpeechProvider`. |
| `Services/TranscriptionService.cs:166` | `ConvertFastResultToTranscriptionResult` | Deleted — providers emit `RawTranscript` directly. |
| `Program.cs:361` | Deserializes `FastTranscriptionResult` from raw JSON on reprocess | Deserialize `RawTranscript`. See back-compat below. |
| `Models/Configuration/TranscriptionSettings.cs` | Only `AzureSpeech` | Add `Provider` selector + `WhisperX` settings. |

### Speaker label normalization (fixes a latent bug)

WhisperX emits string labels (`SPEAKER_00`); Azure emits ints. Providers should
emit their native label as a **string** on the segment, and `TranscriptFormatter`
assigns ordinal integer IDs **by order of first appearance**.

This incidentally fixes real breakage in the current formatter:
`TranscriptFormatter.cs:22-27` builds the speaker map by looping `1..speakerCount`,
which assumes Azure's speaker IDs are contiguous and 1-based. Any gap, or a
phrase with a null speaker (which becomes `0`), yields `"Unknown"` turns and a
speaker map that doesn't match the turns. Deriving the map from observed labels
removes the assumption.

## Provider: local WhisperX

Shell out; don't try to host Python in-process.

```bash
whisperx <audio> --model large-v3 --diarize --min_speakers N --max_speakers N \
         --output_format json --output_dir <tmp> --language en
```

Notes:

- Diarization needs a HuggingFace token with the pyannote gated-model terms
  accepted. Config as `WhisperX.HuggingFaceToken`; fail with a *specific*
  message when diarization is requested without it — this is the single most
  likely first-run failure and a generic subprocess error will waste an hour.
- WhisperX JSON: `segments[]` with `start`, `end`, `text`, `speaker`, plus
  `word_segments[]`. Map segments → `TranscriptionSegment`. Word-level timings
  are richer than Azure gave us; nothing consumes them yet, so ignore them for
  now rather than modeling them speculatively.
- Resolve the binary via configured path → `PATH`. A venv path is likely.
- Stream stderr to `IProgress<string>` so the Spectre status line stays alive;
  a long transcription with a frozen spinner reads as a hang.

## Provider: remote WhisperX

There is no standard WhisperX server, so we define the contract and ship the
server. Keep it dumb:

```
POST /transcribe   multipart: audio=<file>, options=<json {language, min_speakers, max_speakers}>
→ 200 {"model": "large-v3", "duration": 1095.7, "language": "en",
       "segments": [{"start": 0.0, "end": 5.2, "speaker": "SPEAKER_00", "text": "..."}]}
GET  /health → {"status":"ok","model":"large-v3"}
```

- Server lives in `serve/whisperx-server.py` (FastAPI, ~80 lines) with a README
  covering venv + systemd unit. It is deployment code, not application code.
- Response shape is deliberately *already* our normalized shape, so
  `RemoteWhisperXProvider` is a POST plus a deserialize.
- **Timeouts:** set `HttpClient.Timeout` explicitly. An hour of audio can exceed
  any default. This is the same trap already logged for Azure in
  `imp/note/inbox/20260515-183125-httpclient-timeout.md` — that static
  `HttpClient` in `AzureSpeechFastService.cs:13` has the default 100-second
  timeout and will fail on long recordings. **Fix it as part of this work**
  while the transport layer is open; don't leave a known-broken provider behind
  the new seam and call it "it's there and it works."
- Uploading an hour of audio to another box is minutes of transfer. Report it
  as a distinct progress stage so it isn't mistaken for a stall.

## Config shape

```json
{
  "Transcription": {
    "Provider": "whisperx-local",
    "WhisperX": {
      "ExecutablePath": "whisperx",
      "Model": "large-v3",
      "ComputeType": "float16",
      "Language": "en",
      "HuggingFaceToken": "",
      "RemoteEndpoint": "http://strix-01:9000"
    },
    "AzureSpeech": { "…unchanged…" }
  }
}
```

`Provider` ∈ `whisperx-local` | `whisperx-remote` | `azure-speech-fast`. Work
laptop sets `whisperx-local`; home sets `whisperx-remote` + `RemoteEndpoint`.

`appsettings.example.json` must be updated in the same commit, and
`AppSettings.IsValid()` must validate *only the selected provider's* settings —
today it demands Azure credentials unconditionally, which would make a
WhisperX-only machine refuse to start.

## Raw JSON filename and back-compat

`fast-transcription-raw.json` is an Azure-specific name for what is now a
provider-neutral artifact. Rename to `raw-transcription.json` (carrying
`provider` and `model` fields).

Reprocess mode must still open existing output directories: on directory input,
look for `raw-transcription.json`, then fall back to `fast-transcription-raw.json`
and parse it as the Azure shape. Cheap to keep, and the alternative is that
every meeting directory you already have stops reprocessing.

## Test fixtures — currently missing

**`samples/` is gitignored in full** (`.gitignore:11`), so there is no committed
fixture, and the one sample's audio is a NotebookLM-generated podcast that can't
be committed for licensing reasons anyway. A refactor that reshapes the parsing
layer with no fixture is how you get silent regressions.

Commit a small synthetic fixture — no audio, no licensing question:

- `Scribe.Tests/fixtures/azure-raw.json` — ~10 phrases, 2 speakers, hand-written.
- `Scribe.Tests/fixtures/whisperx-raw.json` — the same conversation in WhisperX
  shape, including a `SPEAKER_00`-style label and one segment with no speaker.

Then the load-bearing test is: **both fixtures produce an identical `Transcript`**
(modulo provider/model metadata). That single assertion is what proves the seam
actually normalizes, and it's what protects the WhisperX path when only Azure is
convenient to run.

### Test audio (researched 2026-08-05)

Three distinct needs; only one of them requires audio at all:

| Need | Asset | Licensing surface |
|---|---|---|
| Unit tests — parsing, turn merging, markdown writing | The two JSON fixtures above | None |
| E2E smoke test of the WhisperX path | ~60 s multi-speaker clip | Use **AMI Meeting Corpus** |
| README demo sample | Optional; generate our own | Use **Kokoro/Piper TTS** |

**Do not commit the NotebookLM audio.** Not because Google forbids it — Google
doesn't claim ownership of Audio Overviews and their terms are silent on
redistributing outputs. The unresolvable part is *upstream*: an Audio Overview is
generated from source documents, and we no longer know what was fed in. We can't
produce a provenance chain for a file we'd be publishing. Keeping it locally for
dev is fine — `samples/` is gitignored and local use isn't distribution.

**Use the AMI Meeting Corpus** for the E2E clip: 100 h of real 3–5 speaker meeting
recordings, released under **CC BY 4.0**, and it ships reference diarization
annotations — so we get ground truth to assert speaker turns against, not just an
audio file. Commit a short excerpt (~60 s, ~1 MB) with attribution in a `NOTICE`
file. This is a strictly better fixture than the podcast was.

If a demo sample is ever wanted, generate it from a script we write ourselves
using **Kokoro-82M (Apache 2.0)** or **Piper (MIT)** — clean provenance end to
end, CPU-only, free. Avoid ElevenLabs' free tier specifically: it grants no
commercial rights and requires attribution; commercial rights start at the paid
Starter plan, which is money spent to solve a problem Kokoro solves for nothing.

Worth knowing either way: purely AI-generated output carries no US copyright
(*Thaler v. Perlmutter*, left standing March 2026), so nobody owns the podcast's
audio — but that doesn't supply the missing provenance for its source material.

`Scribe.Tests/TranscriptFormatterTests.cs` (229 lines) already covers turn
merging and will need updating to the new input type — treat its existing cases
as the spec for merge behavior and keep them passing.

## Steps

1. **Spike CT2/ROCm on a strix box.** Decision gate above. Nothing below depends
   on the answer, but the answer may change which provider you write second.
2. Commit the two fixtures + the equivalence test (against the *current*
   formatter for the Azure one, so it's green before the refactor).
3. Introduce `RawTranscript` + `ITranscriptionProvider`; convert
   `TranscriptFormatter` to consume `RawTranscript`; wrap Azure as a provider.
   No behavior change. Tests stay green. Commit.
4. Fix the speaker-map derivation; fix the `HttpClient` timeout. Commit.
5. Config: provider selector, per-provider validation, example file sync. Commit.
6. `LocalWhisperXProvider` + fixture test. Commit.
7. `serve/whisperx-server.py` + `RemoteWhisperXProvider`. Commit.
8. Raw filename rename + reprocess back-compat. Commit.

Steps 3–5 are pure refactor and can land before any WhisperX code exists.

## Out of scope

- Removing Azure. It stays as a working provider; it is simply not the default.
- Word-level timestamps, though WhisperX gives them free. No consumer yet.
- Model management on the strix boxes (the `swap-model` pattern). The server
  loads one model and holds it.
- Streaming/live transcription.
