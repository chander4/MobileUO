# Hollowmere Project State

**This is a living snapshot. Update it after every milestone — not "when
convenient," as part of the milestone's own documentation step (see
`docs/development/DEVELOPMENT_PROCESS.md`).** If this file is stale, trust it less than a fresh
`git log`, but trust it more than a training-data assumption or a prior
conversation's memory.

Last updated: Issue #28A — PlayMode HUD smoke infrastructure (`e4397f6`, committed).
Implementation/commit status is not the same as full validation — see
"Validation status" below before assuming any HUD milestone is fully proven.

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
  adapter). **23 EditMode tests** in `Hollowmere.Hud.Tests`. No runtime HUD wiring.
- **Issue #25B** — `HudLayoutService`, `HudWidgetHost`, composition bootstrap.
- **Issue #26** — `IHudWidgetPresenter`, presenter registry, Primary Action migration.
- **Issue #27** — Action Bar migration (`ActionBarPresenter`, `hollowmere.action-bar`).
- **Issue #28A** — PlayMode smoke-test harness (`HudRuntimeSmoke`): implemented
  and committed (`e4397f6`). `HudHost_Initializes` and `Hud_Presenters_Created`
  have passed; `Hud_NoDuplicateWidgets` and `Hud_NoDuplicates_AfterReload` are
  not established as passing. **Not a verified 4/4 PlayMode pass** — see
  "Validation status."
- **Issue #29** — Player vitals (`VitalsPresenter`, `hollowmere.vitals`, top-left default).
- **Fix (`ee89109`)** — StateManager scene-reload `MissingReferenceException`,
  reproduced by #28A's `Hud_NoDuplicates_AfterReload` test; fixed via
  `StateManager.Reset()` called first in `Init.Awake()`.

**HUD runtime:** `HudWidgetHostComposition` + `HudRuntime/Gumps/*` presenters.

## Validation status

- **EditMode:** `Hollowmere.Hud.Tests` — 39/39 passed, 0 failed, 0 skipped
  (latest confirmed run).
- **PlayMode:** `Hollowmere.HudRuntime.Tests` discovery confirmed (asmdef
  `includePlatforms: []`, correctly enumerated as PlayMode).
  - `HudHost_Initializes` — passed.
  - `Hud_Presenters_Created` — passed in a favorable in-game run.
  - `Hud_NoDuplicateWidgets` — not established as passing.
  - `Hud_NoDuplicates_AfterReload` — not established as passing.
  - Clean continuous 4/4 PlayMode validation remains outstanding.

## Current development priorities

Migrate additional widgets or edit mode — not HUD foundation redesign.

## Next milestone

Planning: joystick/Unity widget adapter (per architecture) or edit mode.

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
- **`ClientRunner`/`UnityMainThreadDispatcher` scene-reload lifecycle
  exception — open, unresolved.** Exposed by #28A's `Hud_NoDuplicates_AfterReload`
  test after `ee89109` fixed the earlier StateManager failure and let the
  reload path progress further. Root cause not yet confirmed. `ClientRunner`
  is a Protected system (see `CLAUDE.md`) — requires explicit approval before
  investigation or modification.

## Known architectural debt

- **No resolved plan for Gump/Unity touch ownership at drag-and-drop
  frequency.** `docs/architecture/HOLLOWMERE_UI_RENDERING_STRATEGY.md` names
  this as the sharpest stress case (Inventory redesign, Docked Layout) and
  doesn't fully resolve it — flagged there as needing a decision before either
  of those milestones starts.
- **Automated tests are limited to the HUD foundation.** `Hollowmere.Hud.Tests`
  (EditMode) covers registry, profiles, placement math, storage, migration,
  layout service, variant fallback, and host. No CI gate yet; gameplay/engine
  stacks remain manually validated.
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

## Recently completed work

Through commit `e4397f6`: Issue **#26** (`HudWidgetHost`, presenter registry,
Primary Action migration), **#27** (Action Bar migration), **#29** (Vitals
migration), **#28A** (PlayMode smoke-test harness — implemented/committed,
not fully validated; see "Validation status"), and fix **`ee89109`**
(StateManager scene-reload reset).

Full engine/layer detail: `docs/architecture/HOLLOWMERE_ENGINE.md`,
`docs/architecture/HUD_WIDGET_ARCHITECTURE.md`, `docs/architecture/ENGINE_STRUCTURE.md`.
