# ADR-001: Engine Fork Philosophy

## Context

Hollowmere began as "a thin Unity wrapper around ClassicUO" — symlink the upstream
client's source into `Assets/`, override a handful of files where mobile/Unity
genuinely needed something different, build gameplay on top. That description was
accurate for a while and stopped being accurate without anyone deciding it should.

A full audit (2026-08-06) found 73 files under `Assets/Scripts/ClassicUO/src/` that
are real, modified copies of upstream source — spanning rendering, input dispatch,
networking, asset loading, and UI. Only ~10 of them were ever documented. The rest
accumulated because the "thin wrapper" framing made each individual override feel
like a one-off exception rather than part of a pattern, so nobody stepped back to
name the pattern.

## Decision

Hollowmere is a **maintained ClassicUO engine fork**, not a thin wrapper with
exceptions. This is a naming and posture change, not a technical one — the symlink
mechanism, the override workflow, and the submodule relationship are unchanged. What
changes is that a modified engine file is now treated as first-class Hollowmere code:
tracked normally in git, documented in `docs/architecture/ENGINE_MODIFICATIONS.md`,
held to the same review standard as gameplay code — not waved through as "just an
override, don't worry about it."

## Alternatives considered

- **Stay a thin wrapper; push every mobile-specific need upstream instead.** Rejected
  — ClassicUO's upstream maintainers aren't obligated to prioritize a mobile-first
  UI rewrite, and Hollowmere needs to ship on its own timeline. Some fixes (like the
  `DenyMoveItem` recovery bug) are genuinely upstream-worthy and should be
  contributed back, but that's a parallel track, not a gate on Hollowmere's own
  progress.
- **Vendor a full copy of ClassicUO instead of a submodule + symlinks.** Rejected —
  loses the ability to track upstream fixes at all, and turns every override into an
  untrackable diff against a moving, uncommitted target.
- **Formal upstream/downstream branching model (rebase Hollowmere's changes onto
  each ClassicUO release).** Deferred, not rejected — worth revisiting once the
  override surface stabilizes, but premature while the fork is still actively
  growing.

## Consequences

- Engine changes now require the same rigor as gameplay changes: a stated reason,
  a diff someone can review, a place in the inventory doc.
- The "is this file safe to edit" question has a clear answer: check if it's still
  a symlink (don't touch it there — convert it first) or already a tracked override
  (edit it, document the change).
- Merging upstream ClassicUO updates becomes real, ongoing maintenance work, not a
  hypothetical. 73 files (and growing) is enough surface area that a naive upstream
  merge will produce real conflicts.
- The project needs an explicit override policy (see ADR-006) now that "override" is
  a named, permanent category instead of an implicit exception.

## Future implications

- Expect the override count to keep growing as more of ClassicUO's mouse/desktop
  assumptions get replaced. That's expected, not a warning sign — but it means the
  inventory doc needs to be kept current, not written once and left stale.
- A future milestone should evaluate whether some overrides are stable enough to
  formally propose upstream, both to reduce fork maintenance burden and as good
  open-source citizenship.
- If the fork keeps growing, revisit whether a lighter-weight upstream-tracking
  strategy (e.g., a proper git remote + cherry-pick workflow instead of a
  submodule + symlinks) is worth the migration cost.
