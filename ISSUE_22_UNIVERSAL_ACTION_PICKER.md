# Issue #22 — Universal Action Picker

Scoping document only. Nothing here has been implemented. Written to be
reviewed and approved before any code is written, per the project's
plan-first workflow.

---

## Goal

Every #21A/#21B deliverable and everything shipped in #21-followup so far
(spellbook one-click-cast, ShopGump quantity entry, Skills touch sizing,
world map touch) has been *per-system* polish — one Gump at a time. That
converges slowly and asymmetrically: a mage's experience improves when we
fix the spellbook, but a tamer's doesn't, and neither does a crafter's or
a bard's.

The actual target (per the corrected Design Manifesto principle 9): mobile
UX good enough that any character/skill build feels like the full game is
available on a phone or tablet, not "the systems we happened to fix."

The lever that generalizes across every build is already built and
underused: `ActionBar` + `GameActionDefinition` (#21B). Today it's
hardcoded to two fixed bars we chose (Target Last, quick-chat). This
milestone makes it player-driven: an "Add Action" picker that lets a
player put *any* spell, skill, macro, or pet command on their own bar,
so a tamer's bar ends up nothing like a mage's bar without either of us
hand-designing either one.

---

## What already exists (reuse, don't rebuild)

- **`ActionBar`/`GameActionDefinition`/`ActionButtonGump`** (#21B) — the
  container, the action abstraction, and the button rendering are already
  built and validated. This milestone is entirely about *populating* bars
  from player choice instead of a hardcoded bootstrap list.
- **Spells** — `SpellDefinition` per school (`SpellsMagery.GetSpell(idx)`,
  `SpellsNecromancy`, `SpellsChivalry`, `SpellsBushido`, `SpellsNinjitsu`,
  `SpellsSpellweaving`, `SpellsMysticism`, `SpellsMastery`), exactly what
  `SpellbookGump` already iterates to build its own icon grid. Casting via
  `GameActions.CastSpell(def.ID)` is the same call `SpellbookGump`'s one-
  click-cast fix uses today.
- **Skills** — `GameActions.UseSkill(index)`, the same call
  `SkillListEntry`'s use-button already makes. Skill list comes from
  `World.Player.Skills`/`SkillsLoader.Instance.Skills`.
- **Pet commands** — already implemented as plain speech, in
  `Assets/Scripts/Assistant/Hotkeys.cs` (`PetAllCome/Follow/Guard/Kill/Stay`,
  each just `UOSObjects.Player.Say("All Follow")` etc.) — the exact same
  `GameActions.Say(...)` path `QuickChatBarBootstrap` already uses.
- **A large existing action catalog** — `Hotkeys._HotKeyActions` in the
  same file is a `Dictionary<string, Func<string, bool>>` already covering
  pet commands, skill-use-by-name (`skills.{name}`), and misc actions
  (`actions.dropcurrent`, etc.). It's currently `private`; needs a small,
  additive public accessor (e.g. `internal static IEnumerable<string>
  RegisteredActionNames`) to be enumerable by a picker. This single
  catalog may cover most of "skills" and "pet commands" as one source
  rather than two.
- **User macros** — needs a short research pass before scoping precisely.
  There appear to be two candidate systems: ClassicUO's native
  `MacroManager` (`external/ClassicUO/src/Game/Managers/MacroManager.cs`,
  keystroke-recorded macros) and the Assistant subsystem's own script
  engine (`ScriptManager`/`Interpreter`/`Lexer`, referenced from
  `Hotkeys.cs`). Confirm which one is the actual "player-authored macro"
  source before Deliverable 3 below.

---

## Design

### New pieces

- **`ActionCatalog`** (or similar) — a read-only aggregator that exposes
  available actions grouped by source (Spells, Skills/Hotkey-actions, Pet
  Commands, Macros), each yielding `GameActionDefinition`s on demand. Pure
  adapter over the existing systems above; no new gameplay logic.
- **Action picker UI** — a Hollowmere-styled panel (reusing
  `HollowmereButton`/`HollowmerePanel`/`HollowmereTheme` from #20A) listing
  `ActionCatalog` entries by category, with search (UO has a lot of
  spells/skills - browsing 8 spell schools one at a time won't be usable).
  Tapping an entry adds it to the currently-open (or a chosen) `ActionBar`
  via the existing `AddButton`.
- **Bar management** — a way to open the picker from an `ActionBar` itself
  (e.g. a persistent "+" button at the end of the bar), and a way to
  remove a button already on a bar (`ActionBar.RemoveButton` already
  exists - just needs a UI trigger, e.g. long-press an existing button).
- **Persistence of *contents*, not just position** — `ActionBar` currently
  only persists position (`IActionBarPersistence`, #21B). Player-chosen
  contents need to persist too (which actions are on which bar, in what
  order) so the bar survives relogin. This is new: a `GameActionDefinition`
  isn't serializable as-is (per #21B's own reasoning for not using
  ClassicUO's Gump XML save), so persistence here means saving a stable
  *reference* to each action (source + id/name), not the action object
  itself, and re-resolving it back to a `GameActionDefinition` through
  `ActionCatalog` on load.

### No changes needed to

- `AnchorManager`, `UIManager`, or any other Engine-layer/protected file -
  everything above is additive composition over `ActionBar`'s existing
  public surface plus read-only access to existing spell/skill/macro
  lists.

---

## Suggested deliverable breakdown

(One deliverable at a time, stop and validate after each, matching #21A/
#21B discipline.)

1. **`ActionCatalog` for Spells + Hotkey-actions** — the read-only
   aggregator, covering the two source types with existing, already-
   understood APIs (`SpellDefinition` lists, `Hotkeys._HotKeyActions` via
   a new public accessor). No UI yet - validate via a throwaway test
   (construct the catalog, confirm entry counts/labels are sane) the same
   way #21B's `ActionBar` was validated before any UI existed.
2. **Picker UI** — the Hollowmere-styled panel with category browsing and
   search, wired to Deliverable 1's catalog, adding to a real `ActionBar`
   on tap.
3. **Macro source** — after the ClassicUO-native-vs-Assistant-script
   research question above is resolved, add macros as a third catalog
   source.
4. **Bar content persistence** — save/restore which actions are on which
   bar across sessions.
5. **Bar management affordances** — "+" button to open the picker from a
   bar directly, remove-button gesture.

---

## Risks

- **Serialization design (Deliverable 4)** is the trickiest part - needs
  a stable, source-agnostic reference format (e.g. `"spell:magery:14"`,
  `"hotkey:actions.creatures.follow"`) decided before implementing, not
  discovered ad hoc.
- **Search/browse UX** for a large catalog (UO has dozens of spells across
  8 schools, dozens of skills) needs real design thought, not just a flat
  list - this is the one piece of this milestone that's genuinely new UI,
  not composition of existing pieces.
- **Macro source is unconfirmed** until the research question above is
  answered - Deliverable 3 shouldn't start until that's resolved.

## Out of scope for this milestone

- Radial/gesture-based action selection (a separate, later idea from an
  earlier conversation - this milestone is about *what* can go on a bar,
  not a new bar-interaction paradigm).
- Item/consumable quick-slots (potions, bandages) - a plausible future
  fourth source, but items are per-instance (serials), not per-definition
  like spells/skills, and need their own design pass for "which item"
  semantics (by type? last-used? explicitly assigned?).
- Multiple simultaneous custom bars beyond what already exists - this
  milestone assumes the two existing bootstrap bars become player-editable
  first; a full "create additional custom bars" UI is a natural follow-up,
  not required to hit the stated goal.

---

**Not started. Awaiting approval / direction before implementation
begins**, per the milestone's own instructions.
