# Hollowmere Decisions

This is not the ADRs. The ADRs (`docs/adr/ADR-001` through `ADR-007`, plus the
earlier `docs/adr/0001-hollowmere-ui-theme-system.md`) explain *why* a decision
was made — context, alternatives, consequences. This file is the short,
declarative "what is currently true" list. Check here first; read the ADR only
if you need the reasoning.

## Rendering strategy

Game state (inventory, spellbook, vendor windows, health bars, anything wired
to `GameActions`/`SelectedObject`) is a ClassicUO Gump, permanently. App chrome
(input capture, search/picker overlays, settings, navigation shells) is Unity
UI, permanently. Unity may wrap Gumps for shared layout (a HUD shell hosting a
Gump-rendered health bar); Gumps never host Unity content.
→ `docs/architecture/HOLLOWMERE_UI_RENDERING_STRATEGY.md`

## Diagnostics strategy

All tracing goes through `Hollowmere.Diagnostics.Diag`, category-based, off by
default, zero-cost when disabled. No bare `Debug.Log` in Engine or Gameplay
code for anything beyond genuinely one-off, throwaway investigation.
→ `ADR-005`

## Persistence strategy

Action Bar layout persists via `PlayerPrefs` (`IActionBarPersistence`,
currently `PlayerPrefsActionBarPersistence`) — deliberately simple, not a
generic save-system abstraction. Diagnostics category toggles are code-only,
no persisted settings, no settings UI.
→ `ADR-004`

## HUD widget system

The HUD is a renderer-neutral layout system: `HudWidgetHost` coordinates
registered widgets, versioned full-snapshot profiles, variants, and Edit Mode;
Gump and Unity presenters retain their own rendering. Widget IDs are permanent,
namespaced persistence API with explicit alias/deprecation migration.
`HudWidgetState` is host-owned layout data only; widget-specific configuration is
versioned `HudWidgetSettings`. No generic controller layer, live profile
inheritance, or widget package system exists by default.
→ `ADR-008`, `docs/architecture/HUD_WIDGET_ARCHITECTURE.md`

## Action system pipeline

A "thing a player can do" is a `GameActionDefinition` — label, action delegate,
`canExecute` predicate, optional icon/tooltip. Every action surface (spells,
skills, pet commands, macros, Primary Action) produces one of these; no
UI element bypasses it to call `GameActions` directly for a player-facing
action.

Context-sensitive resolution (Primary Action) is a strict one-way pipeline:
`SelectedObject → PrimaryActionResolver → PrimaryAction (pure domain value) →
PrimaryActionDefinitionFactory → ActionButtonGump`. The resolver never
references `GameActionDefinition` or anything presentation-shaped.
→ `ADR-002`, `ADR-003`

## Engine modification policy

Never edit a ClassicUO source file while it's still a symlink into
`external/ClassicUO` — convert it to a real, tracked file first. Every
converted file is documented in `docs/architecture/ENGINE_MODIFICATIONS.md`
(purpose, why it differs, mobile-specific or not, upstream-worthy or not).
Prefer an extension point (event, virtual method, Gameplay-layer wrapper) over
a direct engine edit where ClassicUO already exposes one.
→ `ADR-001`, `ADR-006`

## Mobile UX philosophy

Two gestures, used consistently everywhere: tap (primary action) and long-press
(500ms threshold, <5px movement tolerance — secondary/destructive action,
replacing right-click). No feature invents a third gesture or a different
long-press threshold without a stated reason and an ADR.
→ `ADR-007`

## Protected systems (never change without explicit approval)

- `ClientRunner` movement logic
- Networking
- Packet serialization
- Asset loading pipeline

## Stable systems (do not refactor without approval)

- Mobile Input V2
- `FloatingJoystick`
- LeanTouch integration
- `ClientRunner` movement
- Raw touch input
- Networking
- Mobile Settings

(Both lists are inherited from the project root `CLAUDE.md`, which remains
binding — this file doesn't relax or replace it.)

## Composition over inheritance for player actions

A new verb or action is a new `GameActionDefinition` instance, not a new
subclass of `ActionButtonGump`. The button class stays generic; behavior is
injected as data. This is why adding "Open Door" or "Mount" as a future Primary
Action verb means one new resolver case and one new factory method — never a
new button type.

## Never duplicate engine behavior

No Unity-rendered UI element maintains its own copy of state a Gump already
renders (e.g., no second, Unity-side health bar existing alongside a
Gump-rendered one for the same entity). One system renders a given piece of
game state; everything else reads it through the same public surface
(`GameActions`, `SelectedObject`, `UIManager`) rather than tracking a parallel
copy.
→ `docs/architecture/HOLLOWMERE_UI_RENDERING_STRATEGY.md` §9
