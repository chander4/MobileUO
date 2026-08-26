# Hollowmere Roadmap Vision

This is not a backlog. There are no issue numbers, no estimates, no sequencing
promises here. This is the set of themes Hollowmere's future work should be drawn
from, each evaluated against `HOLLOWMERE_PRODUCT_VISION.md` before it becomes real
scope. A planning session picks *from* this document; it doesn't need to invent
scope from nothing.

---

## Modern Mobile UX

**Goal:** Replace the remaining desktop-shaped chrome (floating draggable Gumps,
hover-dependent tooltips, mouse-emulation input) with genuinely touch-native
equivalents — a persistent HUD, docked information instead of floating windows,
gestures instead of pixel-precise dragging.

**Player value:** Less time fighting the interface, more screen showing Britannia.
The single biggest remaining gap between "playable on a phone" and "designed for a
phone."

**Architectural impact:** Likely the largest theme here. Today, ClassicUO's native
Gump rendering (via `Batcher2D`) and Hollowmere's native Unity UI (joystick, chat
popup, Action Bar's Canvas layer) are two separate rendering surfaces sharing one
screen. A real Persistent HUD milestone forces a decision about that boundary — see
the architecture review's technical-debt section.

**Dependencies:** Should follow, not precede, a decision on the dual-rendering-
surface question above. Building more HUD on an unresolved rendering boundary
compounds the debt instead of paying it down.

**Possible milestones:** A Persistent HUD design (the Primary Action button's
current top-right position is explicitly a placeholder for this). A docked-UI
system replacing floating Gump drag-and-drop for frequently-used windows. An audit
of remaining hover-dependent tooltips.

---

## Player Expression

**Goal:** Let players customize how they play without touching mechanics — macro
authoring, quick-slot personalization, chat phrase customization, Action Bar
loadouts per character or per activity.

**Player value:** UO has always rewarded players who build their own workflow. On
desktop that's hotkeys and macros; the mobile equivalent needs the same expressive
range without a keyboard.

**Architectural impact:** Extends the existing Action Pipeline (ADR-003) and Action
Bar (ADR-004) rather than replacing them — this theme is largely "give players more
ways to configure what already exists."

**Dependencies:** `ActionCatalog`'s macro-source deliverable (previously scoped,
not yet built) is a likely prerequisite for real macro authoring.

**Possible milestones:** Named, saveable Action Bar loadouts. Custom macro
authoring UI. Per-character quick-chat phrase sets.

---

## Inventory Evolution

**Goal:** A touch-native answer to container/backpack management — currently the
least-adapted major system, still shaped entirely around mouse drag-and-drop.

**Player value:** Inventory triage under pressure (looting, crafting, trading) is
real UO gameplay, not administrative overhead — it needs to be *fast and legible*
on touch, not just technically possible.

**Architectural impact:** Significant — touch-friendly item movement (tap-to-move-
to-container vs. drag), stack-splitting without pixel precision, and quantity
selection all need first-class treatment. Likely produces new Interaction-layer
primitives reusable elsewhere (a "move this item" action shape sits naturally next
to the Primary Action pipeline).

**Dependencies:** Should reference `MOBILE_DESIGN_GUIDELINES.md`'s "never require
pixel precision" rule directly — this is the system where that rule is most
currently violated.

**Possible milestones:** Tap-based item transfer as an alternative to drag.
Touch-friendly stack splitting. A redesigned container view evaluated against
Persistent HUD's docking model.

---

## Combat Modernization

**Goal:** Evaluate ability/spell access, targeting, and combat-state awareness
(health, mana, stamina, notoriety) for touch, building on the Primary Action
button's Attack verb rather than replacing it.

**Player value:** Combat is UO's highest-stakes, highest-attention moment — the
worst possible place for interface friction.

**Architectural impact:** Likely extends the Primary Action pipeline (a "primary
combat action" is a natural sibling to Attack) rather than introducing a parallel
combat-UI system. Targeting small/fast-moving mobiles on touch (a real,
already-reported difficulty) needs a genuine solution, not a bigger hit-box patch.

**Dependencies:** Should be evaluated after Inventory Evolution's tap-precision
work, since the same "don't require pixel accuracy" problem applies to targeting.

**Possible milestones:** A touch-appropriate targeting-precision solution. Ability
access that doesn't require memorized hotkeys. Combat-state HUD elements evaluated
against the Persistent HUD design.

---

## Social Improvements

**Goal:** Reduce friction in expressing UO's social systems (guild, party, world
chat, player trade) without changing what those systems are or gamifying them.

**Player value:** Spontaneous social interaction is core to why UO's world feels
alive. Friction here is friction on the game's soul, not just its UI.

**Architectural impact:** Builds on the existing Quick Chat and chat popup work.
Likely needs Action Pipeline integration for party/guild actions (invite, kick,
promote) to stay consistent with how every other action in the game is expressed.

**Dependencies:** None blocking — can proceed independently of the HUD/rendering
questions above.

**Possible milestones:** Faster party/guild action access. Chat that doesn't
compete for screen space with the world (ties into Modern Mobile UX). Player-trade
flow evaluated for touch.

---

## Accessibility

**Goal:** Systematic accessibility review and remediation — not incidental
touch-target fixes discovered while working on something else, but a deliberate
pass against `HOLLOWMERE_PRODUCT_VISION.md`'s accessibility philosophy.

**Player value:** Direct — this is who gets to play the game at all, and how
comfortably.

**Architectural impact:** Touch-target sizing likely touches every Interaction-
layer control (`ActionButtonGump` and friends). Colorblind-safe notoriety/health
signaling may need new visual language layered onto existing systems, not new
mechanics. Text scaling needs evaluation against the `HollowmereUI` typography
system.

**Dependencies:** Should happen before major new UI surfaces ship, not after,
since remediating an already-shipped UI is more expensive than designing it in.

**Possible milestones:** Minimum touch-target-size audit and fix pass (the small
ground-item tap-precision issue found during #24 is a concrete, already-known
instance). Colorblind-safe status signaling. Text scaling support.

---

## Performance

**Goal:** Keep the game responsive on lower-end and older mobile hardware as the
engine surface and UI complexity grow.

**Player value:** A phone game that stutters or drains battery fast loses players
regardless of how good the design is.

**Architectural impact:** The engine review (below) flags asset loading and the
dual-rendering-surface as the most likely sources of future performance debt.
Requires profiling data, not assumptions, before committing to specific fixes.

**Dependencies:** Needs real device profiling across a range of hardware before
this theme can produce concrete milestones.

**Possible milestones:** A profiling pass on low-end target devices. Asset-loading
audit (building on the iOS case-sensitivity fix already in `UOFileManager.cs`).

---

## Controller Support

**Goal:** A controller input path, likely for tablet/TV-adjacent play, mapping onto
the same gesture semantics touch already established (ADR-007) rather than
inventing a third input model.

**Player value:** Expands where and how Hollowmere can be played without
fragmenting the interaction model players already learned.

**Architectural impact:** The real test of whether the Interaction layer's
abstractions (ADR-002, ADR-003) generalize past touch. If `GameActionDefinition`
and the Primary Action pipeline can drive a controller face-button prompt without
modification, that validates the layering decision; if not, that's important
signal about where the boundary was actually drawn.

**Dependencies:** Should follow, not precede, Modern Mobile UX's HUD work — a
controller needs something stable to navigate.

**Possible milestones:** A minimal controller-driven Primary Action test (attack/
talk/loot/pick-up via a controller button) as a validation exercise before
committing to full controller support.

---

## Tablet Experience

**Goal:** Evaluate Hollowmere's UI at tablet scale — more screen space changes the
"one thumb, one hand" constraint that drives most current mobile decisions.

**Player value:** Tablet players get a genuinely tablet-appropriate layout, not a
stretched phone UI.

**Architectural impact:** Tests whether current UI sizing/spacing decisions
(`HollowmereMetrics`) are phone-specific constants or genuinely scale. Likely
needs explicit breakpoints rather than pure proportional scaling.

**Dependencies:** Should follow Modern Mobile UX's HUD decisions, since a docked
HUD's tablet layout differs meaningfully from its phone layout.

**Possible milestones:** A tablet-specific layout audit. Explicit phone/tablet
breakpoints in the metrics system if proportional scaling proves insufficient.

---

## Visual Polish

**Goal:** Motion, feedback, and visual coherence pass once the underlying
interaction models (HUD, Action Pipeline, Inventory) have stabilized.

**Player value:** "Every animation communicates state" (the project's own design
manifesto) — polish here is functional, not decorative.

**Architectural impact:** Low — this theme should mostly consume existing systems,
not restructure them. A signal that it's being pursued too early is if it starts
requiring architectural changes to ship.

**Dependencies:** Deliberately sequenced last among the UI themes — polishing an
interaction model that's about to change (HUD, Inventory) is wasted work.

**Possible milestones:** A motion/feedback audit against the design manifesto's
"every animation communicates state" principle.

---

## Engine Improvements

**Goal:** Pay down the engine-fork technical debt named in the architecture review
below: the ~15 unreviewed override files, the `.meta`-tracking gap for symlinked
files, real DeepWiki/CodeDebrief indexing of the full engine surface, and
evaluating the `DenyMoveItem` fix (and any siblings like it) for upstream
contribution.

**Player value:** Indirect but real — a healthier engine fork means faster, safer
delivery of every other theme on this list.

**Architectural impact:** This *is* architectural work — closing gaps identified in
`ENGINE_MODIFICATIONS.md` and the review below.

**Dependencies:** None — can and should proceed continuously, in parallel with
other themes, rather than being treated as a discrete milestone that's "done" once.

**Possible milestones:** Manual review pass closing the "needs manual review"
inventory entries. A decision on the `.meta`-tracking policy for symlinked files.
A scoped DeepWiki/CodeDebrief index covering the full 73-file engine surface.
