---
kind: plan
title: LLM-native output — a single meeting markdown file replaces the HTML report
state: exploring
created: 2026-08-05
updated: 2026-08-06
---

# LLM-native output — a single meeting markdown file replaces the HTML report

Scribe's deliverable becomes a single markdown file per meeting, written to be
read by a model — dense, self-describing, and built so that any chunk of it
retrieved in isolation still means something. The HTML report is deleted.

**The worked example is the spec:
[`llm-native-output/2025-11-30-generative-ui-mechanics.example.md`](llm-native-output/2025-11-30-generative-ui-mechanics.example.md).**
Read it before this plan. It is built from the real `generative-ui-meeting`
sample — real turns, real summary content, real timestamps — so that a wrong
assumption about the format shows up as a visibly wrong line rather than a
paragraph everyone nods at. If that file is wrong, this plan is wrong.

Grilled 2026-08-05; six decisions settled (see "Settled by grilling" below) and
folded into the worked example. Its filename demonstrates the output naming
convention.

> ⚠️ **The worked example is a 2-speaker meeting; the real workload is six.**
> (2026-08-06.) The session driving this is a UX research interview with two
> customers, the researcher, a PM, a developer, and a second designer. Rewriting
> the example around that session is **step 1 of the build** — until it is done,
> treat every "2 speakers" line in it as illustrative, not as spec.
>
> Three decisions settled 2026-08-06, to be folded into the rewritten example:
>
> - **Roles are captured, not inferred.** The speaker-naming loop asks for a name
>   *and* a role per speaker ("Speaker 3 → Dana Okafor, researcher"). Roles go in
>   frontmatter and in the section stamps, because "the customer said X, the PM
>   committed to Y" is the retrieval that matters and "Speaker 4" cannot support
>   it. Inferring roles from what people say was rejected: a wrong guess is a
>   fabricated attribution, which is the same failure rule 7 exists to prevent.
> - **Diarization errors get fixed, not disclaimed.** The naming loop gains merge
>   (two labels are one person) and split/flag (one label is two people). Six
>   voices on a single-channel recording will mis-cluster; correcting at the
>   source lets the file assert attribution honestly instead of shipping
>   known-wrong speakers behind a confidence note.
> - **`transcript.json` is demoted to `.scribe/summary.json`**, a reprocessing
>   cache. The `.md` is the only top-level artifact.

## What changed about the problem

Scribe was built for "oops, a Zoom recording with no transcript." The output was
therefore a nice web page for a human to read once. The problem is now "no
transcript, *and* the artifact needs to be something Claude can read" — the
consumer is a model doing retrieval, weeks later, over many meetings.

That reframes every output decision. A web page optimizes for a human scanning
top to bottom with the whole document present. A RAG chunk arrives alone, with
no neighbors and no page.

## Design rules (why the format looks like that)

These are the non-obvious commitments in the worked example. They are the whole
value of the format; if they erode, the file is just a transcript dump.

1. **Every section restates its context, at chunk granularity.** Each `##`
   section opens with a full italic stamp — *"(From the 2025-11-30 meeting on
   generative UI, 2 unidentified speakers.)"* — and each `###` subsection opens
   with a compact one — *"(GenUI meeting, 2025-11-30.)"*. A retrieved chunk that
   says "we decided to defer it" and nothing else is worse than useless; it is
   confidently misleading. Stamping `##` alone is not enough: chunkers usually
   split at the deepest heading, so the common chunk lands *inside* a `###` and
   never sees the section stamp. This is the single most important rule and the
   most likely to be "simplified" away as repetitive. It is repetitive **on
   purpose** and only for a reader who has the whole file. Cost is ~300 tokens
   on an 18-minute meeting, proportionally less as meetings get longer.
2. **Front-load conclusions.** Abstract → decisions → actions → open questions →
   key points → topics → transcript. Partial retrieval and truncated context
   both favor the top of the file.
3. **Stable turn IDs.** `T000`-style, zero-padded, assigned once. Every claim
   cites them. A chunk containing a citation stays resolvable against the file
   even when the cited turn wasn't retrieved. Positional turn indices (today's
   `turnIndices`) break the moment anything is re-segmented.
4. **Explicit absence.** "No decisions were taken in this meeting." — never an
   omitted section. An absent section is indistinguishable from a section that
   fell outside the retrieved chunk, and a model will guess.
   **One deliberate exception:** when no summarizer was reachable, the AI-derived
   sections are *omitted* and the header says so. "Never produced" and "empty"
   must not be confusable — emitting "No decisions were taken" for a run that
   never looked would be a fabricated finding.
5. **Semantically loaded headings.** `### 02:41–06:05 — The credit-card
   activation example`, not `### Topic 2`. Chunkers split on headings, and the
   heading is often the highest-signal line in the chunk.
6. **No cross-section anaphora.** Sections name entities. No "as mentioned
   above," no bare "he/she/they/it" referring across a section boundary. This
   constrains the summarizer prompt, not just the writer.
7. **Never assert an unidentified speaker's identity.** `TranscriptFormatter.cs:7-11`
   currently assigns placeholder names from a hardcoded list (Alice, Bob,
   Charles…). Harmless in a page a human reads once; in a model-consumed file
   every mention becomes a factual claim that a person named Alice attended and
   holds a view. Until identification lands, emit `Speaker 1`/`Speaker 2`, set
   `speakers_identified: false`, and state in the header that the labels are
   diarization output. Never `SPEAKER_00` in prose either — that's provider
   leakage.
8. **Verbatim words, folded backchannels.** The transcript keeps every
   substantive word; content-free acknowledgements ("Yeah.", "Exactly.") are
   absorbed into the continuing speaker's turn as `[S2: Yeah.]`. Turn IDs are
   numbered **pre-fold**, so gaps (T000 → T003) mark folding and every ID still
   resolves against `.scribe/raw-transcription.json`. No disfluency stripping:
   summary quotes must match the transcript exactly.
9. **Frontmatter carries the filters.** date, participants, topics, duration,
   `speakers_identified`, `summary_status` — the fields a retrieval layer filters
   on before it ever embeds anything, plus the two that tell a consumer how much
   to trust the rest.

Rules 1, 3, 4, 6 and 7 are format *invariants*: violating them silently degrades
retrieval without breaking anything visible. They are the natural content of a
`rules/` entry — **proposed, not written**, since `rules/` is human-authored
(see CLAUDE.md). Text is drafted in "Proposed rule" below for you to promote.

## Output directory layout

```
2025-11-30-generative-ui-mechanics/
  2025-11-30-generative-ui-mechanics.md   ← the deliverable
  Generative_User_Interfaces.m4a          ← copied source audio
  .scribe/
    raw-transcription.json                ← provider output, for reprocessing
    summary.json                          ← cached AI pass, so reprocess is free
    scribe.log
```

The `.md` is named `<date>-<slug>.md`, not `meeting.md`: filenames routinely
survive into chunk metadata, and a store full of `meeting.md` carries no signal.
The slug comes from the confirmed title; the directory takes the same name.

One obvious deliverable at top level; machinery hidden. Today `transcript.json`,
`fast-transcription-raw.json`, `transcript.html` and the audio sit as peers,
which makes it ambiguous which file *is* the meeting.

`transcript.json` stops being an output and becomes `.scribe/summary.json`, a
cache. Reprocess mode reads `.scribe/`, falling back to the legacy flat names so
existing meeting directories keep working (shared with the transcription plan).

## Model changes

`Models/Transcript.cs` needs to carry what the format promises:

- `TranscriptTurn.Id` — `"T042"`, assigned in `TranscriptFormatter`.
- `TranscriptSummary.Abstract` — replaces `Overview`. Renamed deliberately: an
  "overview" invites 2–3 loose paragraphs, an "abstract" invites one dense one.
- `TranscriptSummary.Decisions` — `{ id, decision, rationale, turnIds }`. New,
  and the highest-value field for the "what did we agree" retrieval that
  motivates the whole pivot.
- `TranscriptSummary.OpenQuestions` — `{ question, turnIds }`. New.
- `SummaryKeyPoint.TurnIds` / `SummaryActionItem.TurnIds` — string IDs replacing
  `TurnIndices`.
- `TranscriptTopic` — gains `EndTime`, `Summary` (currently nullable and never
  populated), and `TurnIdRange`. Today `TranscriptFormatter.cs:95-102` hardcodes
  a single topic titled `"Full Transcript"`; real segmentation is the load-
  bearing new capability, since topic boundaries are what make chunk boundaries
  fall in sensible places.
- `TranscriptMetadata.SpeakersIdentified` — bool, false until the naming loop
  from `speaker-identification.md` runs. Drives rule 7's header note.
- `TranscriptMetadata.SummaryStatus` — `ok` | `unavailable`, so the writer can
  distinguish "no decisions" from "never looked for decisions".
- `TranscriptTurn.FoldedBackchannels` — the absorbed interjections with their
  original IDs and speakers, so the writer can emit `[S2: Yeah.]` and the fold
  stays reversible.

Remove `TranscriptMetadata.Speakers`' dependence on the hardcoded name list in
`TranscriptFormatter.cs:7-11`; delete the list. Per rule 7 the default is
`Speaker N`, and once identification lands the names come from the human.

## Interactive prompts

Three values are confirmed by a human rather than inferred, because each is
either unrecoverable from the audio or currently wrong:

- **Date.** `TranscriptFormatter.cs:87` sets `RecordingDate = DateTime.Now` —
  the *processing* date. Reprocess a March meeting today and the file claims it
  happened today, corrupting the field retrieval filters on hardest. Prompt,
  pre-filled with a best guess (recording-filename patterns like Zoom's
  `GMT20251130-140000_Recording.m4a`, else the audio file's mtime) so it's one
  keypress. Reprocess mode reuses the stored date and does not re-ask.
- **Title.** Prompt with the AI one-liner as the editable default. A human title
  beats a generated one, and this also gives a titled file when no summarizer
  was reachable.
- **Purpose.** Prompt, blank allowed. `meetingPurpose` already exists in the
  model and is never populated. Why a meeting happened is frequently absent from
  what was said in it, and it is the context that makes a retrieved chunk
  actionable.

All three run after summarization, so the AI output can serve as the default.
Prompts live in `Program.cs`, not the service layer — console I/O stays out of
services, per the same reasoning in `speaker-identification.md`.

## Summarizer changes

`SummaryService` moves to an **OpenAI-compatible base URL in config**, defaulting
to a local endpoint (glmchat on a strix box) with Azure OpenAI still selectable —
mirroring the provider seam in `pluggable-transcription.md`. This is a small
change: `SummaryService.cs:28` already goes through `Microsoft.Extensions.AI`'s
`IChatClient`, which is endpoint-agnostic.

If no summarizer is reachable, **still write the file**: frontmatter, header,
and full transcript, with `summary_status: unavailable` and a header line saying
the AI sections were never produced (see rule 4's exception). A transcript with
no summary is far more useful than a failed run, and this is now the offline
path rather than a rare error case.

Watch the quality of local topic segmentation specifically — structured JSON
segmentation over a long transcript is where a smaller model gets sloppy. If it
degrades, the Azure path is the fallback, which is a reason to keep it.

**Constrained decoding, not prompt pleading.** `bugs/local-model-json-fence.md`
established that llama.cpp accepts `response_format: {"type":"json_object"}` and
does not act on it, while `json_schema` is honoured and yields clean output. The
OpenAI path must use `ChatResponseFormat.ForJsonSchema(...)` derived from
`TranscriptSummary`. Azure keeps `ChatResponseFormat.Json` until the Responses
API is verified under a schema — do not assume the two providers behave alike.

**Split into two calls: summary, then topic segmentation.** Everything this plan
adds makes the response longer and more structured, on exactly the axis that just
failed. Real numbers from the first full run: a 57-minute meeting is 227 turns and
≈13.7k prompt tokens, and generation took 196 s. Two calls halve the output per
call (the truncation risk, since `MaxOutputTokens = 16000` is sized for o4-mini's
*reasoning* budget and local reasoning models spend from the same allowance), and
they isolate failure — sloppy segmentation should not discard a summary that
already cost three minutes.

The rewritten prompt and schema, beyond the new fields:

- **It must segment the meeting into topics** with start/end turn IDs. This is
  new work for the model and the part most likely to come back sloppy — validate
  that segments are contiguous, non-overlapping, and cover every turn; repair by
  extending the neighbors rather than failing the run.
- **Turn IDs, not indices.** Feed turns as `[T042] Alice (04:31): …` and require
  citations in that vocabulary.
- **Anaphora constraint** in the system prompt, per design rule 6.
- **Explicit empties**: return `[]` for decisions rather than omitting, so the
  writer can emit rule 4's explicit-absence line.
- `CleanupTurnMarkers` (`SummaryService.cs:130-171`) — the regex that strips
  `(turn 5)` from summary prose. Its motivation inverts under the new format: we
  now *want* citations, just in the bracketed `[T042]` form. Keep a narrowed
  version that strips prose-style mentions, and rewrite the comment to say why
  it still exists — a stale "we don't want turn references" comment next to
  citation-generating code will steer the next session wrong.
- Long meetings will eventually exceed a single call. Out of scope; note the
  ceiling when you hit it rather than building map-reduce speculatively.

## New: MeetingMarkdownWriter

`Services/MeetingMarkdownWriter.cs` — takes `Transcript`, writes the meeting markdown file.
Pure string building, no I/O beyond the final write, trivially unit-testable
against the worked example. It replaces `HtmlReportGenerator`.

## Removal pass (do this first)

Per the pivot discipline: the deletions land **before** the new writer, so the
old artifacts stop acting as the spec while the new format is being built.

- **Delete** `Services/HtmlReportGenerator.cs`.
- **Delete** `Templates/transcript-template.html` (16 KB) and the `Templates/` dir.
- **Delete** `HtmlPath` from `Models/TranscriptionResult.cs` and every assignment
  (`TranscriptionService.cs:107-108,129,142`; `Program.cs:208-209,220,275,310`).
- **Flip** `plans/report-design.md` to `state: shelved` with a line explaining
  that the HTML report was removed in favor of the meeting markdown file — a plan describing
  the artifact we just deleted is exactly the residue that reasserts the old
  purpose.
- **Sweep narrative residue**: the README and CLAUDE.md both describe scribe as
  producing an HTML report ("interactive HTML reports", "HTML reports with
  interactive media controls"). CLAUDE.md's *Future Considerations* still lists
  media controls and REPORT_DESIGN.md. These sentences are cheap to fix and
  disproportionately misleading — a comment or doc line asserting the old
  purpose steers a fresh session harder than deleted code does.

Recoverable from git history if ever wanted; note the pre-removal commit hash in
this plan when you do it.

## Steps

0. **JSON contract first.** `ForJsonSchema` on the OpenAI path; split the AI pass
   into summary + segmentation calls; assert against truncation rather than
   discovering it mid-object. Closes `bugs/local-model-json-fence.md`. Everything
   below enlarges the model response, so this lands first. Commit.
1. **Rewrite the worked example** around the six-speaker research session, with
   roles in frontmatter and stamps. The `.md` leads; grill it before building.
2. Model changes: turn IDs, decisions, open questions, topic ranges,
   `speakersIdentified`, `summaryStatus`, roles on the speaker map. Commit.
3. Backchannel folding in `TranscriptFormatter` + tests (pre-fold ID numbering is
   the part worth asserting). Commit.
4. `MeetingMarkdownWriter` + unit test against the worked example, **including the
   degraded no-summary variant**, which is the branch most likely to rot untested.
   Commit.
5. Summarizer prompt/schema rewrite, topic segmentation with contiguity
   validation and neighbour-extension repair. Commit.
6. Speaker identification: naming loop with **name + role**, plus merge and
   split/flag. Sets `speakers_identified: true`. Commit.
7. Interactive date/title/purpose prompts with pre-filled defaults. Commit.
8. Output restructure: `.scribe/` for machinery, `<date>-<slug>.md` as the
   deliverable, `transcript.json` → `.scribe/summary.json`, reprocess back-compat
   for existing meeting directories. Commit.
9. README/CLAUDE.md rewrite to describe the new deliverable.

Done already (2026-08-06): the removal pass — HTML generator, template, paths,
doc sweep, plan state flips — in commit `cd2fcb3`.

## Settled by grilling (2026-08-05)

Decided against the worked example, which has been revised to match:

| Question | Decision |
|---|---|
| Unidentified speakers | Neutral `Speaker N` labels, `speakers_identified: false`, header note. Never invent a person. |
| Transcript fidelity | Verbatim words; content-free backchannels folded inline. No disfluency stripping. IDs numbered pre-fold, gaps mark folds. |
| Summarizer location | Local OpenAI-compatible endpoint default, Azure retained, graceful degradation when unreachable. |
| Meeting date | Prompted, pre-filled from filename/mtime guess. Reprocess reuses stored value. |
| Title & purpose | Both prompted; AI one-liner pre-fills title. File named `<date>-<slug>.md`. |
| Stamp granularity | Full stamp at `##`, compact stamp at `###`, because chunks land inside subsections. |

Carried over from the original draft, unchanged and not re-litigated: the full
transcript stays in the one file (splitting it breaks citation resolution for
summary-half chunks); `[T042 04:31]` turn-line format; decisions before action
items; transcript repeats the topic headings so its chunks inherit topic context.

## Proposed rule (human-required — for you to promote into `rules/`)

> **Meeting-file format invariants.** Every `##` section restates meeting date
> and participants; every `###` subsection carries a compact restatement, because
> that is the granularity chunks actually land at. Every section that can be
> empty is emitted with an explicit "none" statement rather than omitted — except
> when the summarizer never ran, in which case the sections are omitted and the
> header says so, since "empty" and "never produced" must not be confusable. All
> citations use stable `T###` turn IDs, never positional indices. No section
> refers to another by position or with unresolved pronouns. **No speaker is
> named unless a human identified them**; unidentified speakers are `Speaker N`
> with `speakers_identified: false`. Rationale: the file is consumed in chunks by
> retrieval; each of these silently degrades retrieval quality — or, in the
> speaker case, fabricates attributable claims — without failing visibly.

## Out of scope

- Any HTML/web output, including "just a small one." That's the pivot.
- Multi-file output. Considered and rejected: one file keeps citations
  resolvable and is one thing to drop into a store.
- Embedding/indexing. Scribe emits the artifact; the RAG layer is someone else's
  job.
- Cross-meeting linking or a meeting corpus index. Plausible later; not now.
