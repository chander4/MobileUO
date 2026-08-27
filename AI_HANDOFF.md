# Hollowmere AI Handoff

## Current milestone

**Issue #29 (Vitals) and Issue #28A (PlayMode smoke infrastructure) are implemented and committed** (`44b8d87`, `e4397f6`). Commit status is not the same as validation status — see Tests below.

- **#29:** Vitals HUD widget (`hollowmere.vitals`) via `VitalsPresenter` + `PlayerVitalsGump` (wraps `HealthBarGumpCustom` for `World.Player`).
- **#28A:** PlayMode smoke-test harness (`HudSmokeTestBridge` + `Hollowmere.HudRuntime.Tests`) — implemented, committed, and correctly discovered as PlayMode tests. Not fully validated.

## HUD runtime wiring

- **Composition:** `HudWidgetHostComposition` registers widgets and presenter factories.
- **Gump presenters:** `PrimaryActionPresenter`, `ActionBarPresenter`, `VitalsPresenter` (`HudRuntime/Gumps/`).
- **Legacy bootstraps disabled:** `PrimaryActionButtonBootstrap`, `ActionBarBootstrap` (`InstallDisabled`).
- **OptionsGump** still uses `ActionBarBootstrap.Current` / `SetLayout` — set by `ActionBarPresenter` on materialize.

## Persistence

- Action Bar **contents/layout/position:** `PlayerPrefsActionBarPersistence` + `ActionBarBootstrap.PersistenceKey` (unchanged).
- HUD **layout profile:** `PlayerPrefsHudLayoutStorage` — default placement; merge from built-in template for missing widgets.

## Tests

- **EditMode:** `Hollowmere.Hud.Tests` — required for HUD logic changes. Latest confirmed run: **39/39 passed, 0 failed, 0 skipped**.
- **PlayMode (optional):** `Hollowmere.HudRuntime.Tests` category `HudRuntimeSmoke` — manual Test Runner only; not CI/blocking. Uses `HudSmokeTestBridge`. Per-test status (latest confirmed):
  - `HudHost_Initializes` — **passed**.
  - `Hud_Presenters_Created` — **passed** in a favorable in-game run.
  - `Hud_NoDuplicateWidgets` — **not established as passing**.
  - `Hud_NoDuplicates_AfterReload` — **not established as passing**.
  - **#28A does not have a verified 4/4 PlayMode pass.** Tests Ignore (not fail) when the session isn't logged in past `BootState`/`GameScene`; the two unestablished tests above remain unresolved beyond that gating — see Known issues.

## Known issues

- **Fixed:** `ee89109 fix(core): reset state manager on scene reload` — #28A's `Hud_NoDuplicates_AfterReload` test reproduced a stale-`StateManager`/`MissingReferenceException` on a second `Init.Awake()` (scene reload). Root cause: `currentState`/`states` were never cleared across reload. Fixed via `StateManager.Reset()`, called first in `Init.Awake()`.
- **Open, unresolved:** after the StateManager fix, the reload path progresses further and now exposes a separate `ClientRunner`/`UnityMainThreadDispatcher` lifecycle exception. Root cause **not yet confirmed**. This is a pre-existing production issue exposed by #28A's reload test, not a defect in #28A itself. `ClientRunner` is a Protected system (see `CLAUDE.md`) — requires explicit approval before investigation or modification.

## Next work

Immediate priority: triage the remaining PlayMode validation gap and decide whether the `ClientRunner`/`UnityMainThreadDispatcher` issue needs investigation before treating the HUD checkpoint as fully validated (Protected-system change — needs explicit approval). Migrating additional widgets or shipping edit mode is secondary until that decision is made. Do not redesign `HudWidgetHost` / `HudLayoutService` without a proven flaw.
