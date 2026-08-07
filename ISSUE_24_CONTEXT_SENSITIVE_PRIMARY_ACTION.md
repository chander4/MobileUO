# Issue #24 — Context-Sensitive Primary Action (Vision Doc Phase 4)

Scoping document only. Nothing here has been implemented. Written to be
reviewed and approved before any code is written, per the project's
plan-first workflow.

---

## Goal

One prominent, always-present button whose behavior changes based on what
the player has currently selected: **Attack** a hostile mobile, **Talk**
to an NPC, **Loot** a corpse, **Pick Up** a ground item - instead of the
player hunting through per-object double-click/context-menu behavior to
find the right verb, and instead of them needing to already have the
right button on a customized bar (#22) for whatever they're standing
next to right now.

Per the vision doc's own framing, this is an **interaction shortcut, not
a new capability** - every verb below is a call to a `GameActions` method
that a manual double-click or a #22 picker button already exposes today.
This button never does anything a player couldn't already do; it just
removes the "which button do I need" step.

The classification logic (SelectedObject → Attack/Talk/Loot/Pick-Up) is
kept UI-independent on purpose (see Design's `PrimaryAction` model) so
the same "what's the smart verb for this object" answer could later
drive more than a HUD button - action bars, a radial menu, keyboard
shortcuts, controller prompts, or accessibility tooling - without any of
those depending on `ActionButtonGump`. None of those consumers are
in scope here; the model is just shaped so adding one later doesn't
require touching the resolver.

---

## What already exists (reuse, don't rebuild) - confirmed by direct source reading

- **`SelectedObject.LastObject`** (`external/ClassicUO/src/Game/SelectedObject.cs`) -
  the engine already tracks "whatever was last selected," updated on
  tap/click (`GameScene.cs:875`) and *specifically* set to a corpse when
  its loot window opens (`GridLootGump.cs:308`, confirmed while building
  #24's sibling feature, Loot Corpse, in `ActionCatalog.GetUtilityActions`
  this session). No new selection-tracking needed - this is the same
  source Loot Corpse already reads.
- **`GameActions.Attack(uint serial)`** (`GameActions.cs:94`) - sends
  `PAttackRequest` directly; does not require the player to already be
  in war mode first. Handles the "this may flag you criminal" confirm
  dialog itself when attacking an Innocent, if that profile setting is
  on - the button doesn't need to reimplement that.
- **`GameActions.DoubleClick(uint serial)`** (`GameActions.cs:124`) - already
  contains its *own* context dispatch: attacks if the target is a mobile
  and the player is in war mode, otherwise sends a generic double-click
  request that the **server** resolves into "open vendor gump," "open
  quest dialog," "open container," etc. depending on object type. This is
  the natural "Talk" implementation - the client doesn't need to know in
  advance whether an NPC is a vendor or a quest giver.
- **`GameActions.GrabItem(uint serial, ushort amount, uint bag = 0)`**
  (`GameActions.cs:467`) - already the "Pick Up" and "Loot" primitive
  (Loot Corpse, shipped this session, is `GrabItem` looped over a
  corpse's `Items` linked list). Same call, single target, for "Pick Up."
- **`NotorietyFlag`** (`external/ClassicUO/src/Game/Data/NotorietyFlag.cs`) -
  `Innocent/Ally/Gray/Criminal/Enemy/Murderer/Invulnerable`. Used only for
  the button's *label/icon* classification (does this look like something
  to fight or something to talk to) - the actual `Attack` call already
  handles the real legal/criminal-flagging consequences server-side
  regardless of what the button predicted.
- **`Item.IsCorpse`** (`Item.cs:190`) - already used by Loot Corpse.
- **`ActionButtonGump`/`GameActionDefinition`** (#21B/#22) - the vision
  doc's own suggestion is "reuse `ActionButtonGump`'s rendering with a
  dynamically-swapped `GameActionDefinition`," not a new button type.
  Confirmed feasible: `ActionButtonGump` already stores `definition` as a
  field it reads from in `Draw`/`OnMouseUp`/tooltip, not baked into
  immutable Gump state - swapping it in place needs one new method, not
  a rewrite.

---

## Design

### Pipeline

```
SelectedObject.LastObject
    ↓
PrimaryActionResolver.Resolve(...)      -- pure classification, no UI knowledge
    ↓
PrimaryAction (Type + Target)           -- small immutable domain value
    ↓
PrimaryActionDefinitionFactory.Create(...)  -- the only place that knows about labels/icons/GameActionDefinition
    ↓
ActionButtonGump.SetDefinition(...)
```

Revised from an earlier draft that had the resolver build a
`GameActionDefinition` directly. Splitting classification from
presentation was requested explicitly and holds up on inspection: a
`GameActionDefinition` is already a presentation/execution-shaped type
(delegate, `CanExecute`, `Tooltip`, `IconGraphic`) built for rendering as
a Gump button - baking that into the resolver would mean "what's the
right verb here" and "what icon/label does that verb get" live in the
same function, and the only way to unit-test the classification would be
to invoke delegates or reflect into a presentation object. It's also not
a new pattern for this codebase: `ActionCatalog` (data) /
`ActionButtonGump` (rendering) / `ActionBar` (layout) already keep those
concerns separate in #21B/#22 - this is the same separation one step
earlier, not a new architectural idea. The cost is one enum, one small
struct, and one factory class with a switch - no interfaces, no DI,
nothing speculative beyond what the 4-5 real cases need.

### `PrimaryAction` - the domain model between resolver and presentation

```csharp
public enum PrimaryActionType
{
    None,
    Attack,
    Talk,
    Loot,
    PickUp
}

// Plain immutable value, not a Gump/GameActionDefinition concept - this is
// what makes the model reusable by something other than a HUD button later
// (radial menu, keyboard shortcut, controller prompt, accessibility reader)
// without any of them depending on ActionButtonGump.
public readonly struct PrimaryAction
{
    public PrimaryActionType Type { get; }
    public BaseGameObject Target { get; }

    public PrimaryAction(PrimaryActionType type, BaseGameObject target)
    {
        Type = type;
        Target = target;
    }

    public static readonly PrimaryAction None = new PrimaryAction(PrimaryActionType.None, null);
}
```

### Verb resolver (pure C#, testable - same discipline as `LongPressStateMachine` in #21A)

Knows only about `BaseGameObject`/`Item`/`Mobile`/`NotorietyFlag` -
nothing about `GameActionDefinition`, icons, labels, or `ActionButtonGump`.
Stateless, so it's unit-testable the same way #21A's state machine was,
and trivially so: asserting `result.Type == PrimaryActionType.Attack` on
a plain struct, no delegate invocation or reflection needed.

```csharp
internal static class PrimaryActionResolver
{
    public static PrimaryAction Resolve(BaseGameObject selected)
    {
        if (selected is Item corpse && corpse.IsCorpse && !corpse.IsDestroyed)
        {
            return new PrimaryAction(PrimaryActionType.Loot, corpse);
        }

        if (selected is Item groundItem && /* on the ground, not equipped/in a container the player doesn't own */)
        {
            return new PrimaryAction(PrimaryActionType.PickUp, groundItem);
        }

        if (selected is Mobile mobile && mobile != World.Player)
        {
            return IsHostile(mobile.NotorietyFlag)
                ? new PrimaryAction(PrimaryActionType.Attack, mobile)
                : new PrimaryAction(PrimaryActionType.Talk, mobile);
        }

        return PrimaryAction.None;
    }

    private static bool IsHostile(NotorietyFlag flag) =>
        flag == NotorietyFlag.Gray || flag == NotorietyFlag.Criminal ||
        flag == NotorietyFlag.Enemy || flag == NotorietyFlag.Murderer;
}
```

### `PrimaryActionDefinitionFactory` - the only place that knows about presentation

Converts a `PrimaryAction` into a `GameActionDefinition` - owns the
labels ("Attack," "Talk," "Loot," "Pick Up"), icon graphics (if any),
tooltip text (built from `Target`'s name, e.g. "Attack a Skeleton"), and
which `GameActions` call each verb actually makes. This is where
`GameActions.Attack`/`DoubleClick`/`GrabItem` get called from - the
resolver never references `GameActions` at all.

```csharp
internal static class PrimaryActionDefinitionFactory
{
    public static GameActionDefinition Create(PrimaryAction action)
    {
        switch (action.Type)
        {
            case PrimaryActionType.Attack:
                return BuildAttack((Mobile) action.Target);
            case PrimaryActionType.Talk:
                return BuildTalk((Mobile) action.Target);
            case PrimaryActionType.Loot:
                return BuildLoot((Item) action.Target);
            case PrimaryActionType.PickUp:
                return BuildPickUp((Item) action.Target);
            default:
                return NeutralDefinition(); // disabled placeholder, not null - see ActionButtonGump.SetDefinition below
        }
    }
}
```

### `ActionButtonGump.SetDefinition(GameActionDefinition)` - the one new piece in existing Hollowmere code

Everything above is new files; this is the only change to something that
already exists. Today a button's `GameActionDefinition` is set once in
the constructor and never changes - every #21B/#22 button (Target Last,
spells, skills, phrases) is "pick a verb once, keep it forever." This
feature needs "keep the same button, screen position, and anchor-group
membership, but let its verb change" - swapping the label/icon/action/
tooltip in place, without disposing and recreating the Gump (which would
churn the `AnchorGroup` for no reason and could visually flicker).

```csharp
// ActionButtonGump - new method, existing fields become mutable instead
// of readonly where the swap needs to touch them.
public void SetDefinition(GameActionDefinition newDefinition)
{
    // rebuild label/icon child controls only if icon-vs-text presentation
    // changed (e.g. verb changed from an icon-having spell-adjacent action
    // to a plain-text one) - otherwise just update text/graphic in place.
}
```

### Controller: watches selection, swaps the definition on *change* only

Not a per-frame poll-and-recreate - re-resolves only when
`SelectedObject.LastObject` actually changes (cheap reference comparison),
running it through the resolver → factory pipeline and calling
`SetDefinition` just then. Lives outside `ActionBar` entirely - see
placement below.

### Placement: its own persistent HUD element, not inside an `ActionBar`

This button is **not** one more entry a player adds/removes via the #22
picker, and it doesn't belong to the Main or QuickChat bar. The vision
doc frames it as "one prominent, dock-anchored button" - a fixed, always-
present HUD element (conceptually the Phase 2 "Persistent HUD" category,
even though it's scoped as its own phase). Reasoning:

- Its whole value is being in the *same* predictable spot regardless of
  what's selected - if it lived inside a player-customizable, wrappable,
  draggable bar (#22), its position/row could shift as the player adds
  other actions, undermining "always reachable, same place."
- It's structurally a single fixed button, not a list - `ActionBar`'s
  row-wrapping/reordering/persistence machinery doesn't apply to it and
  would be dead weight if reused.
- **Screen position is a real open question, not decided here.** Both
  thumb zones are already claimed: left = joystick, right = the Main +
  QuickChat bars (#22, right-docked this session specifically to clear
  the joystick's zone). A third always-present element needs its own
  placement decision - candidates: stacked above the two existing right-
  docked bars (extends the already-established right-side vertical
  stack), or a larger/more prominent button in a currently-empty zone
  (e.g. bottom-center, closer to thumb reach on a two-handed tablet grip
  per the user's earlier ergonomics note). Needs a decision before
  Deliverable 3 below, not guessed at implementation time.

---

## Suggested deliverable breakdown

(One deliverable at a time, stop and validate after each, matching #21A/
#21B/#22 discipline.)

1. **`PrimaryActionType` + `PrimaryAction` + `PrimaryActionResolver`** -
   the pure, UI-independent classification layer. No UI yet - validate
   with a throwaway runtime test (same pattern as
   `ActionCatalogValidationTest.cs`, already deleted after use this
   session): feed it known selection states, assert the right
   `PrimaryActionType` comes back. This is also cheap to give a real
   unit test (not just a throwaway runtime one) since it's now a pure
   function over a plain struct - worth doing here rather than
   deferring, unlike the Gump-dependent deliverables below.
2. **`PrimaryActionDefinitionFactory`** - converts `PrimaryAction` into a
   `GameActionDefinition` (labels, tooltip text, icon if any, and the
   actual `GameActions.Attack`/`DoubleClick`/`GrabItem` call per verb).
   Validated by constructing definitions for one `PrimaryAction` of each
   type and checking label/tooltip/action are sane - same throwaway-test
   discipline, still no Gump/scene involvement yet.
3. **`ActionButtonGump.SetDefinition`** - the one architecture extension
   to existing code. Validate in isolation: construct a button, call
   `SetDefinition` twice with different definitions, confirm the Gump
   updates without a new `AnchorGroup` entry (no anchor-drift, no
   duplicate Gump in `UIManager.Gumps`).
4. **Controller + bootstrap** - wires `SelectedObject.LastObject` through
   the resolver → factory pipeline and into the button via
   `SetDefinition` on selection-change, decides final screen placement
   (per the open question above - needs a direction from the user before
   this deliverable starts, not an assumption).
5. **Polish** - tooltip shows full verb + target name ("Attack a
   Skeleton," "Talk to Vendor," "Loot corpse"), neutral/disabled visual
   state when nothing's selected.

---

## Risks

- **Notoriety-based Attack/Talk labeling has real UO edge cases** a
  simple flag check won't perfectly cover (a Criminal player's own
  `NotorietyFlag` context, guard-zone restrictions, tamed/summoned
  creatures, invulnerable NPCs). The button's *label* is a prediction,
  not a guarantee - actual consequences are still decided server-side by
  the same `GameActions.Attack`/`DoubleClick` calls a manual click would
  trigger, so a mislabel is a UX rough edge (wrong-looking button), not a
  new way to accidentally flag criminal or attack the wrong thing. Should
  still default conservatively (prefer `DoubleClick`'s own smart dispatch
  over a guessed `Attack` call) when classification is ambiguous.
- **`SetDefinition` is genuinely new architecture** - first departure
  from "every button's verb is fixed at construction." Needs its own
  careful validation (Deliverable 3) before anything depends on it,
  same discipline #21B's `AnchorManager` reuse got before #22 built on
  top of it.
- **Screen placement is undecided** (see Design) - this blocks
  Deliverable 4, not earlier ones.
- **"Pick Up" grabs the full item/stack**, same simplification already
  accepted for Loot Corpse (no partial-amount picker) - consistent, not
  a new corner cut.

## Out of scope for this milestone

- Verb coverage beyond Attack/Talk/Loot/Pick-Up (mount, bandage a target,
  open a door, use a vendor's buy/sell directly) - a natural follow-up
  once the resolver/`SetDefinition` pattern exists, not required to hit
  the stated goal.
- Radial/gesture-based selection - out of scope per #22's own scoping
  doc for the same reason (separate interaction-paradigm idea, not what
  this milestone is about).
- Changing what `Attack`/`DoubleClick`/`GrabItem` actually do server-side -
  this feature is a routing shortcut over existing capability only.

---

**Not started. Awaiting approval / direction before implementation
begins** - in particular, a decision on screen placement (Design section)
before Deliverable 3.
