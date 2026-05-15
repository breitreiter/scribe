# Substrate log

Append-only chronological record of substrate changes and gnome
sweep findings. Entries lead with `## [date] kind | title` so simple
grep works (`grep "^## \[" log.md | tail -20`).

---

## [2026-05-15] promote | applied P-2026-05-15-001 through P-2026-05-15-004

Category: migration. Source: project-migrate-skill:M-2026-05-15-1652.
Affected: plans/input-flexibility.md (created), plans/report-design.md (created),
imp/reference/azure-services-setup.md (created), DESIGN.md (superseded_by),
REPORT_DESIGN.md (superseded_by), SETUP.md (superseded_by).
Deferred: REQUIREMENTS.md (unknown shape, needs human classification).

## [2026-05-15] init | substrate created

Substrate initialized via `imp init`. Layout: gnome-maintained
`learnings/`, `reference/`, `concepts/`, `_index/`, `note/`,
`log.md` under `imp/`; human-owned `plans/`, `bugs/`, `TODO.md`,
`rules/` at repo root.
