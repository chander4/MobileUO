# Hollowmere AI Handoff

## Current milestone

**Mobile targeting / overlay panels checkpoint** — committed `3a978dd`..`27cb584` (see `git log`). Commit status is not validation status.

- **Targeting:** `WorldTapTargeting` (+ `WorldTapStateMachine`) pins a tapped Mobile/Item; `CurrentSelection` is the single canonical selection. Target Vitals (`hollowmere.target-vitals`), the target marker/name (`WorldEntityScreenAnchor`) and Primary Action all follow it. `NearestThreatFinder`, `SpellTargetingIndicator` added.
- **Context menu v1:** long-press on a pinned Item/Mobile opens `ContextActionMenuPresenter` (one row: "Examine" = `GameActions.SingleClick`). Closes on outside tap, Examine, or pin change. Not a `HudWidgetHost` widget. The dormant `ContextMenuLongPressTrigger` / native `PopupMenuGump` path is intentionally untouched.
- **HUD widgets:** Status effects (`hollowmere.status-effects`, `BuffBarPresenter`), Target Vitals, Stats; responsive built-in profiles + `HudDistanceFormatter`.
- **Unity UI overlay panels:** Backpack, Paperdoll, Spell/Skill book (`*Presenter`, `*TriggerBootstrap`, `BackpackItem.prefab`).
- **Touch isolation:** LeanTouch `GuiLayers` must include layer 0 (Default) or `PointOverGui` stops protecting these panels; guarded by PlayMode test `LeanTouch_GuiLayers_IncludesDefaultLayer`.
- **Joystick fix (`c0c6097`):** `GameScene` joystick now always walks; only follow-cancel is gated by the `JoystickCancelsFollow` pref (previously the pref gated walking itself).
- **Earlier:** #29 Vitals, #28A PlayMode smoke harness (`44b8d87`, `e4397f6`).
- ADR-009 (mobile UX principles) and ADR-010 (renderer-agnostic presentation boundary) added.

## HUD runtime wiring

- **Composition:** `HudWidgetHostComposition` registers widgets and presenter factories.
- **Gump presenters:** `PrimaryActionPresenter`, `ActionBarPresenter`, `VitalsPresenter`, `StatsPresenter`, `BuffBarPresenter`, `TargetVitalsPresenter` (`HudRuntime/Gumps/`).
- **Legacy bootstraps disabled:** `PrimaryActionButtonBootstrap`, `ActionBarBootstrap` (`InstallDisabled`).
- **OptionsGump** still uses `ActionBarBootstrap.Current` / `SetLayout` — set by `ActionBarPresenter` on materialize.

## Persistence

- Action Bar **contents/layout/position:** `PlayerPrefsActionBarPersistence` + `ActionBarBootstrap.PersistenceKey` (unchanged).
- HUD **layout profile:** `PlayerPrefsHudLayoutStorage` — default placement; merge from built-in template for missing widgets.

## Tests

- **EditMode:** `Hollowmere.Hud.Tests` — required for HUD logic changes. Last confirmed run: **48/48 passed** — as reported by the previous CLI session (2026-10-04). **Not re-run since the commits above; UnityMCP was down when they were made.**
- **PlayMode (optional):** `Hollowmere.HudRuntime.Tests` category `HudRuntimeSmoke` — manual Test Runner only; not CI/blocking. Uses `HudSmokeTestBridge`. Per-test status (latest confirmed):
  - `HudHost_Initializes` — **passed**.
  - `Hud_Presenters_Created` — **passed** in a favorable in-game run.
  - `Hud_NoDuplicateWidgets` — **not established as passing**.
  - `Hud_NoDuplicates_AfterReload` — **not established as passing**.
  - New: `LeanTouch_GuiLayers_IncludesDefaultLayer` — 1/1 passed per the 2026-10-04 session (transient flake on first run after a live session, stable on two reruns).
  - **#28A does not have a verified 4/4 PlayMode pass.** Tests Ignore (not fail) when the session isn't logged in past `BootState`/`GameScene`; the two unestablished tests above remain unresolved beyond that gating — see Known issues.

## Known issues

- **Fixed:** `ee89109 fix(core): reset state manager on scene reload` — #28A's `Hud_NoDuplicates_AfterReload` test reproduced a stale-`StateManager`/`MissingReferenceException` on a second `Init.Awake()` (scene reload). Root cause: `currentState`/`states` were never cleared across reload. Fixed via `StateManager.Reset()`, called first in `Init.Awake()`.
- **Possibly fixed, unverified:** `3a978dd` makes `Init.Awake()` dispose the prior `Client.Game` (UnloadContent/Dispose/null) before `StateManager.Reset()`, and sets `UnityMainThreadDispatcher` execution order to -1000. This targets the stale-`Client.Game` read in `ClientRunner.Awake()` below, but `Hud_NoDuplicates_AfterReload` has not been re-run to confirm. Note it touches the Protected `ClientRunner` lifecycle indirectly — get approval/review before building on it.
- **Original report, now possibly addressed:** after the StateManager fix, the reload path progresses further and now exposes a separate `ClientRunner`/`UnityMainThreadDispatcher` lifecycle exception. Root cause **not yet confirmed**. This is a pre-existing production issue exposed by #28A's reload test, not a defect in #28A itself. `ClientRunner` is a Protected system (see `CLAUDE.md`) — requires explicit approval before investigation or modification.

## Next work

1. In Unity: compile, run EditMode + PlayMode (incl. `Hud_NoDuplicates_AfterReload`) against the committed state. The big `45793f9` commit bundles mutually-dependent files and was not compile-checked.
2. Corpse long-press was code-path verified only (none nearby in live test); context menu and overlay panels have no dedicated tests.
3. Clean up: scope-regenerate `codedebrief-out` (skill `codedebrief-scope`), gitignore `Assets/Screenshots/`, remove orphan `Assets/Tests/Interaction.meta`.
4. Then edit mode / further widget migration. Do not redesign `HudWidgetHost` / `HudLayoutService` without a proven flaw.
