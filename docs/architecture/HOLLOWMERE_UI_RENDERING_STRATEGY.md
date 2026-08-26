# Hollowmere UI Rendering Strategy

**Status: durable architectural philosophy.** This document defines where every
future UI feature's technology choice comes from. It supersedes the framing in
`docs/architecture/ARCHITECTURE_REVIEW.md`, which called the two-rendering-surface
situation "the single largest piece of debt" — that was half right. The debt isn't
having two rendering systems. The debt is not having a rule for which one a new
feature uses. This document is that rule.

## The premise this document rejects

The question "ClassicUO Gumps or Unity UI?" is usually framed as a temporary
inconsistency to be resolved by eventually picking a winner. That framing is wrong,
and worth stating plainly before anything else: **Hollowmere should keep both,
permanently, on purpose.** They are good at genuinely different things. Forcing
everything into one technology doesn't remove complexity — it just moves the
complexity somewhere worse:

- Reimplementing ClassicUO's native Gump system (inventory, spellbook, vendor
  windows, health bars — all deeply wired into `GameActions`, `SelectedObject`,
  `AnchorManager`, and the wire protocol itself) in Unity UI would mean
  permanently maintaining two parallel implementations of the same game systems,
  for zero player-facing benefit. The player doesn't care what rendered their
  backpack. Hollowmere's maintainers would care a great deal about maintaining it
  twice.
- Reimplementing Unity-side app chrome (settings, onboarding, search/picker
  overlays, touch input capture) in the Gump system would mean giving up Unity's
  layout, animation, and platform-integration tooling — the things Unity is
  actually better at — for the sake of a false consistency.

So this document does not choose a winner. It draws the boundary precisely enough
that nobody has to guess again.

## The rule, stated once

**If it represents the game's own state — an object, a character, an inventory, a
spell, a piece of the world protocol talks about — it's a Gump. If it's Hollowmere's
own app chrome — layout, navigation, input capture, search, settings, anything that
exists to *organize access to* the game rather than *be part of* the game — it's
Unity UI.**

Everything in this document is that sentence, worked out in detail.

## The pattern already exists — this formalizes it, doesn't invent it

Before writing a new rule, the codebase was checked for what it already does. It
already sorts cleanly:

**Gump-rendered (ClassicUO `Batcher2D`, `AnchorableGump`/`AnchorManager`):**
`ActionButtonGump`, `ActionBar` (composes `ActionButtonGump` via `AnchorManager`),
the Primary Action button, `BaseHealthBarGump`/`HealthBarGump`, and every native
ClassicUO window — containers, spellbooks, vendor/shop Gumps, the world map.

**Unity UI (`MonoBehaviour`, Canvas/UGUI):** `ActionPickerPresenter` (the Universal
Action Picker), `ChatPopupPresenter`, the floating joystick and mobile input
capture, the Mobile Settings panel, and the `HollowmereUI` theme/typography/button/
panel system.

Look at what actually separates those two lists: the Gump-side items are all things
that *are* game state — a button that fires a `GameActionDefinition` against a
selected game object, a health bar rendering a mobile's actual hit points, a
container showing actual items. The Unity-side items are all things that exist to
help the player *reach* game state — a picker for finding an action, a popup for
composing a chat line, a joystick for capturing input, a settings screen that isn't
part of the game world at all.

Nobody planned this split. It happened because each feature's author reached for
the tool that fit. That's the strongest possible evidence the rule is right — it's
already what good instinct produced, repeatedly, independently. This document just
writes it down so the next feature doesn't have to rediscover it by instinct too.

---

## 1. What belongs to ClassicUO Gumps forever

Anything that renders or manipulates actual game/world state, full stop:

- Inventory and container contents
- Character paperdoll and equipment
- Spellbooks, skill lists, ability state
- Vendor/shop buy-sell windows
- Health/mana/stamina bars for any entity
- Targeting and world-object selection
- Anything that reads `World`, `SelectedObject`, or issues `GameActions` calls
  against a live server-tracked object

This is "forever" because the alternative — reimplementing deep, protocol-coupled
game state rendering in a second technology — has no plausible payoff. ClassicUO's
Gump system already speaks the wire protocol fluently. Unity UI never needs to.

## 2. What belongs to Unity UI forever

Anything that is Hollowmere's own interface to the game, not the game itself:

- Touch/gesture input capture (the joystick, gesture recognizers)
- Search, filter, and picker overlays (the Universal Action Picker)
- Settings, onboarding, and account-adjacent screens
- Chat composition UI (as distinct from the chat log itself, which is a native
  ClassicUO system)
- Any screen-organizing shell: docking chrome, tab bars, navigation

This is "forever" for the mirror-image reason: these are exactly the surfaces where
Unity's layout system, animation tooling, and platform integration (safe areas,
accessibility APIs, text input) are doing real work that the Gump system has no
equivalent for, and never needs to.

## 3. Which new UI should always be Gumps

Any redesign or new feature that is fundamentally *about* a game object stays a
Gump: a redesigned inventory, a redesigned spellbook, a redesigned vendor window,
a new health-bar layout, a new context-sensitive action button. The visual design
can and should modernize (per `MOBILE_DESIGN_GUIDELINES.md`); the rendering
technology doesn't change, because the reason it's a Gump was never about how it
looks — it's about what it's connected to.

## 4. Which new UI should always be Unity UI

Any new app-chrome surface: a new settings screen, a new onboarding flow, a new
search or filter overlay, any new input-capture mechanism (a future controller
input layer belongs here too — see ADR-007's note that controller support should
reuse existing gesture *semantics*, which is a separate question from which
rendering system displays controller prompts).

## 5. Wrapping, hosting, and migration

**Should Unity wrap Gumps?** Yes — for *layout and composition*, never for
*content*. A Persistent HUD is the concrete case: Unity UI should own the shell
(safe-area handling, docking positions, responsive layout across phone/tablet), and
slot existing Gump-rendered elements (health bars, the Action Bar, the Primary
Action button) into positions that shell defines. Unity provides the frame;
ClassicUO's Gump system still renders everything inside it. This is compositional
wrapping, not reimplementation — the HUD shell never redraws a health bar's
contents, it just decides where the health bar's Gump appears on screen.

**Should Gumps host Unity?** No. There's no case in this document's other answers
that needs it, and ClassicUO's `Batcher2D` render pipeline has no native mechanism
for embedding a Unity Canvas element inside a Gump's own render tree without
building custom bridging machinery to support a direction nothing actually
requires. If a future need seems to call for this, treat that as a sign the feature
was scoped backwards — the game-state content probably belongs in the Gump, and the
chrome around it belongs in a Unity shell wrapping the Gump, not the other way
around.

**Should they remain independent?** Mostly, yes — with the wrapping relationship
above as the one deliberate exception, confined to shared layout shells (HUD,
future docking), not casual reach-across-the-boundary calls from feature code.

**Should there ever be migration?** Not wholesale. No project-wide "port everything
to Unity" or "port everything to Gumps" effort is ever justified by this
architecture — see the premise section above. Individual systems may migrate
technology *at redesign time*, as a side effect of that system's own roadmap theme
(e.g., if Inventory Evolution's touch-native redesign genuinely needs layout/
animation capability the Gump system doesn't have, that redesign can choose Unity
UI for its new implementation). That's a per-system decision made when the system
is already being rebuilt for other reasons — never a standalone migration project
whose only goal is technology consistency for its own sake.

## 6. Input flow between the two systems

This already has a working answer, built out of necessity during #24's touch-
dispatch investigation, and it generalizes: **exactly one system owns a given
screen point at a time, decided before either system sees the touch.**
Concretely, `GameController`'s touch pipeline checks whether a touch lands over
Unity UI (`LeanTouch.PointOverGui`) before ClassicUO's own `Mouse`/`UIManager`
dispatch ever runs; when it does, the touch is zeroed out for the Gump side
entirely rather than both systems independently guessing.

The rule for all future hybrid layout (Persistent HUD, Docked Layout) is the same
invariant, generalized: **a screen region is unambiguously owned by one system,
determined by layout, not by which system happens to process the event first.**
Two systems racing to interpret the same touch — which is exactly the shape of
bug #24 spent real effort root-causing — is the failure mode this rule exists to
prevent. Any new composited layout (a HUD shell hosting a Gump-rendered health bar)
needs an explicit answer to "who owns this pixel" before it ships, not an assumption
that it'll sort itself out.

## 7. Diagnostics across both systems

`Hollowmere.Diagnostics.Diag` (ADR-005) is already system-agnostic — it's a static
logger with no dependency on either rendering technology. The existing `UI`
category currently only has Gump-side call sites (`ActionButtonGump`,
`UIManager.OnLeftMouseButtonDown`). As Unity-side presenters (`ActionPickerPresenter`,
`ChatPopupPresenter`, a future HUD shell) need the same kind of dispatch tracing,
they should log through the same `Diag` framework and the same `UI` category by
default — not a parallel Unity-specific logging mechanism, and not automatically a
new category. Split into a new category only if a real investigation shows that
"Gump UI" and "Unity UI" tracing need to be toggled independently often enough to
justify it (matching ADR-005's own stated principle: add categories deliberately,
matching a real subsystem, not preemptively).

## 8. How future contributors should decide

Before writing a new UI element, answer one question: **does this represent
something the game world already knows about, or does it exist to help the player
reach something the game world knows about?** The first is a Gump. The second is
Unity UI. If the honest answer is "both, at different layers" (the Persistent HUD
case), split it: the shell that organizes and positions is Unity UI, the elements
it hosts that render actual game state stay Gumps.

If a contributor genuinely can't answer the question — the feature seems to be
neither, or seems to need to be both in a way this document doesn't resolve — that
is exactly the situation that warrants a new ADR, not a guess.

## 9. Boundaries that should never be crossed

- **Never duplicate the same piece of game state in both systems.** No
  Unity-rendered health bar existing alongside a Gump-rendered health bar for the
  same entity. One system renders it; the other, if it touches that entity at all,
  reads the same underlying state through `GameActions`/`SelectedObject`, never
  maintains its own copy.
- **Never let a Unity element reach into Gump internals, or a Gump reach into a
  Unity `MonoBehaviour`'s internals, directly.** Cross-system communication goes
  through the same public surface either system would use anyway
  (`GameActionDefinition`, `UIManager`, `SelectedObject`) — never a special-cased
  back door built for one specific pair of components.
- **Never let two systems independently decide who owns a screen region or a
  touch.** Ownership is a layout-time decision (§6), not a runtime race.
- **Never pick the rendering technology based on which is more convenient to
  implement in the moment.** That's exactly how an unplanned, inconsistent hybrid
  happens — pick based on §8's question, even when the "wrong" answer would be
  faster to build today.

## 10. Mistakes that would create long-term debt

- **Building two parallel widget libraries** — a Gump-side button and a visually-
  and-behaviorally-similar-but-not-identical Unity-side button, each maintained
  separately because "it was easier to add to whichever system I was already in."
  This is the single most likely way this architecture quietly rots: not a dramatic
  wrong turn, just accumulated small conveniences.
- **Treating this document's rule as advisory rather than a real gate.** A rule
  that gets overridden "just this once" for a deadline stops being a rule after
  the second exception.
- **Assuming migration is cheap and therefore deferring the technology decision**
  on a new feature "for now, we'll move it later if needed." §5 already says
  migration is legitimate at redesign time — but that's different from building
  something without deciding, on the assumption a decision can be retrofitted
  cheaply. It can't; the coupling to `AnchorManager` or to Unity's layout system
  gets load-bearing fast.
- **Letting diagnostics diverge** — a Unity-side `Debug.Log` habit growing back
  next to the Gump side's disciplined `Diag` usage, recreating the exact "ad hoc
  debug scaffolding nobody trusts" problem ADR-005 was written to close.
- **Scoping a feature "backwards"** (§5's note on Gumps hosting Unity) — building
  toward a direction this document says not to build, rather than reshaping the
  feature to fit the boundary that already works.

## 11. Five to ten years out

"Translate the interface, never the game" is the whole answer to what this
architecture should look like at scale. The Gump system's job never changes: it
stays the permanent, protocol-native surface for everything that *is* Ultima
Online, exactly as durable as the underlying game systems themselves — a
spellbook redesigned five times over a decade is still a Gump on version five,
because it's still a spellbook. Unity UI's job never changes either: it stays the
permanent surface for however Hollowmere chooses to *organize access* to that game
at any given moment — which is allowed to look completely different in ten years
(a Persistent HUD giving way to something else, a Docked Layout system replaced by
whatever mobile interaction patterns exist by then) without that evolution ever
requiring the game-state layer underneath to change at all.

That's the actual argument for keeping the hybrid permanently instead of resolving
it: **the two systems age at different rates for a real reason.** The game doesn't
change; how a player reaches it should be free to change as often as it needs to.
Collapsing both into one technology would force them to evolve at the same rate,
for no reason except uniformity — and uniformity was never the goal. Legibility of
*which system owns what, and why* was the goal, and that's what this document
exists to lock in.

### Applying it to the named future systems

- **Persistent HUD** — Unity-owned shell (§5), hosting Gump-rendered health bars,
  Action Bar, and Primary Action button as slotted content. The shell's job is
  layout and safe-area handling; it never reimplements what it's hosting.
- **Docked Layout** — same pattern as Persistent HUD, generalized: a Unity docking
  shell that can host either Gump content (an undocked-but-redesigned inventory
  window) or pure Unity content (a fully redesigned inventory that outgrew the Gump
  system at its own redesign time, per §5's migration allowance). The docking
  *mechanism* is always Unity's job regardless of what's docked.
- **Tablet mode** — not a rendering-technology question at all; it's a layout-
  scaling concern that applies orthogonally inside both systems (Unity's own
  layout tooling on one side, `AnchorManager` plus explicit breakpoints on the
  other, per `ROADMAP_VISION.md`'s Tablet Experience theme).
