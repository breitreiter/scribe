---
captured: 2026-05-15
repo: scribe
source: human
git-head: 3212a80
---

Bug: AzureSpeechFastService uses a static HttpClient with the default 100-second timeout. Azure Speech Fast Transcription is synchronous but can take well over 100s for long recordings (1hr+ audio). The request will time out before the API responds.

Fix: set `_httpClient.Timeout = Timeout.InfiniteTimeSpan` (or a generous explicit value like 30 minutes) on the static client. Since it's a static field, this needs to be set in a static initializer or constructor before first use.
