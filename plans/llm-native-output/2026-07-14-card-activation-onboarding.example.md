# Worked example — six-speaker research session

This is the **spec** for `MeetingMarkdownWriter`. Content is synthetic; the shape
is not. Grilled 2026-08-06; four decisions from that pass are recorded at the
bottom and are already applied here.

The meeting is deliberately short (13 minutes, 22 turns) so the transcript can be
shown **complete**. That is the point: a real 227-turn meeting produces the same
file, just longer. Nothing is elided, ever.

It exercises what six speakers actually produce: roles, a folded backchannel
leaving an explained ID gap, and one diarization label the human could not
identify and flagged as possibly holding two voices.

---

```markdown
---
date: 2026-07-14
title: Card activation onboarding — session 3 debrief
slug: 2026-07-14-card-activation-onboarding
purpose: Debrief the unaided card-activation task with both customers present, and decide whether in-app activation ships this quarter.
duration: 0:13:44
media: GMT20260714-140012_Recording.m4a
speakers_identified: partial
summary_status: ok
participants:
  - name: Dana Okafor
    role: researcher (facilitator)
  - name: Priya Raman
    role: customer
  - name: Tom Alderidge
    role: customer
  - name: Marcus Webb
    role: product manager
  - name: Ines Barros
    role: engineer
  - name: Unidentified speaker 6
    role: unknown
    flagged: may contain more than one voice
topics:
  - What the customers did with the envelope
  - Why neither found an activate button
  - Whether in-app activation ships this quarter
---

# Card activation onboarding — session 3 debrief

Meeting on 2026-07-14, 6 participants, 13 minutes, recording `GMT20260714-140012_Recording.m4a`.
Held to debrief the unaided card-activation task with both customers present, and
to decide whether in-app activation ships this quarter. Five speakers were
identified by a human. One diarization label could not be identified and was
flagged as possibly holding more than one voice; its turns read "Unidentified
speaker 6" and no identity is claimed for them.

## Abstract

*(Card activation onboarding session 3 debrief, 2026-07-14, 6 participants.)*

Dana Okafor (researcher) debriefed an unaided activation task with the two
customers who had attempted it. Neither Priya Raman (customer) nor Tom Alderidge
(customer) found an in-app activation control, because none exists; Priya Raman
concluded the card was already active, and Tom Alderidge used the phone number on
the sticker, a method he knew from another bank. Ines Barros (engineer) confirmed
the activation API is complete and only the interface is missing. Marcus Webb
(product manager) accepted that the shipped flow does not match the intended
design, committed to scoping in-app activation this quarter, and decided the
sticker stays regardless. Ownership of the design work was raised and not settled.

## Decisions

*(Card activation onboarding session 3 debrief, 2026-07-14, 6 participants.)*

**D-T017 — In-app activation is scoped for the current quarter.** [T017]
Marcus Webb (product manager) committed to this after both customers failed the
task and after Ines Barros (engineer) confirmed the API already supports it.
Rationale given: the feature was cut to make an earlier date and was never
revisited.

**D-T020 — The sticker and its phone number stay in the envelope.** [T020]
Marcus Webb decided this holds even once in-app activation ships, because a
customer without the app would otherwise have no activation path at all.

## Action items

*(Card activation onboarding session 3 debrief, 2026-07-14, 6 participants.)*

- **Scope in-app activation for the current quarter.** — Marcus Webb (product manager) [T017]
- **Confirm which fields the activation API call requires.** — Ines Barros (engineer) [T016]
- **Share the clip of Priya Raman's envelope attempt with the design team.** — Dana Okafor (researcher) [T002]

## Open questions

*(Card activation onboarding session 3 debrief, 2026-07-14, 6 participants.)*

- Who designs the in-app activation screen? Dana Okafor (researcher) raised that
  no designer was present; no owner was named. [T019]
- Should the activation control be available before the physical card arrives?
  Raised by Unidentified speaker 6 and not answered. [T012]

## Key points

*(Card activation onboarding session 3 debrief, 2026-07-14, 6 participants.)*

- Both customers read the printed material before touching a device; neither
  opened the app first. [T001, T004]
- Priya Raman (customer) assumed the card was already active because no
  activation step was presented anywhere. [T008]
- Tom Alderidge (customer) found the sticker number quickly, having used the same
  method at another bank. [T006]
- The activation API is complete and unexposed — the gap is interface-only.
  [T016]

## Topics

*(Card activation onboarding session 3 debrief, 2026-07-14, 6 participants.)*

### 0:00:00–0:04:12 — What the customers did with the envelope

*(Card activation onboarding session 3 debrief, 2026-07-14.)*

Dana Okafor (researcher) asked both customers to describe what they did on
opening the envelope. Priya Raman (customer) and Tom Alderidge (customer) each
read the printed insert before picking up a phone.

### 0:04:12–0:09:30 — Why neither found an activate button

*(Card activation onboarding session 3 debrief, 2026-07-14.)*

Neither customer found an activation control in the app. Ines Barros (engineer)
explained that the API supports activation and no screen calls it. Marcus Webb
(product manager) traced the omission to an earlier scope cut.

### 0:09:30–0:13:44 — Whether in-app activation ships this quarter

*(Card activation onboarding session 3 debrief, 2026-07-14.)*

Marcus Webb (product manager) committed to scoping the work this quarter and to
keeping the sticker regardless. Design ownership was raised and left open.

## Transcript

*(Card activation onboarding session 3 debrief, 2026-07-14, 6 participants.
Complete and verbatim. Short acknowledgements are folded into the turn they
interrupt; the folded turn's ID is shown inline, so every ID is accounted for.)*

### 0:00:00–0:04:12 — What the customers did with the envelope

*(Card activation onboarding session 3 debrief, 2026-07-14.)*

[T000 0:00:00] Dana Okafor (researcher): Thanks for staying on, both of you. I want to walk back through what you each did when the envelope arrived, and I'd rather hear it in your words than mine.

[T001 0:00:34] Priya Raman (customer): I opened it at the kitchen table. There was the card, and a folded leaflet thing. I read the leaflet first because it looked like it was telling me what to do.

[T002 0:01:12] Priya Raman: And it doesn't say activate anywhere on it. [T003 folded: Dana Okafor: Mm-hmm.] So my assumption was that it's ready to use — it came from the bank, it's got my name on it, why would there be another step.

[T004 0:01:58] Tom Alderidge (customer): Mine was the same, except I was looking for a sticker. That's the bit I remember from the last one I got.

[T005 0:02:40] Dana Okafor: When you say the last one — that's a card from somewhere else?

[T006 0:03:20] Tom Alderidge: Different bank, couple of years ago. There was a sticker on the front with a number, you ring it, you say some digits, done. So I went looking for that and it was there, so I rang it.

### 0:04:12–0:09:30 — Why neither found an activate button

*(Card activation onboarding session 3 debrief, 2026-07-14.)*

[T007 0:04:12] Dana Okafor: Did either of you open the app at any point during that?

[T008 0:04:45] Priya Raman: Not until afterwards. And when I did, there was nothing about activating — which is why I thought it must already be done. [T009 folded: Tom Alderidge: Same.]

[T010 0:05:30] Ines Barros (engineer): I should say — the API supports this. You can activate a card from the app today as far as the backend is concerned. There just isn't a screen anywhere that calls it.

[T011 0:06:20] Marcus Webb (product manager): That's on us. It was in the original design and it came out when we were cutting to make the date.

[T012 0:07:05] Unidentified speaker 6: Could it not be in the app before the card physically turns up? You know it's coming, you get the email.

[T013 0:07:40] Ines Barros: Possibly, but I'd want to check what the call actually needs before anyone promises that. I think it's the last four digits and the expiry, and I don't want to say that and be wrong.

[T014 0:08:35] Priya Raman: From my side it would just be nice to be told there's a step. Even if the step is ringing a number.

### 0:09:30–0:13:44 — Whether in-app activation ships this quarter

*(Card activation onboarding session 3 debrief, 2026-07-14.)*

[T015 0:09:30] Dana Okafor: So where does that leave the in-app path?

[T016 0:09:58] Ines Barros: Buildable. It's interface work, not backend work. I'll confirm the fields this week.

[T017 0:10:40] Marcus Webb: Then I'll scope it for this quarter. [T018 folded: Ines Barros: Okay.] What we shipped isn't what we designed, and it's been a year of nobody going back to it.

[T019 0:11:35] Dana Okafor: Who's drawing that screen, though? There's no designer in this room and there wasn't one in the last two sessions either.

[T020 0:12:20] Marcus Webb: I'll find that out. But the sticker stays either way — if someone hasn't installed the app, taking the number off leaves them with nothing at all.

[T021 0:13:02] Unidentified speaker 6: That's the part I'd want to put back in front of people once it's built.
```

---

## Settled by grilling (2026-08-06)

| Question | Decision |
|---|---|
| Complete transcript or a selection? | **Complete, always.** ID gaps mean folding and nothing else. A selection would make gaps ambiguous and could leave a cited turn absent from the file. |
| How much roster per section stamp? | **Compressed stamp; role inline at first mention within each section.** The role travels with the claim, so it survives in the chunk that carries the claim, and cost scales with people mentioned rather than people present. |
| Two or more still-unnamed labels? | **Kept distinct by display ID** ("Unidentified speaker 6"). Collapsing them to one string would merge distinct voices into an apparent single person — a fabricated coherence, the same error as inventing a name. |
| Do decisions need stable IDs? | **Derived from evidence**: `D-T017` is the decision first cited at turn T017. Ordinal `D1`/`D2` go stale silently across re-summarization, which now happens on every speaker rename. Collisions take a suffix. |

Two further commitments from the earlier draft, unchanged:

- **`speakers_identified` is tri-state** (`all` | `partial` | `none`), not the bool
  the plan specified. With a flagged label, `false` understates a file where five
  of six people are named.
- **Folded backchannels name their speaker** — `[T003 folded: Dana Okafor: Mm-hmm.]`
  — rather than the `[S2: Yeah.]` in rule 8. With identified speakers, `S2` is a
  second competing speaker vocabulary in the same file.

## The degraded variant

When no summarizer was reachable, the AI-derived sections are **omitted** rather
than emitted empty, and the header says so:

```markdown
---
date: 2026-07-14
title: Card activation onboarding — session 3 debrief
duration: 0:13:44
media: GMT20260714-140012_Recording.m4a
speakers_identified: partial
summary_status: unavailable
participants:
  # …as above…
---

# Card activation onboarding — session 3 debrief

Meeting on 2026-07-14, 6 participants, 13 minutes, recording `GMT20260714-140012_Recording.m4a`.

**No summary was produced for this meeting.** The summarizer was unreachable when
this file was written, so it contains the transcript only. The absence of
decisions, action items and open questions below is not a finding: nothing looked
for them.

## Transcript

*(Card activation onboarding session 3 debrief, 2026-07-14, 6 participants. Complete and verbatim…)*
```

Contrast with a section that was produced and is genuinely empty:

```markdown
## Decisions

*(Card activation onboarding session 3 debrief, 2026-07-14, 6 participants.)*

No decisions were taken in this meeting.
```

"Nothing was decided" and "nobody looked" must never be confusable by a model
reading one chunk. This is the one case where rule 4's explicit-absence
requirement inverts, and it is the branch most likely to rot untested.
