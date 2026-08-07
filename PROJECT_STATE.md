# Hollowmere Project State

**This is a living snapshot. Update it after every milestone — not "when
convenient," as part of the milestone's own documentation step (see
`docs/development/DEVELOPMENT_PROCESS.md`).** If this file is stale, trust it less than a fresh
`git log`, but trust it more than a training-data assumption or a prior
conversation's memory.

Last updated: Issue #25A — HUD layout contract foundation (committed).

## Current release

**`v0.2.0-mobile-foundation`** — annotated tag, points to commit `aff5e8a`
("feat: add long-press to close health bars") on branch `controls-update`.

## Current version / tag

```
v0.2.0-mobile-foundation -> aff5e8a
```

Tag message summary: engine overrides tracked, architecture documented,
diagnostics framework established, interaction primitives complete, action
system complete, Primary Action system complete, mobile QoL foundation
established.

## Completed milestones

- **Issue #19A** — Server browser work (`ISSUE_19A_REVIEW.md`).
- **Issue #21 (#21A/#21B)** — Gameplay controls audit and Action Bar system
  (`ISSUE_21_GAMEPLAY_CONTROLS_AUDIT.md`), `docs/adr/0001-hollowmere-ui-theme-
  system.md`.
- **Issue #22** — Universal Action Picker (`ISSUE_22_UNIVERSAL_ACTION_PICKER.md`).
- **Issue #23** — Interruption resilience (`ISSUE_23_INTERRUPTION_RESILIENCE.md`).
- **Issue #24** — Context-sensitive Primary Action button
  (`ISSUE_24_CONTEXT_SENSITIVE_PRIMARY_ACTION.md`), including a root-caused
  touch-dispatch fix and a genuine `DenyMoveItem` recovery bug fix.
- **Engine baseline milestone** — 71 previously-untracked ClassicUO engine
  override files brought under normal git tracking, two `.gitignore` bugs
  fixed, full architecture documentation written (see below).
- **Issue #25A** — HUD layout contract foundation (renderer-neutral registry,
  versioned profile model, placement math, `IHudLayoutStorage` + PlayerPrefs
  adapter). **21+ EditMode tests** in `Hollowmere.Hud.Tests` (registry, profile,
  placement, storage contract, migration fixtures). **No runtime HUD wiring** —
  zero visible gameplay change. Architecture: `ADR-008`,
  `docs/architecture/HUD_WIDGET_ARCHITECTURE.md`.

## Current architecture

Layered: ClassicUO (vendor submodule, untouched) → Hollowmere Engine (73
tracked override files: rendering, input, networking, asset loading, mobile
runtime, UI runtime) → Gameplay (`Assets/Scripts/Hollowmere/*`) → Presentation
(Unity scenes/prefabs, the `HollowmereUI` theme system).

**HUD foundation (Issue #25A, not yet live in scenes):** `Assets/Scripts/Hollowmere/Hud/`
— `HudWidgetRegistry`, `HudLayoutProfile` / variants / widget state & settings,
`HudPlacementMath`, `IHudLayoutStorage`, `Hollowmere.Hud.Unity` for local
persistence. Host, edit mode, and widget presenters are **not** implemented.

Full detail: `docs/architecture/HOLLOWMERE_ENGINE.md`. HUD widget system:
`docs/architecture/HUD_WIDGET_ARCHITECTURE.md`. Layer-ownership table:
`docs/architecture/ENGINE_STRUCTURE.md`.

## Current engine philosophy

Hollowmere is a maintained ClassicUO engine fork, not a thin wrapper (ADR-001).
An engine file starts as a symlink into `external/ClassicUO`; the moment it
needs a real change, it's converted to a tracked file, edited, and documented
in `docs/architecture/ENGINE_MODIFICATIONS.md` (ADR-006). 73 files are
currently tracked this way; ~15 are flagged "needs manual review" (classified
by size/role heuristics, never actually read).

## Known technical debt

- ~15 engine override files never individually reviewed (see
  `docs/architecture/ENGINE_MODIFICATIONS.md`'s summary table).
- No plan yet for reconciling future upstream ClassicUO releases against 73+
  diverged files.
- `.meta` tracking gap for ~325 still-symlinked (untouched) engine files —
  deliberately deferred, not resolved.
- `README.md` is still the original upstream MobileUO README — does not
  reflect Hollowmere's identity.

## Known architectural debt

- **No resolved plan for Gump/Unity touch ownership at drag-and-drop
  frequency.** `docs/architecture/HOLLOWMERE_UI_RENDERING_STRATEGY.md` names
  this as the sharpest stress case (Inventory redesign, Docked Layout) and
  doesn't fully resolve it — flagged there as needing a decision before either
  of those milestones starts.
- **Automated tests are limited to the HUD foundation.** `Hollowmere.Hud.Tests`
  (EditMode) covers registry, profiles, placement math, storage contract, and
  migration fixtures. No CI gate yet; gameplay/engine stacks remain manually
  validated.
- **The Primary Action pipeline's reusability is unproven.** ADR-003's
  four-stage separation is designed for reuse by a second consumer that
  doesn't exist yet.
- **`HudLayoutProfileSerializer` may mutate profiles in place** during
  migrate/serialize — documented TODO; revisit when `HudLayoutService` and
  `HudWidgetHost` exist.

## Current branch expectations

Active development branch: **`controls-update`**. Confirm ahead/behind with
`git status` before assuming any count is current.

## Current diagnostics system

`Hollowmere.Diagnostics.Diag` (ADR-005): a static, category-based logger.
Categories: `Input`, `UI`, `ItemHold`, `Actions`, `Networking` — all off by
default. No settings UI for toggling categories yet.

## Current rendering strategy

Game state renders as ClassicUO Gumps; app chrome as Unity UI. See
`docs/architecture/HOLLOWMERE_UI_RENDERING_STRATEGY.md`.

## Current development priorities

**Approved next:** Issue **#25B** — `HudLayoutService` (profile resolution,
default profile registration, variant fallback, migration orchestration). Host
must consume the service, not embed layout policy.

## Recently completed work

Issue **#25A** — HUD foundation assemblies and permanent EditMode tests; no
scene or gameplay integration.

## Next milestone

**Issue #25B — `HudLayoutService`.** Small infrastructure milestone: layout and
profile resolution only. Then **#26 — `HudWidgetHost`** (coordination and
rendering bridge). Primary Action migration remains a separate milestone after
the host exists. See `docs/architecture/HUD_WIDGET_ARCHITECTURE.md` proposed
milestones (numbers may differ from this sequencing — this file wins for current
intent).
