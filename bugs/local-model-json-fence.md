---
kind: bug
title: Summary is lost when a local model fences its JSON — response_format json_object is not enforced
state: patched-locally
created: 2026-08-06
updated: 2026-08-06
---

# Summary is lost when a local model fences its JSON

Running against a local OpenAI-compatible endpoint, `GenerateSummaryAsync` throws
and the whole summary is discarded:

```
[WRN] Failed to generate AI summary
System.Text.Json.JsonException: '`' is an invalid start of a value.
  Path: $ | LineNumber: 0 | BytePositionInLine: 0.
  at Scribe.Services.SummaryService.GenerateSummaryAsync(...) SummaryService.cs:line 67
```

Scribe reports `! Could not generate AI summary (continuing anyway)` and writes a
`transcript.json` with an empty `summary`. The transcript is fine; only the
enrichment is lost. **The run still costs full generation time** — 196 s on a
57-minute meeting — so the failure is expensive as well as silent-ish.

## Cause

The model returns its JSON wrapped in a markdown fence:

```
```json
{
  "oneLiner": "...",
```

`SummaryService` sets `ResponseFormat = ChatResponseFormat.Json`, which sends
`response_format: {"type":"json_object"}`. **llama.cpp accepts that field and does
not act on it.**

Measured 2026-08-06 against `glm-4.5-air` on `llm-air` (`:8082`), same prompt:

| Request | First bytes of `content` |
|---|---|
| with `response_format: {"type":"json_object"}` | `` '```json\n{\n  "oneLiner": ...' `` |
| with no `response_format` at all | `` '```json\n{\n  "oneLiner": ...' `` |
| with `response_format: {"type":"json_schema", ...}` | `'{\n  "oneLiner": ...'` |

Byte-identical for the first two — the field changes nothing. **`json_schema` is
honoured** and produces clean, unfenced, schema-valid JSON.

So this is not a prompt problem and not a model problem. Telling the model harder
to emit bare JSON would paper over it; the server simply is not constraining
decoding unless it is given a schema.

## Workaround (local only — deliberately not committed)

A `StripCodeFence()` helper in `SummaryService`, unwrapping a leading ```` ``` ````
fence before deserialization: five lines, no behaviour change on Azure, which
never fences. It was enough to get a summary out of the 57-minute meeting, and it
is **not** in the repo — it treats the symptom, and shipping it would make the
real fix look done.

## Proper fix

Use constrained decoding on the OpenAI-compatible path:
`ChatResponseFormat.ForJsonSchema(...)` with a schema derived from
`TranscriptSummary`, instead of `ChatResponseFormat.Json`.

That is strictly better than fence-stripping, because it removes a second, worse
failure this bug is currently hiding. Fence-stripping only rescues output that is
*valid JSON in a wrapper*. Unconstrained decoding can also emit prose before the
fence, a trailing explanation, `turnIndices` as strings, or a truncated object —
none of which stripping helps with, and all of which surface as the same
one-line warning and a silently empty summary.

Two things to settle when doing it:

- **Keep `ChatResponseFormat.Json` for Azure**, or verify the Responses API path
  behaves the same under a schema. The two providers should not be assumed
  identical here.
- **`MaxOutputTokens = 16000` is sized for o4-mini's reasoning budget.** On a
  local reasoning model those tokens come out of the same allowance, so the
  server must run with reasoning off (`THINK=off llm-air`) or the JSON can
  truncate mid-object. That is a *different* failure with the same symptom, and
  a schema does not prevent it.

## Reproducing

```bash
THINK=off llm-air        # in another session; ask first, ports are shared
curl -s http://127.0.0.1:8082/v1/chat/completions -H 'Content-Type: application/json' -d '{
  "model":"glm-4.5-air","max_tokens":200,
  "response_format":{"type":"json_object"},
  "messages":[{"role":"user","content":"Return JSON with keys oneLiner and overview."}]}' \
 | python -c "import json,sys; print(repr(json.load(sys.stdin)['choices'][0]['message']['content'][:80]))"
```

Expect a leading `` ```json ``. Swap in the `json_schema` form and it disappears.

## Environment

- Scribe at `Completion.Provider = "OpenAI"`, endpoint `http://127.0.0.1:8082/v1`
- `glm-4.5-air` (GLM-4.5-Air 106B-A12B UD-Q4_K_XL) via `llm-air`, llama.cpp server
- 227-turn transcript, ≈13.7k prompt tokens
