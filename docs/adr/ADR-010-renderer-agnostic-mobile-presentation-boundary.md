# ADR-010: Renderer-Agnostic Mobile Presentation Boundary

## Status

Accepted. This ADR establishes an architectural constraint; it authorizes no
implementation, refactor, or renderer change by itself. See Scope below.

## Context

Hollowmere's north star is a modern mobile UO client for phones and tablets.
ClassicUO's existing 2D renderer must remain the primary, fully-supported
presentation path — not a legacy system pending replacement. A future Unity
3D renderer, if it ever happens, is an *alternative* presentation option
that coexists with the 2D renderer, not a migration destination for it.

This session's own investigation (a read-only 3D-feasibility audit, followed
by live GameScene/renderer inspection) established, concretely rather than
speculatively, that Hollowmere already has most of the boundary this
direction needs — it has just never been written down as a rule future work
is checked against:

- `HudWidgetRendererKind` (ADR-008) already treats Gump and Unity
  presentation as distinct renderer kinds, not a single assumed path.
- `PrimaryActionResolver`, `PrimaryAction`, and `ActionCatalog` are
  renderer-independent data/classification systems — `ActionButtonGump`
  consumes that data rather than owning the underlying action logic.
- ClassicUO entity state (`X`/`Y`/`Z`/`Serial`/`Graphic`) is plain data,
  never inherently coupled to how it's drawn.
- `WorldEntityScreenAnchor` (this session's POC) demonstrates a working
  world-state → screen-presentation boundary that required zero engine
  changes, precisely because the state it reads was already
  renderer-agnostic.
- ADR-009 already separates world-anchored presentation from screen-anchored
  HUD as two distinct layers — the same boundary this ADR generalizes,
  previously stated only as a UX rule.

What's missing is not the boundary itself — it's a name for it, and a
standing question new mobile UI work gets checked against before it quietly
assumes ClassicUO 2D is the only renderer that will ever exist.

## Decision

1. ClassicUO's 2D renderer is a first-class, fully-supported presentation
   path. Nothing in this ADR deprecates it, slows it down, or treats it as
   provisional.
2. Mobile UI and presentation logic must not depend on a specific world
   renderer unless that dependency is explicit and deliberately
   renderer-specific (a Gump-only interaction, a Unity-UI-only layout).
3. Game state, commands, actions, and other presentation-independent data
   remain renderer-agnostic wherever practical — preserving the property
   `PrimaryActionResolver`/`PrimaryAction`/`ActionCatalog`/entity state
   already have, not introducing a new pattern.
4. Renderer-specific behavior lives behind an explicit presentation/
   rendering boundary — the widget presenter, the Gump, the Unity component
   — never inline inside domain or command logic.
5. Future 3D support, if and when it happens, is additive and coexistent
   with the current 2D renderer, not a replacement for it or a migration
   off of it.
6. New mobile UI work is evaluated against this boundary before
   implementation — "does this assume ClassicUO 2D is the only renderer, or
   does it only consume renderer-agnostic state/commands" — the same way
   HUD work already gets checked against ADR-008 and ADR-009.
7. No renderer-selection abstraction, third presentation kind, or feature-
   flag system is introduced until a second concrete renderer creates a
   demonstrated need for one. This ADR is the constraint; it is not the
   design for a 3D renderer that doesn't exist yet.

## Alternatives considered

### A generalized `RendererService`/renderer-selection abstraction now

Rejected — no second renderer exists yet to select between. Building an
abstraction for a hypothetical second renderer risks guessing its shape
wrong and paying real complexity for zero current benefit — the same
reasoning ADR-008 already applied to rejecting a generic `WidgetController`
ahead of a second concrete consumer.

### A third `HudWidgetRendererKind` value now

Rejected — the existing two-value `Gump`/`Unity` enum has no third renderer
to represent yet. Extend it when an actual 3D-rendered widget needs to say
so, not before.

### Renderer feature flags now

Rejected for the same reason — a flag needs two real, working states to
toggle between. Today there is exactly one working world renderer.

### Refactoring existing renderer code for theoretical future support

Rejected. `GameScene.DrawWorld()`, `UltimaBatcher2D`, and the rest of
ClassicUO's rendering path are untouched by this ADR. This decision governs
where *new* mobile UI work sits, not preparation of the existing renderer
for a replacement it isn't getting.

### Designing the future 3D renderer now

Rejected — this ADR fixes the boundary future 3D work would need to
respect, not the 3D work itself. Designing it now would mean committing to
specifics with no second data point (a working 3D prototype) to design
against — the same "wait for a second consumer" discipline ADR-008 and
ADR-009 already apply elsewhere.

## Consequences

### Positive

- Mobile UI (Action Bar, Primary Action, Vitals, Target Vitals, Backpack,
  and everything that follows) can keep evolving without becoming a
  dependency of ClassicUO's 2D renderer specifically.
- ClassicUO's existing rendering path is explicitly protected — nothing
  here asks it to change, slow down, or become legacy.
- If 3D experimentation happens later, it can be evaluated and built
  without first requiring a rewrite of `PrimaryActionResolver`,
  `ActionCatalog`, or any other command/state system — those are already
  positioned correctly.
- Future mobile UI work has one consistent question to check itself
  against, the same way ADR-009's eight rules already give HUD work a
  consistent check.

### Negative / tradeoffs

- Some future features will legitimately need a renderer-specific adapter
  (a Gump-only drag-to-anchor interaction, a future 3D-only camera control)
  — this ADR doesn't remove that need, it only defers the generalized
  version until a second renderer makes it concrete.
- If and when a real second renderer arrives, some presentation abstraction
  will be needed that doesn't exist today — this ADR defers that cost, it
  doesn't eliminate it.
- Some existing ClassicUO systems (the render loop itself, pixel-perfect
  hit-testing via `SelectedObject`) are tightly coupled to the 2D renderer
  by design and won't become renderer-agnostic just because this ADR
  exists — that coupling is correct and expected for engine-owned
  rendering, not a violation of this boundary.
- Every new feature now carries one more explicit judgment call — "does
  this belong on the state/command side or the renderer-specific side" —
  usually obvious, occasionally not, and getting it wrong quietly re-couples
  mobile UI to one renderer.

## Scope

This ADR governs how future mobile UI and presentation work is evaluated;
it does not, by itself, authorize or require any implementation, refactor,
or renderer change. Consistent with Architectural Principle #10, it is
recorded now because it is a durable, cross-cutting boundary — not because
any current code violates it or needs to change today. No source files,
assets, scenes, prefabs, renderer code, or existing UI are touched by this
ADR.
