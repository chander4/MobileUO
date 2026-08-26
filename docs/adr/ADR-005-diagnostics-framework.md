# ADR-005: Diagnostics Framework

## Context

Diagnosing #24's touch-dispatch bugs (a tap silently lost after a world
interaction, an item "reconnecting" to the cursor after pickup, a denied server move
vanishing an item) required extensive `Debug.Log` scaffolding scattered across seven
files spanning the Engine and Gameplay layers. Once the bugs were root-caused, the
instinct was to delete all of it — but the debugging session made clear that these
exact categories of bug (touch dispatch, `ItemHold` state, action execution,
networking edge cases) are structural, not one-off. The next one will need the same
kind of visibility, in the same places.

## Decision

Replace throwaway `Debug.Log` calls with `Hollowmere.Diagnostics.Diag`: a static,
category-based logger (`DiagCategory`: `Input`, `UI`, `ItemHold`, `Actions`,
`Networking`, extensible) where every category defaults **off** and costs a single
bool-array read when disabled. Call sites are required to guard with
`Diag.IsEnabled(category)` before building the log message, so a disabled category
pays nothing for string interpolation or allocation — not just "doesn't print," but
"doesn't do the work to decide what it would have printed."

Diagnostic code that proved generally useful during #24 was converted into
permanent `Diag` calls at their original sites, rather than deleted; only genuinely
one-off investigation logs (the throwaway validation-test harnesses) were removed.

## Alternatives considered

- **Delete all debugging code once the bug is fixed.** Rejected — this was the
  default instinct and the user explicitly overrode it: "build permanent engine
  diagnostics rather than deleting valuable debugging capability." The cost of
  re-adding equivalent tracing for the *next* bug in the same subsystem is higher
  than the cost of a well-designed permanent framework.
- **Leave `Debug.Log` calls in place, always-on.** Rejected — unconditional logging
  in hot paths (per-frame input dispatch, per-packet networking) has a real,
  permanent performance cost and floods the console for anyone not actively
  debugging that subsystem.
- **A full structured-logging library (levels, sinks, structured fields).**
  Rejected as overbuilt — Hollowmere has one need (toggle tracing per subsystem,
  pay nothing when off), not a general logging-infrastructure problem. The category
  enum plus a guarded static method is the smallest thing that solves it.

## Consequences

- Every future investigation into touch dispatch, `ItemHold` state, action
  execution, or networking edge cases starts with `Diag.SetEnabled(category, true)`
  instead of re-adding `Debug.Log` calls from scratch.
- New categories should be added deliberately, matching a real subsystem, not
  reused across unrelated concerns just because a category already exists.
- Call sites carry a small, permanent verbosity cost (the `if (Diag.IsEnabled(...))`
  guard) even when disabled — accepted as the price of true zero-cost logging
  content.

## Future implications

- No toggle UI exists yet — categories are flipped via a direct code call
  (`Diag.SetEnabled(...)`), which is fine for developer use but not for field
  diagnostics from a live device. A future milestone could wire categories into the
  Options Gump or a hidden debug menu if remote/field diagnosis becomes a real need
  — deliberately not built now, since it hasn't been needed yet.
- As more subsystems adopt `Diag`, watch for category sprawl (a new category per
  feature instead of per subsystem) diluting the "toggle one thing, see one
  subsystem" value the design is built around.
