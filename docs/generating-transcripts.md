# Generating a transcript for Scribe

Scribe does not transcribe audio. You produce a raw transcription with WhisperX,
drop it in a directory, and Scribe enriches it.

This split exists so that recordings never have to leave the machine they were
made on — which matters when the recording is a customer conversation.

## The short version

```bash
whisperx meeting.m4a \
  --model large-v3 \
  --diarize --min_speakers 6 --max_speakers 6 \
  --hf_token hf_xxx \
  --output_format json --output_dir ./my-meeting

mv ./my-meeting/meeting.json ./my-meeting/raw-transcription.json
dotnet run --project /path/to/scribe -- ./my-meeting
```

WhisperX names the output after the audio file (`meeting.m4a` → `meeting.json`).
Scribe looks for `raw-transcription.json`, so rename it.

## Installing WhisperX

```bash
uv tool install whisperx        # or: pipx install whisperx
```

It pulls torch and pyannote, so it is a large install. On the work laptop (Arch)
and on the strix box (Ubuntu) these are separate installs with separate model
caches; nothing is shared between them.

## The two things that will eat an hour

**1. Diarization is gated behind a Hugging Face token.** `--diarize` loads
`pyannote/speaker-diarization-community-1` (the current default in WhisperX
3.8.6), which is a gated model. You need to:

- create a HF account and an access token,
- visit the model page and *accept its terms* while logged in,
- pass the token via `--hf_token`.

Skipping the terms-acceptance step fails at model load with an error that looks
like a network problem. It isn't.

**2. Tell it how many speakers there are.** With six people in a room, leaving
`--min_speakers`/`--max_speakers` unset lets clustering pick the count, and it
will usually pick wrong — merging two quiet participants or splitting one person
across two labels. If you know the number, pass it as both min and max.

Diarization quality on a single-channel recording of six people with cross-talk
is the weakest link in this whole pipeline. Expect to correct labels afterwards;
that is what Scribe's speaker-naming step is for.

## CPU is a legitimate choice here

WhisperX's ASR backend is faster-whisper (CTranslate2), whose GPU support has
historically been **CUDA-only**, so on an AMD/ROCm machine it runs on CPU.

For a weekly meeting that is usually the right trade: transcription is slow but
unattended, and the setup cost of getting GPU inference working on a work laptop
is paid every time the environment changes. Run it on CPU, expect the fans, come
back later. **The work laptop deliberately runs CPU-only** (decided 2026-08-06).

If throughput ever does matter, check what CT2 actually sees before assuming a
GPU is helping:

```bash
python -c "import ctranslate2; print(ctranslate2.get_cuda_device_count())"
```

The fallback for a fast local path is whisper.cpp (Vulkan) for ASR plus pyannote
for diarization, emitting the same JSON shape.

## Output format Scribe reads

Verified against WhisperX 3.8.6 (`whisperx/utils.py:WriteJSON`, which dumps the
result dict verbatim, and `whisperx/diarize.py:assign_word_speakers`).

```json
{
  "segments": [
    {
      "start": 0.309,
      "end": 4.72,
      "text": " So the thing I keep coming back to is the activation flow.",
      "words": [
        {"word": "So", "start": 0.309, "end": 0.43, "score": 0.87, "speaker": "SPEAKER_00"}
      ],
      "speaker": "SPEAKER_00"
    }
  ],
  "word_segments": [ ... ],
  "language": "en"
}
```

Details that are easy to get wrong:

| | |
|---|---|
| `start` / `end` | **Seconds**, floating point. (Azure used integer milliseconds.) |
| `speaker` | A **string** label like `SPEAKER_00`, zero-based. Never surface it in output prose — it's provider leakage. |
| Missing `speaker` | The key is **absent entirely** — not `null` — on any segment where diarization found no overlapping speaker turn. A reader that assumes the key exists will throw on real files. |
| Duration | **There is no top-level duration field.** Derive it from the last segment's `end`. |
| Confidence | No per-segment confidence. Aligned words carry `score`; segments may carry `avg_logprob`. |
| `word_segments` | Present only when alignment ran (it does by default; `--no_align` drops it). Scribe ignores it for now. |
| Speaker labels are not contiguous | Nothing guarantees `SPEAKER_00`…`SPEAKER_05` with no gaps. Assign display IDs by order of first appearance. |

## Legacy: Azure Speech Fast Transcription

Meeting directories transcribed before 2026-08-06 contain
`fast-transcription-raw.json` in Azure's format (integer `offsetMilliseconds`,
integer `speaker`). Scribe still reads those so old meetings keep reprocessing.
Nothing new should be produced in that format.
