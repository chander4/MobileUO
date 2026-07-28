# Action Bar System

## Purpose

The Action Bar is Hollowmere's reusable gameplay action container. It arranges one or more gameplay actions as touch-friendly buttons and manages their on-screen layout and position persistence.

Future systems — Combat Bar, Spell Bar, Crafting Bar, Pet Commands, controller-driven layouts, and any other collection of player-triggerable actions — should reuse `ActionBar` rather than creating new button containers. A new use case is a new `ActionBar` instance populated with different `GameActionDefinition`s, not a new container type.

---

## Architecture

```text
GameActionDefinition
        │
        ▼
ActionButtonGump
        │
        ▼
ActionBar
        │
        ▼
AnchorManager
        │
        ▼
ClassicUO
```

- **`GameActionDefinition`** owns behavior — what an action does and whether it can currently execute.
- **`ActionButtonGump`** owns presentation — how a single action renders and responds to touch.
- **`ActionBar`** owns layout — how multiple buttons are arranged and grouped together.
- **`AnchorManager`** owns positioning — the grid-snap placement and drag mechanics, provided by ClassicUO.
- **Persistence** (`IActionBarPersistence`) owns saving/loading — where a bar's position is stored between sessions.

No layer assumes another layer's responsibility. `ActionBar` never inspects what an action does; `ActionButtonGump` never decides where it sits in a group; `GameActionDefinition` never renders anything.

---

## Responsibilities

### `ActionBar`
Plain C# class (no Unity lifecycle) that owns a collection of `ActionButtonGump`s. Adds buttons to a shared `AnchorManager` group in horizontal or vertical layout, exposes `Buttons`, `Count`, `Contains`, `AddButton`, `RemoveButton`, and `Clear`, and drives position persistence via `Tick()`.

### `ActionButtonGump`
Touch-friendly `AnchorableGump` that renders a single `GameActionDefinition` and executes it on tap. Knows nothing about what a specific action does or which bar it belongs to.

### `GameActionDefinition`
Presentation-agnostic record of a single gameplay action: label, the action itself, an optional availability check, tooltip, and icon. Any future consumer (a bar, a radial menu, a keybinding) can use the same definition.

### `IActionBarPersistence`
Interface for loading and saving an `ActionBar`'s on-screen position, keyed by the bar's persistence key. Decouples `ActionBar` from *how* position is stored.

### `PlayerPrefsActionBarPersistence`
The current `IActionBarPersistence` implementation, backed by Unity `PlayerPrefs`.

---

## Design Principles

- **`AnchorManager` is reused, not reimplemented.** `ActionBar` positions buttons using the same `AnchorGroup`/`AnchorControlAt` primitives a manual drag-to-anchor already uses, so bars built from it interoperate with existing Gumps (Assistant hotkey/macro buttons, etc.) through one shared anchor system instead of a second, competing layout engine.
- **`ActionBar` does not render buttons.** Rendering is `ActionButtonGump`'s job; `ActionBar` only decides where each button goes.
- **`GameActionDefinition` contains no UI logic.** It is a plain data/behavior record, safe to reuse from any future presentation.
- **`ActionButtonGump` contains no gameplay logic.** It only renders whatever `GameActionDefinition` it is given and calls `TryExecute()` on tap.

---

## Extending the System

Adding a new gameplay action to an existing bar:

1. Create a new `GameActionDefinition` describing the action.
2. Add it to an `ActionBar` via `AddButton`.
3. No `ActionBar` changes required.
4. No `ActionButtonGump` changes required.

Adding an entirely new bar (e.g. a Combat Bar) is the same pattern: construct another `ActionBar` instance with its own persistence key, and populate it with the relevant `GameActionDefinition`s.

---

## Future Work

- Multiple `ActionBar`s active at once
- Combat Bar
- Spell Bar
- Crafting Bar
- Pet Commands
- Controller-driven layouts
- Cloud/profile-based persistence (an alternate `IActionBarPersistence` implementation)
- Replacing `Tick()`'s polling with `AnchorManager` movement events, if the engine exposes them

---

## Out of Scope

`ActionBar` does not implement:

- Paging
- Scrolling
- Drag-to-reorder
- Categories
- Animations
- Controller navigation

These belong to future milestones.
