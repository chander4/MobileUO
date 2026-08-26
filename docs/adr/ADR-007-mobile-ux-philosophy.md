# ADR-007: Mobile UX Philosophy

## Context

ClassicUO's UI code assumes a mouse throughout: hover state precedes click state,
right-click opens context menus, dragging is pixel-precise, and modifier keys
(shift, ctrl) exist. Touch has a fundamentally different event shape — there is no
"hover before press," a long touch is a distinct gesture from a tap, and there is no
right mouse button. Patching individual symptoms as they were discovered (bigger
buttons, a joystick for movement) left the project without a shared answer to "what
does a touch gesture mean, consistently, everywhere."

Concretely, this gap caused a real bug during #24: the engine's own touch-to-mouse
translation layer synthesizes hover *before* dispatching a press, and a
context-sensitive button reacted to that synthetic hover by nulling its own state
before the press it was hovering for ever arrived — a bug that only exists because
touch was being simulated as slightly-wrong mouse input instead of treated as its
own input model.

## Decision

Touch gets first-class, named semantics instead of being simulated as approximate
mouse input:

- **Tap** = the primary action for whatever's under the finger (maps to left-click).
- **Long-press** (500ms threshold, <5px movement tolerance) = the consistent
  "secondary/destructive" gesture, replacing right-click contextually: closing a
  health bar, removing an Action Bar entry. One threshold, one tolerance, used
  everywhere it applies — not reinvented per feature.
- **Context-sensitive resolution** (the Primary Action pipeline, ADR-003) replaces
  right-click context menus outright rather than trying to simulate a menu on
  touch.
- Where the engine's touch-to-mouse translation has structural gaps (the
  hover-before-press ordering above), Hollowmere's own code defends against them
  explicitly (`IsPressed`/`IsHovered` guards) rather than working around symptoms
  case by case.

## Alternatives considered

- **Simulate a mouse as faithfully as possible and leave ClassicUO's native
  interaction model otherwise untouched.** Rejected — this is what produced the
  hover-before-press bug and the double-tap dispatch bug; treating touch as "mouse,
  but weirder" keeps surfacing the same class of bug because the translation is
  lossy in ways that only show up under real touch timing.
- **A generic gesture-recognition library.** Rejected as premature — Hollowmere
  needs exactly two gestures (tap, long-press) with one consistent set of
  thresholds, not a general gesture framework built for gestures nothing currently
  needs (swipe, pinch is handled separately and locally where it's genuinely
  needed, e.g. the world map).
- **Per-feature gesture thresholds** (each new long-press feature picks its own
  timing). Rejected — inconsistent thresholds across features is exactly the kind
  of thing a player learns to distrust; one constant, reused, is a small technical
  choice with an outsized consistency payoff.

## Consequences

- Every long-press feature going forward should use the same 500ms/<5px convention
  by default, deviating only with a stated reason.
- New touch-facing code needs to account for the engine's synthetic-hover-before-
  press ordering explicitly, not assume mouse-equivalent event ordering.
- Desktop mouse users are not a secondary consideration bolted onto touch handling
  — the same event path serves both, so touch-specific fixes need to be verified
  not to regress mouse behavior (and vice versa).

## Future implications

- Controller support (ROADMAP_VISION.md) will need its own answer to "what gesture
  vocabulary does a controller map to" — likely reusing the same
  tap/long-press-equivalent semantics (press vs. hold a face button) rather than
  inventing a third parallel model.
- If a genuine need for richer gestures arrives (swipe-to-dismiss, multi-finger
  shortcuts), evaluate then whether ad hoc local handling still suffices or whether
  it's time for the shared gesture layer that was deliberately not built now.
