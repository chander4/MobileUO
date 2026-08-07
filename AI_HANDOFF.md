# Hollowmere AI Handoff

This is a lightweight continuation snapshot, not an architectural source of truth. Read `AGENTS.md` and `PROJECT_STATE.md` first; update this file whenever a milestone changes hands or reaches a meaningful stopping point.

## Current branch and release

- **Branch:** `controls-update`
- **Last released tag:** `v0.2.0-mobile-foundation` (`aff5e8a`)
- **Working-tree status:** confirm with `git status` before relying on this line.

## Current milestone

**Issue #25A is complete** (HUD layout contract foundation). **Next approved work: Issue #25B** (`HudLayoutService`).

## Issue #25A — what exists now

Renderer-neutral HUD infrastructure under `Assets/Scripts/Hollowmere/Hud/`:

| Piece | Notes |
|--------|--------|
| `HudWidgetRegistry` | Stable IDs, aliases, deprecated tombstones, `Validate()` |
| Profile model | `HudLayoutProfile`, variants, `HudWidgetState`, `HudWidgetSettings` |
| Serialization | `HudLayoutProfileSerializer` — schema v1, migrate from v0 |
| Placement | `HudPlacement`, `HudPlacementMath` (safe area, dock, snap, orientation transform) |
| Storage | `IHudLayoutStorage`; `PlayerPrefsHudLayoutStorage` in `Hollowmere.Hud.Unity` |

**Tests:** `Assets/Tests/Hud/` — `Hollowmere.Hud.Tests` (EditMode). Suites: registry, profile, placement, storage contract (`InMemoryHudLayoutStorage`), migration fixtures. Run via Unity Test Runner; `com.unity.test-framework` is a direct package dependency.

**Not implemented:** `HudLayoutService`, `HudWidgetHost`, edit mode, presenters, any scene/bootstrap wiring. **No visible gameplay change.**

**Serializer note:** `Migrate` / `Serialize` may mutate the input profile — TODO in `HudLayoutProfileSerializer`; do not redesign until host/service lifecycles exist.

## Active work and blockers

None known for #25B. Architecture is accepted (`ADR-008`, `HUD_WIDGET_ARCHITECTURE.md`). Do not redesign unless implementation exposes a real flaw.

## Expected sequencing (product, not ClassicUO upstream)

1. **#25B** — `HudLayoutService` (defaults, variant fallback, profile resolution, migration decisions).
2. **#26** — `HudWidgetHost` (lifecycle, geometry application; **consumes** layout service).
3. **Later** — Primary Action migration as its own milestone (not bundled with host).

HUD layout is **Hollowmere product architecture**, not ClassicUO engine work — do not propose widget registry / mobile profiles upstream.

## Protected systems

Do not change `ClientRunner` movement logic, Networking, packet serialization, or the asset-loading pipeline without explicit approval. Authoritative lists: `DECISIONS.md`, `CLAUDE.md`.

## Diagnostics state

`Hollowmere.Diagnostics.Diag` — categories off by default (`ADR-005`). No active diagnostics milestone.

## Files to read before #25B

`docs/architecture/HUD_WIDGET_ARCHITECTURE.md` (§6 `HudLayoutService`, §13 variant fallback), `DECISIONS.md` (HUD widget system), `docs/development/TESTING_STRATEGY.md`.
