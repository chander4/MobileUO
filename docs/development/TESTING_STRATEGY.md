# Hollowmere Testing Strategy

Hollowmere values player-facing confidence over a coverage target. The project currently has no automated regression suite; this strategy describes the evidence a milestone should produce and where tests add real confidence.

## Unit testing philosophy

Unit-test deterministic, presentation-independent rules: classification, transformation, persistence boundaries, and state machines. Keep tests focused on public behavior and use them where they make regressions cheaper to detect than runtime reproduction. Do not force Unity, Gump, or engine objects into unit tests merely to increase a count.

## Integration testing philosophy

Use integration tests or deterministic runtime harnesses where correctness depends on a Hollowmere–ClassicUO boundary: packet recovery, action dispatch, selection state, persistence round-trips, or Unity-to-engine input translation. Exercise the real boundary and prove a named risk; do not repeat unit tests on either side.

## Runtime validation philosophy

Runtime validation is mandatory for behavior players can touch or see. Check Unity console, scene references, lifecycle behavior, and real input paths. For mobile input, UI, rendering, networking, or interruption resilience, validate on a representative device when possible. Prefer a deterministic probe over repeated manual gestures for recovery or timing-sensitive state.

## Engine testing boundaries

ClassicUO is a maintained upstream engine dependency. Test Hollowmere's intentional overrides and their integration behavior; do not claim coverage of upstream internals merely because a local change passes. Engine changes require ADR-006 discipline, an `ENGINE_MODIFICATIONS.md` entry, and runtime evidence proportionate to risk.

## What must not be duplicated from upstream

Do not create parallel implementations of ClassicUO game rules, packet handling, world state, or Gump-owned game-state rendering merely to make them easier to test. Reuse public engine surfaces and test Hollowmere's composition around them. Unity app chrome must not maintain a second source of game state; see `HOLLOWMERE_UI_RENDERING_STRATEGY.md` and `DECISIONS.md`.

## Evidence to record

The milestone record and `AI_HANDOFF.md` should state what was validated, on what surface (unit, integration, editor, runtime, device), what diagnostics were used, and remaining risks. A passing compile is supporting evidence, never the full completion argument.
