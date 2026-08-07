# Worked example — six-speaker research session

This is the **spec** for `MeetingMarkdownWriter`. Everything between the rules
below is what scribe must emit, byte-for-byte in structure. Content is synthetic;
the shape is not.

It deliberately exercises the awkward cases a two-speaker example cannot:
six participants with **roles**, a **folded backchannel** leaving an ID gap, a
speaker label the human **flagged as more than one person**, and a topic boundary
that falls mid-conversation.

---

```markdown
---
date: 2026-07-14
title: Card activation onboarding — research session 3
slug: 2026-07-14-card-activation-onboarding
purpose: Watch two customers activate a new card unaided, to decide whether in-app activation ships this quarter.
duration: 0:52:18
media: GMT20260714-140012_Recording.m4a
speakers_identified: partial
summary_status: ok
participants:
  - name: Dana Okafor
    role: researcher (facilitator)
  - name: Priya Raman
    role: customer (participant)
  - name: Tom Alderidge
    role: customer (participant)
  - name: Marcus Webb
    role: product manager
  - name: Ines Barros
    role: engineer
  - name: unidentified
    role: unknown
    note: One diarization label was flagged as containing more than one voice; its turns are attributed to "Unidentified speaker".
topics:
  - Getting the card out of the envelope
  - The missing activate button
  - Whether in-app activation ships this quarter
---

# Card activation onboarding — research session 3

Meeting on 2026-07-14, 6 participants, 52 minutes, recording `GMT20260714-140012_Recording.m4a`.
Convened to watch two customers activate a new card unaided, to decide whether
in-app activation ships this quarter. Speakers were identified by a human, with
one exception: a single diarization label held more than one voice and was flagged
rather than guessed at — turns from it read "Unidentified speaker" and no identity
is claimed for them.

## Abstract

*(From the 2026-07-14 card activation onboarding research session, 6 participants:
Dana Okafor (researcher), Priya Raman and Tom Alderidge (customers), Marcus Webb
(product manager), Ines Barros (engineer).)*

Two customers were each given a sealed card and asked to activate it without
help. Neither found the in-app activation control, because there is none: both
eventually used the phone number printed on the sticker, which the product team
had shipped as a fallback rather than the primary path. Ines Barros confirmed the
API already supports in-app activation and that only the interface is missing.
Marcus Webb accepted that the current flow does not match the intended design and
committed to scoping in-app activation for the current quarter. The session did
not settle who owns the design work.

## Decisions

*(From the 2026-07-14 card activation onboarding research session, 6 participants:
Dana Okafor (researcher), Priya Raman and Tom Alderidge (customers), Marcus Webb
(product manager), Ines Barros (engineer).)*

**D1 — In-app activation is scoped for the current quarter.** [T041, T043]
Marcus Webb committed to this after both customers failed to find an activation
control and after Ines Barros confirmed the API already supports it. Rationale
given: the sticker phone number was always intended as a fallback, and shipping
it as the only path was a consequence of an earlier scope cut that was never
revisited.

**D2 — The sticker stays in the envelope for now.** [T047]
Marcus Webb decided against removing the printed number when in-app activation
ships, on the grounds that customers without the app would otherwise have no path
at all.

## Action items

*(From the 2026-07-14 card activation onboarding research session, 6 participants:
Dana Okafor (researcher), Priya Raman and Tom Alderidge (customers), Marcus Webb
(product manager), Ines Barros (engineer).)*

- **Scope in-app activation for the current quarter.** — Marcus Webb [T041]
- **Confirm which API fields the activation call needs.** — Ines Barros [T038]
- **Share the recording of Priya Raman's envelope attempt with the design team.** — Dana Okafor [T012]

## Open questions

*(From the 2026-07-14 card activation onboarding research session, 6 participants:
Dana Okafor (researcher), Priya Raman and Tom Alderidge (customers), Marcus Webb
(product manager), Ines Barros (engineer).)*

- Who designs the in-app activation screen? No designer was present and no owner
  was named. [T044]
- Should the activation control appear before a customer has the physical card in
  hand? Tom Alderidge raised it; the session moved on without answering. [T031]

## Key points

*(From the 2026-07-14 card activation onboarding research session, 6 participants:
Dana Okafor (researcher), Priya Raman and Tom Alderidge (customers), Marcus Webb
(product manager), Ines Barros (engineer).)*

- Neither customer looked in the app first; both searched the envelope contents
  for instructions. [T009, T021]
- Priya Raman assumed the card was already active because no activation step was
  presented. [T012]
- Tom Alderidge found the sticker number within a minute, having used the same
  method at another bank. [T023]
- The activation API exists and is unexposed; the gap is interface-only, not
  backend. [T036, T038]

## Topics

*(From the 2026-07-14 card activation onboarding research session, 6 participants:
Dana Okafor (researcher), Priya Raman and Tom Alderidge (customers), Marcus Webb
(product manager), Ines Barros (engineer).)*

### 0:00:00–0:14:22 — Getting the card out of the envelope

*(Card activation onboarding research session, 2026-07-14.)*

Dana Okafor set both customers the same unaided task. Priya Raman and Tom
Alderidge each opened the envelope and read the printed material before touching
a device, which neither had been prompted to do.

### 0:14:22–0:35:40 — The missing activate button

*(Card activation onboarding research session, 2026-07-14.)*

Both customers searched the app for an activation control and did not find one.
Priya Raman concluded the card was already active; Tom Alderidge called the
sticker number. Ines Barros explained that the API supports activation and the
screens do not surface it.

### 0:35:40–0:52:18 — Whether in-app activation ships this quarter

*(Card activation onboarding research session, 2026-07-14.)*

Marcus Webb reviewed the scope cut that removed in-app activation, accepted the
current flow as unintended, and committed to scoping the work. Ownership of the
design was raised and left unresolved.

## Transcript

*(From the 2026-07-14 card activation onboarding research session, 6 participants:
Dana Okafor (researcher), Priya Raman and Tom Alderidge (customers), Marcus Webb
(product manager), Ines Barros (engineer). Verbatim; short acknowledgements are
folded into the surrounding turn and their IDs are skipped.)*

### 0:00:00–0:14:22 — Getting the card out of the envelope

*(Card activation onboarding research session, 2026-07-14.)*

[T007 0:08:14] Dana Okafor: Take your time with it. I'm going to stay quiet — pretend I'm not here and do what you'd normally do at home.

[T009 0:08:41] Priya Raman: Okay. So there's the card, and there's this folded thing. [T010 folded: Dana Okafor: Mm-hmm.] I'd probably read this bit first, because it looks like it's telling me what to do.

[T012 0:09:37] Priya Raman: It doesn't actually say activate anywhere. So I'd assume it's ready to go? Like, it came from the bank, it's got my name on it.

[T021 0:12:50] Tom Alderidge: I'm looking for a sticker. Usually there's a sticker with a number on it, that's how the last one worked.

[T023 0:13:26] Tom Alderidge: There it is. So I'd just ring that. I wouldn't even open the app, honestly.

### 0:14:22–0:35:40 — The missing activate button

*(Card activation onboarding research session, 2026-07-14.)*

[T031 0:19:02] Tom Alderidge: Could it not just be in the app before the card turns up? I know it's coming. I got the email.

[T036 0:28:15] Ines Barros: To be clear, the API supports this. You can activate a card from the app today as far as the backend is concerned — there's just no screen that calls it.

[T038 0:29:44] Ines Barros: I'd want to double check which fields the call actually needs before anyone commits to a date. I think it's the last four and the expiry, but I don't want to say that and be wrong.

### 0:35:40–0:52:18 — Whether in-app activation ships this quarter

*(Card activation onboarding research session, 2026-07-14.)*

[T041 0:37:20] Marcus Webb: Right. I'll scope it for this quarter. [T042 folded: Ines Barros: Okay.] We cut it originally because we were trying to make a date, and then it just never came back around.

[T043 0:38:05] Marcus Webb: What we shipped isn't what we designed. The number on the sticker was meant to be the thing you fall back to, not the thing you do.

[T044 0:39:31] Dana Okafor: Who's actually drawing that screen, though? There's no designer in this room.

[T047 0:44:12] Marcus Webb: We keep the sticker either way. If someone hasn't installed the app, taking the number away leaves them with nothing.

[T052 0:49:58] Unidentified speaker: That's the bit I'd want to test again once it's built.
```

---

## Notes on what this example commits to

Read these against `../llm-native-output.md`'s design rules; each is a place the
example decides something the rules only gesture at.

1. **`speakers_identified` is tri-state, not a bool.** `all` | `partial` | `none`.
   The plan specified a bool, which was written before merge/flag existed. With a
   flagged label, "were the speakers identified?" has a third honest answer, and
   collapsing it to `false` would understate a file where five of six people are
   named.
2. **A flagged label appears in `participants`**, as `name: unidentified` with a
   note, rather than being omitted. A retrieval layer filtering on participants
   must be able to see that someone is missing; an absent entry looks like a
   five-person meeting.
3. **Folded backchannels are named inline** — `[T010 folded: Dana Okafor: Mm-hmm.]`
   — not `[S2: Yeah.]` as the plan drafted. With identified speakers, `S2` would
   be a second, competing speaker vocabulary in the same file.
4. **Timestamps are `H:MM:SS` everywhere**, including topic ranges, because they
   are scrub targets for the Zoom recording named in `media`.
5. **Section stamps name roles, not just names.** The whole reason roles are
   captured is that "the customer said X" is the retrievable claim. A stamp
   listing bare names would throw that away at exactly the granularity chunks
   land on.
6. **Turn IDs are cited bare** (`[T041, T043]`), and turn lines carry ID plus
   timestamp (`[T041 0:37:20]`). A citation resolves by scanning for the ID; the
   timestamp is for the human with the video.

## The degraded variant

When no summarizer was reachable, the AI-derived sections are **omitted** rather
than emitted empty, and the header says so. Frontmatter keeps only what came from
the transcription and the human:

```markdown
---
date: 2026-07-14
title: Card activation onboarding — research session 3
slug: 2026-07-14-card-activation-onboarding
purpose: Watch two customers activate a new card unaided, to decide whether in-app activation ships this quarter.
duration: 0:52:18
media: GMT20260714-140012_Recording.m4a
speakers_identified: partial
summary_status: unavailable
participants:
  - name: Dana Okafor
    role: researcher (facilitator)
  # …as above…
---

# Card activation onboarding — research session 3

Meeting on 2026-07-14, 6 participants, 52 minutes, recording `GMT20260714-140012_Recording.m4a`.

**No summary was produced for this meeting.** The summarizer was unreachable when
this file was written, so it contains the transcript only. The absence of
decisions, action items and open questions below is not a finding: nothing looked
for them.

## Transcript

*(From the 2026-07-14 card activation onboarding research session, 6 participants…)*
```

Note the difference from an empty-but-produced section, which reads:

```markdown
## Decisions

*(From the 2026-07-14 card activation onboarding research session, 6 participants…)*

No decisions were taken in this meeting.
```

"Nothing was decided" and "nobody looked" must never be confusable by a model
reading one chunk. This is the single case where rule 4's explicit-absence
requirement inverts, and it is the branch most likely to rot untested.
