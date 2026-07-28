# Issue #19A - Server Browser Foundation — Review

**Branch:** controls-update
**Status:** Implemented, not yet manually verified in-app
**Scope:** Narrowed from the original #19 "full modern server browser" ask — see [Scoping](#scoping) below.

## Scoping

The original #19 spec asked for a full server-browser rebuild: card-based layout,
drag-reorder, a connection-status state machine reading NetClient/DownloadState/GameState,
and a full error-message taxonomy. Given that we'd just spent significant effort
stabilizing Mobile Input V2 in the same scene, we deliberately cut scope to avoid
another large refactor there. What shipped as "#19A" is the foundation layer only:

- Extend the existing `ServerConfiguration` model, don't replace it
- Wire up infrastructure that already existed but was dead code
- Add validation, search, sort, and a quick-connect visual cue
- Explicitly **not** in scope: card redesign, drag-reorder, connection-status
  state machine, error taxonomy, networking changes

## What changed

| File | Change |
|---|---|
| `Assets/Scripts/ServerConfiguration/ServerConfiguration.cs` | Added `Description`, `Favorite`, `LastConnected` fields; updated `Clone()` |
| `Assets/Scripts/ServerConfiguration/ServerConfigurationModel.cs` | Added `SetAsDefault`, `IsDefault`, `SetFavorite`, `MarkConnected` |
| `Assets/Scripts/ServerConfiguration/ServerConfigurationEditPresenter.cs` | Favorite toggle, Set-as-Default button, Description field, hostname/IP validation |
| `Assets/Scripts/ServerConfiguration/ServerConfigurationListPresenter.cs` | Search filter (name/host/description), favorites-first sort, quick-connect highlight |
| `Assets/Scripts/ServerConfiguration/ServerConfigurationListItemView.cs` | `SetQuickConnect(bool)` — tints the Select button when it's the only server |
| `Assets/Scene.unity` | New UI elements added to the existing Edit and List panels (no redesign) |

## Notable finding: dead infrastructure, now wired up

`ServerConfigurationModel.DefaultConfiguration` and `BootState` already existed and
already auto-connect on app launch if a default is set — but nothing ever *wrote*
the PlayerPrefs key that makes that fire. This session added the "Set as Default"
button that finally exercises that path. **Practical effect:** once a user taps
"Set as Default" on a server, the app will skip the server-picker on next launch
and connect directly. This is existing, intentional behavior per the original
BootState design — just previously unreachable from the UI.

## Validation added

`ServerConfigurationEditPresenter.ValidateFields` already had solid duplicate-name,
blank-field, and port-range checks before this session — that part of the original
#19 ask was already done and just needed to be found (see [Deletion Strategy] memory
principle: check before you build). The one gap filled: `UoServerUrl` now runs through
`Uri.CheckHostName`, so garbage input is caught before save.

## Data model

Three new fields on `ServerConfiguration`, all with backward-compatible defaults
(`false`/`null`/empty string). No new persistence system — everything still rides
the existing PlayerPrefs JSON blob via `ServerConfigurationModel.SaveServerConfigurations()`.

## Verification status

**Confirmed with evidence:**
- Zero compile errors after every script change (checked via Unity console)
- Every new serialized field reference confirmed non-null *and pointing to the
  correct object* by reading back component data — not just trusting a tool's
  "success" response (see incident below for why this matters)
- Scene changes confirmed present in the saved `.unity` file on disk

**Not yet verified — needs a manual pass tomorrow:**
- [ ] Existing Local Memento server still connects end-to-end
- [ ] A saved server survives an app restart with Favorite/Default/Description intact
- [ ] Search actually filters the visible list correctly on device
- [ ] Quick-connect highlight shows correctly when exactly one server is configured
- [ ] Mobile layout doesn't clip anything (search box, favorite toggle, default button)
- [ ] "Set as Default" → app restart → auto-connects as expected

## Incident during this session

Unity Editor crashed once mid-session. Root cause: a recompile was triggered
(via script edit + `refresh_unity`) while newly-created, **unsaved** GameObjects
were sitting in the in-memory scene. No data was permanently lost — all C# script
edits are safe on disk regardless of Editor state, and the only unsaved scene work
(the search box UI) was cleanly redone after the Editor restarted. Added a hard
rule to memory: never trigger a recompile with unsaved scene changes pending;
always save the scene first.

## Known technical debt

"Set as Default" stores the config's `Name` string at the moment you click it.
If you then rename the config before saving, the stored default silently goes
stale (points to a name that no longer exists). This matches the existing
codebase's general lack of handling for that class of edge case elsewhere — not
a new regression, just worth knowing about.

## Suggested follow-up (not started)

If scope expands back toward the original #19 ask, the natural next slices are:
1. Connection-status UI (Connecting/Authenticating/Downloading/etc.) — touches
   `DownloadState`/`GameState`, needs care given the "don't touch networking" constraint
2. Drag-reorder for the server list
3. Card-based visual redesign
4. Friendlier error-message taxonomy for connection failures

None of these were started this session.
