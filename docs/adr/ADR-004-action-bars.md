# ADR-004: Action Bars

## Context

Desktop UO players rely heavily on hotkeys and macros — muscle memory built over
years, invisible to a new mobile player and unavailable on a touchscreen with no
keyboard. Hollowmere needed a touch-native equivalent: a persistent, customizable
row of tappable actions (spells, skills, pet commands, macros) that doesn't require
memorizing a keybinding.

## Decision

`ActionBar` is a layout container that positions `ActionButtonGump` instances using
ClassicUO's *existing* `AnchorManager`/`AnchorGroup` grid-snap system — the same
mechanism a player already uses to manually drag-and-anchor native Gumps together.
`ActionBar` doesn't invent a parallel layout system; it calls the same
`AnchorGroup.AnchorControlAt` primitive a manual drag would, so bar buttons and
manually-anchored Assistant buttons interoperate through shared state instead of two
competing systems fighting for the same screen region.

Position persistence goes through an injected `IActionBarPersistence` interface
(currently backed by `PlayerPrefsActionBarPersistence`) — `ActionBar` doesn't know or
care *how* its position is saved, only that it can ask.

Bar contents come from `ActionCatalog`, a read-only aggregator over ClassicUO's own
native action sources (`GameActions.CastSpell`, `UseSkill`, `Say`) — not from
Assistant's legacy `HotKeys` catalog, which required the large, mostly-unrelated
Assistant subsystem to be enabled just to populate a Skills tab.

## Alternatives considered

- **Hand-rolled pixel-based layout instead of reusing `AnchorManager`.** Rejected —
  would create a second, parallel positioning system that a player's manual Gump
  drags don't participate in, producing visually inconsistent snapping behavior.
- **Populate the catalog from Assistant's `HotKeys`.** Rejected — this was tried and
  reverted; it silently left Skills/Pets tabs empty unless the player had the whole
  Assistant subsystem (auto-loot, scavenger, vendor scripting) turned on, which is
  an unrelated, heavyweight dependency for "list the skills I have."
- **Persist bar layout via ClassicUO's own Gump XML save/restore system** (the
  mechanism `Save(BinaryWriter)`/`Restore` uses elsewhere). Rejected — a
  `GameActionDefinition` carries a C# delegate that can't be meaningfully
  reconstructed from saved XML; `CatalogRefId`-based re-resolution against
  `ActionCatalog` on next login was simpler and doesn't require inventing a
  delegate-serialization scheme.

## Consequences

- Action Bar buttons look and behave consistently with every other anchored Gump on
  screen, because they *are* anchored Gumps, not a separate rendering path.
- Swapping the persistence backend (cloud sync, a Hollowmere account profile) later
  is a new `IActionBarPersistence` implementation, not an `ActionBar` change.
- The catalog only surfaces what's genuinely native and always-available — no
  feature gating on an optional subsystem being enabled.

## Future implications

- `ActionCatalog`'s linear scan over ~400 entries at login is cheap today; if the
  catalog grows substantially (custom macros, per-character loadouts), revisit
  whether a dictionary lookup is warranted — deliberately deferred as premature
  optimization for now.
- The bar's row-wrapping and drag-to-reorder behavior should be the reference
  implementation other future "list of tappable things" UI (a spell book grid, a
  quick-loot list) checks itself against, rather than each inventing its own
  wrapping/reorder logic.
