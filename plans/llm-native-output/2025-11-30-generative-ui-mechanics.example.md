<!--
WORKED EXAMPLE — not a real output yet. Target artifact for
../llm-native-output.md, built by hand from samples/generative-ui-meeting/
(real transcript, real summary content, real timestamps).

Revised 2026-08-05 after grilling. Decisions baked in below:
  1. Speakers unidentified → neutral labels + speakers_identified: false.
     Never invent a person. (Shown here in the unidentified state, which is
     the default until the speaker-identification plan lands.)
  2. Verbatim words; content-free backchannels folded inline as [S2: …].
     Turn IDs are numbered PRE-fold, so gaps (T000 → T003) mark folding and
     every ID still resolves against .scribe/raw-transcription.json.
  3. Summarizer is a local OpenAI-compatible endpoint by default; file
     degrades gracefully if unreachable (see "Degraded output" at the end).
  4. Meeting date is prompted, pre-filled from filename/mtime guess.
  5. Title and purpose are prompted, AI one-liner pre-filled as the default.
     Filename is <date>-<slug>.md — this file demonstrates it.
  6. Full context stamp on every ##; compact stamp on every ###.

If a line here is wrong, the plan is wrong. Read it adversarially.
-->

---
type: meeting
title: "Generative UI: mechanics, business case, and adoption barriers"
purpose: "Decide whether GenUI is worth prototyping this quarter"
date: 2025-11-30
duration: "18:15"
participants: ["Speaker 1", "Speaker 2"]
speakers_identified: false
topics: [generative UI, personalization, LLM architecture, natural language interfaces, design practice, adoption cost]
audio: Generative_User_Interfaces.m4a
asr: whisperx large-v3
diarization: pyannote-3.1
summarizer: glmchat
summary_status: ok
turns: 168
generated: 2026-08-05
---

# Generative UI: mechanics, business case, and adoption barriers

Meeting on 2025-11-30, 2 speakers, 18 minutes, audio `Generative_User_Interfaces.m4a`. Convened to decide whether GenUI is worth prototyping this quarter. **Speakers were not identified**: "Speaker 1" and "Speaker 2" are diarization output, not named people, and no claim is made about who they are. Exploratory discussion — no decisions were taken.

## Abstract

*(From the 2025-11-30 meeting on generative UI, 2 unidentified speakers.)*

The speakers work through generative user interfaces (GenUI) — interfaces whose components, layout, and workflow are produced at runtime by generative models from user context, as opposed to one-size-fits-all dashboards that are merely customizable. They anchor the concept in a credit-card activation example, separate GenUI from AI-assisted design tooling, walk the model architecture underneath it, and end on three barriers they consider unsolved: LLM unpredictability, infrastructure cost, and privacy exposure from the context collection GenUI requires.

The through-line: GenUI moves the designer's unit of work from *the screen* to *the constraints under which a screen gets generated*.

## Decisions

*(From the 2025-11-30 meeting on generative UI, 2 unidentified speakers.)*

**No decisions were taken in this meeting.** It was an exploratory discussion; no commitments, no owners, no dates. The convening question — whether to prototype this quarter — was not resolved.

## Action items

*(From the 2025-11-30 meeting on generative UI, 2 unidentified speakers.)*

1. **Define GenUI's core components and assess its relationship to VNLIs and chatbots.** Owner: unassigned. No date given. [T009]

## Open questions

*(From the 2025-11-30 meeting on generative UI, 2 unidentified speakers.)*

- How do you QA an interface that is different for every user and every session? Raised but not resolved. [T130]
- What is the actual inference cost per rendered screen at production traffic? Speaker 1 flags it as the blocker; no numbers were available in the meeting. [T139]
- Does the context collection GenUI needs survive a privacy review in a regulated industry? [T147, T151]

## Key points

*(From the 2025-11-30 meeting on generative UI, 2 unidentified speakers. Citations are turn IDs in the Transcript section below.)*

- GenUI renders and adapts interface elements at runtime from immediate user context — not preference toggles applied to a fixed layout. [T000, T011, T013, T015]
- The credit-card activation example: system detects a pending activation, generates and pre-fills the activation form at login, then swaps that component for a rewards widget once complete — removing the navigation path entirely rather than shortening it. [T016, T023, T027, T031]
- GenUI is distinct from AI-assisted design tools: GenUI produces the live end-user experience, design tools assist the designer at build time. [T035, T041]
- The architecture is a network of specialized models — LLMs, vision models, and component-selection models — consuming behavior, device, and location context to emit UI. [T045, T049, T051]
- Conversational natural-language interfaces let users issue commands directly, bypassing menu hierarchies. [T052, T053, T055]
- Dynamic personalization reorders components and adapts styling, framed primarily as an accessibility win rather than a cosmetic one. [T057, T061, T063]
- Claimed business impact: up to 20% improvement in sign-up conversion, plus churn reduction attributed to accessibility gains. Source of the 20% figure was not stated in the meeting. [T067, T071]
- Visualization-oriented NL interfaces (VNLIs) democratize data access but degrade on complex data types and high-level ambiguous queries. [T091, T095, T103, T111]
- The designer's role shifts to outcome-oriented design: specifying guardrails and constraints rather than drawing screens. [T113, T117, T121]
- Three adoption blockers, in the order they weighted them: LLM unpredictability, infrastructure cost, privacy. [T130, T139, T147, T151]

## Topics

*(Section-by-section map of the 2025-11-30 meeting on generative UI, 2 unidentified speakers.)*

### 00:00–02:41 — Framing: custom-built, not customizable
*(GenUI meeting, 2025-11-30.)*

Speaker 1 opens on the distinction that governs the rest of the meeting: today's interfaces are *customizable* (the user adjusts a fixed layout), where GenUI is *custom-built* (the layout is produced for this user, in this moment, from their context and intent). Speaker 2 frames it as a move away from designing for the average user. [T000–T015]

### 02:41–06:05 — The credit-card activation example
*(GenUI meeting, 2025-11-30.)*

The concrete case they return to throughout. A user logs in with a card pending activation; instead of surfacing a notification that leads to a menu that leads to a form, the interface generates the activation form pre-filled, in place. Once activated, that component is replaced by a rewards widget. Speaker 2's point: the friction removed isn't clicks, it's the user having to know where the feature lives. [T016–T034]

### 06:05–08:30 — GenUI vs. AI-assisted design tools
*(GenUI meeting, 2025-11-30.)*

Speaker 1 separates the two categories explicitly, because they get conflated: tools like AI-assisted mockup generators act on the designer during build; GenUI acts on the end user at runtime. Nothing about a design tool's output is per-user. [T035–T044]

### 08:30–12:10 — Architecture underneath
*(GenUI meeting, 2025-11-30.)*

Not one model. A network: LLMs for intent and copy, vision models for layout evaluation, specialized components for selection and ordering, all fed a context window of behavior history, device characteristics, and location. Speaker 1 notes the latency budget is what makes this hard, not the model quality. [T045–T066]

### 12:10–15:20 — Business case and VNLIs
*(GenUI meeting, 2025-11-30.)*

The 20%-sign-up-conversion claim and churn reduction via accessibility. Then a detour into visualization-oriented natural language interfaces: strong on democratizing access to data, weak on complex data types and high-level queries where the user's question is underspecified. [T067–T112]

### 15:20–18:15 — Designer's role and the three blockers
*(GenUI meeting, 2025-11-30.)*

Design becomes outcome-oriented — you specify guardrails, acceptable variation, and invariants, then let generation fill the space. They close on what stops adoption today: unpredictability of LLM output in a UI surface where wrong is very visible, infrastructure cost per render, and the privacy surface of the context collection the whole approach depends on. [T113–T167]

## Transcript

*(Full transcript of the 2025-11-30 meeting on generative UI, 2 unidentified speakers. Turn IDs `T###` are referenced by the sections above; timestamps are offsets into `Generative_User_Interfaces.m4a`. Backchannels are folded inline as `[S2: …]`; ID gaps mark folded turns.)*

### 00:00–02:41 — Framing: custom-built, not customizable
*(GenUI meeting, 2025-11-30.)*

**[T000 00:00] Speaker 1:** Imagine your computer screen, right? Not just customizable like we're used to, but actually custom built. [S2: Yeah.] Right now, just for you, based on, well, everything, your context, your mood, what you're trying to do.

**[T003 00:10] Speaker 2:** Exactly. It's a huge shift. We're moving away from interfaces designed for, you know, the average person.

<!-- … turns T004–T167 continue in the same form; elided in this worked example only. The real file contains every turn. -->

### 02:41–06:05 — The credit-card activation example
*(GenUI meeting, 2025-11-30.)*

**[T016 02:41] Speaker 1:** So take something really mundane. You've got a new credit card sitting in a drawer, needs activating.

<!-- … -->

---

<!--
DEGRADED OUTPUT — what the same run produces when no summarizer is reachable.
Note it deliberately BREAKS design rule 4 (explicit absence): sections are
omitted rather than emitted as "none", because "never produced" and "empty"
must not be confusable. The header says which case this is.

---
type: meeting
title: "2025-11-30 meeting, 2 speakers"     # no AI one-liner to pre-fill; prompted title or fallback
date: 2025-11-30
duration: "18:15"
participants: ["Speaker 1", "Speaker 2"]
speakers_identified: false
summarizer: none
summary_status: unavailable
turns: 168
---

# 2025-11-30 meeting, 2 speakers

Meeting on 2025-11-30, 2 speakers, 18 minutes, audio `Generative_User_Interfaces.m4a`.
**No AI summary was generated for this meeting** (summarizer unreachable). The
transcript below is complete. Abstract, decisions, action items, open questions
and topic sections are absent because they were never produced — not because
they were empty.

## Transcript
…
-->

<!--
IDENTIFIED-SPEAKER VARIANT — what changes once speaker identification lands:
  participants: [Joseph, Dana]
  speakers_identified: true
  …and the header loses the "Speakers were not identified" sentence, prose uses
  real names, and turn lines read **[T003 00:10] Dana:**. Nothing else changes.
-->
