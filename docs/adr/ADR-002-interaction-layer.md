# ADR-002: Interaction Layer

## Context

ClassicUO's native interaction model assumes a mouse: hover for a tooltip,
left-click to select, right-click for a context menu, drag with pixel precision.
Touch has none of these primitives natively. Early mobile work patched individual
symptoms (bigger buttons, a joystick bolted onto movement) without a shared model
for "what is a player action, and how does touch express it."

That gap surfaced concretely during #24: a button needed to execute "whatever the
correct verb is for the current selection," and there was no existing seam between
"the player wants to do a thing" and "here is the UI element that does it."

## Decision

Introduce `Assets/Scripts/Hollowmere/Interaction/` as a dedicated layer sitting
between the Engine (ClassicUO's native `GameActions`, `SelectedObject`, `UIManager`)
and Presentation (Unity scenes, the `HollowmereUI` theme system). Its job is to
represent "things a player can do" as data (`GameActionDefinition`) and "touch
gestures" as reusable primitives (long-press, context-sensitive resolution),
independent of any specific button or screen.

This is deliberately a *layer*, not a single class — it's meant to be the place
future interaction work (radial menus, controller input, accessibility-driven
input) plugs into, without each one reinventing what "a player action" means.

## Alternatives considered

- **Solve each interaction need inline where it's used** (e.g., write the
  context-sensitive logic directly inside the button's Gump class). Rejected — this
  is exactly the pattern that left #24 with no shared model to build on; it would
  repeat the same gap for the next feature.
- **Adopt a generic, engine-agnostic "input framework" package.** Rejected as
  premature — Hollowmere has one concrete need (bridge ClassicUO's mouse-shaped
  action model to touch), not a general problem needing a general solution. Build
  the specific thing that's needed; generalize later if a second, different need
  shows up.
- **Push touch-gesture logic into the Engine layer** (e.g., directly into
  `GameController.cs` or `UIManager.cs`). Rejected — those files are already large
  and carry enough responsibility bridging XNA to Unity; gameplay-facing gesture
  semantics (what a long-press *means* to the player) belong closer to gameplay.

## Consequences

- Every new "thing a player can tap" should be expressible as a
  `GameActionDefinition`, not a bespoke `OnMouseDown` handler. This is now the
  default path, and deviating from it should be a deliberate, justified choice.
- The layer currently owns: `GameActionDefinition`, `ActionButtonGump`, `ActionBar`,
  the long-press primitives, and the Primary Action pipeline (see ADR-003).
- New interaction surfaces (a radial menu, a controller D-pad mapping, an
  accessibility-driven action list) have a clear, existing seam to build on instead
  of inventing their own.

## Future implications

- As this layer grows, watch for it accumulating Engine-layer knowledge it
  shouldn't have (see ADR-006's boundary discussion) — it should stay a consumer of
  `GameActions`/`SelectedObject`, not a second implementation of them.
- Controller support and accessibility-driven input (ROADMAP_VISION.md) are the next
  real tests of whether this layer's abstractions (`GameActionDefinition` in
  particular) generalize past touch, or whether they're quietly touch-specific and
  need revisiting.
