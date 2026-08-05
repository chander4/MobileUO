# Hollowmere Mobile Modernization Vision

**North star: preserve every gameplay mechanic while reimagining every
interaction for touch.** Every phase below is judged against one test -
does this make someone forget they're playing a port? Not "is this
easier to reach than before" (that's the trap Design Manifesto principle
9 exists to catch), but "does this feel like it was built for a phone."

This is a vision + phase roadmap, not a detailed implementation plan for
any single phase - each phase gets its own scoping document (matching
the #21A/#21B/#22 pattern) when it's actually started.

Nothing here has been implemented. Sequencing and phase boundaries are a
starting proposal, not final.

---

## Why this ordering

**0 → 0.5 → 1 → 2 → 4 → 5 → 3**, with two cross-cutting requirements
(Motion & Juice, Audio Feedback) threaded through every phase rather than
scheduled as their own late phase.

**Phase 0 (Interruption Resilience) goes first, ahead of everything,**
including the structural UI work. It's invisible in a demo and easy to
skip past when scoping - but it's the difference between "occasionally
janky app" and "I trust this with my time," and unlike a small button or
a floating window, nobody notices it's missing until it burns them once.
By then the damage to "feels like a real game" is done. It's also mostly
independent of the UI phases (network/lifecycle, not Gump layout), so it
doesn't block or get blocked by anything else here.

**Phase 0.5 (First 30 Seconds)** comes next for the same reason -
first impression, and it's largely parallel/independent work (it
overlaps with the already-in-flight "Modern Server Browser" milestone
from CLAUDE.md's Current Milestone - Favorites/Search/Default Server is
already doing real "forget it's a port" work, just not framed that way
until now).

**Phase 1 (docked layout)** is the foundation the rest of the structural
work sits on. Phases 2, 4, and 5 are all "a thing that lives in the
dock" - building them after the dock exists means they're native
citizens of the new layout from day one, instead of being built once as
floating Gumps and migrated later.

**Phase 3 (tap-to-move) goes last** for two reasons: it's the
highest-risk phase in the whole vision (touches `ClientRunner`, an
explicitly protected file, unlike everything else which stays in
Presentation/Gump-adjacent or lifecycle territory), and it benefits from
the rest of the modernized shell existing first in case it needs its own
UI (a path preview, a "walking..." indicator).

This ordering also matches the project's existing cheap-before-expensive
value - only phase 3 touches Engine-layer code.

---

## Relationship to Issue #22 (Universal Action Picker)

Complementary, not competing. #22 is about *what* can go on an action
bar (any spell/skill/macro/pet command, player-chosen). This vision is
about *where on screen* things live and *how the screen is organized*
(plus reliability and first-impression work outside the UI entirely).
Once Phase 1's dock exists, an `ActionBar` populated via #22's picker is
one of the things that lives inside it, the same as the HUD (Phase 2) and
the context action button (Phase 4). #22 can proceed independently of
this vision or be sequenced alongside Phase 1 - they don't block each
other.

---

## Cross-cutting requirements (apply within every phase, not a phase of their own)

### Motion & Juice

Design Manifesto principle 6 ("every animation communicates state") is
already written down but nothing's been built against it yet. Native
mobile games have constant small feedback - button press states, damage
numbers popping, loot bursts, screen transitions. UO's Gump UI has
essentially none of this. Scheduling this as one late "polish pass"
phase risks it getting deprioritized/cut; instead, every phase below
(dock transitions, HUD updates, the context button's press state, toast
entrance/exit) should ship with real motion, not just static repositioning.

### Audio Feedback

Mobile players expect tap sounds, success chimes, confirmation dings -
audio does a lot of the "this responded to me" work that desktop players
get from visual precision alone. Same reasoning as Motion & Juice: bake
it into each phase's own scope rather than a separate pass. Needs a short
research check first - what UI-feedback audio (as opposed to world/combat
sound) already exists in this codebase, likely close to none.

---

## Phase 0 — Interruption Resilience

Mobile play is inherently interrupted - a phone call, a notification, the
screen locking, switching apps to check something. Confirmed via direct
search: **zero `OnApplicationPause`/`OnApplicationFocus` handling exists
anywhere in this codebase today.** If backgrounding the app drops the
connection, loses target/state, or dumps the player back to login, that
reads as "this was built for a desktop and squeezed onto a phone" harder
than any single UI rough edge.

- Handle Unity's app pause/resume/focus lifecycle explicitly - what
  should happen to the network connection and game state when
  backgrounded, and how the player is brought back in (silent
  reconnect vs. a clear "you were disconnected, tap to resume" state)
  rather than an unexplained black screen or silent failure.
- Needs research into what ClassicUO's own connection/timeout handling
  already does on socket loss before designing the mobile-specific
  wrapper around it.

---

## Phase 0.5 — First 30 Seconds

Login screen, server selection, connecting - UO's rawest desktop-era
surface, and the very first thing anyone sees. Overlaps with the
already-in-flight Modern Server Browser milestone (Favorites, Search,
Default Server, Validation) - that work already counts toward this goal.

- Audit the actual login → server-select → connecting → in-game sequence
  end to end for anything that reads as technical/desktop (raw socket
  states, unstyled loading moments, no branding) rather than a mobile
  app's polished cold-open.

---

## Text Legibility

Small, foundational, worth doing early rather than folded silently into
Phase 1. UO's default UI text was sized for someone sitting close to a
monitor; held at phone/tablet distance, that's a readability mismatch -
separate from touch-target *size* (already addressed for Skills), this
is about whether the text is comfortably readable at all at typical
mobile viewing distance.

---

## Phase 1 — Docked Layout Foundation

Replace floating, draggable, overlapping Gump windows with a fixed-region
docking layer, **on mobile only** (desktop keeps today's floating Gumps -
this is additive, not a replacement of the underlying system).

- Gump *content* and rendering logic stay untouched - `ContainerGump`,
  `SpellbookGump`, etc. keep working exactly as they do today. What
  changes is *where* a Gump's window is anchored and whether it can
  overlap others.
- A small set of fixed regions (e.g. a bottom sheet for
  inventory/spellbook/skills-type content, a persistent top/side strip
  for HUD elements) that specific Gump types get routed into instead of
  floating at a mouse-drop position.
- This is the biggest single-phase lift in the vision, but it's still
  additive composition over existing Gumps - no protocol, no packet
  handling, no `ClientRunner` changes.

**Unblocks:** Phases 2, 4, 5 (all become dock-native from the start).

---

## Phase 2 — Persistent HUD

Health/mana/stamina/buff icons move out of draggable, closable Gumps into
a fixed dock region - always visible, can't be accidentally dragged away
or closed, the way every modern mobile game's vitals work.

- Reads the same underlying player/buff state existing HUD Gumps already
  read; this is a presentation change, not new data plumbing.

---

## Phase 4 — Context-Sensitive Primary Action

One prominent, dock-anchored button whose behavior changes based on
`SelectedObject`/`TargetManager` state - attack what's targeted, talk to
an NPC, loot a corpse, pick up an item - instead of hunting through
per-object menus for the right verb.

- Likely reuses `ActionButtonGump`'s rendering with a dynamically-swapped
  `GameActionDefinition` based on what's currently selected, rather than
  a new button type.
- Needs care to stay an interaction shortcut, not a gameplay change - it
  should trigger the same actions a manual click/context-menu already
  offers, never a new capability.

---

## Phase 5 — Toast/Snackbar Feedback

Route combat log and system messages into a dock-anchored toast queue
sized for thumb-height reading, alongside (not necessarily replacing)
existing overhead floating text, which was designed for a much larger
desktop viewport.

---

## Phase 3 — Tap-to-Move as Primary Movement

Flip the default movement model: tap-and-pathfind becomes primary,
`FloatingJoystick` becomes an optional/legacy mode rather than the only
one. The highest-risk phase in this vision - touches `ClientRunner`
(protected, requires explicit approval) and the most-used interaction in
the entire game, so it goes last, after the rest of the modernized shell
exists to integrate with.

---

## Out of scope for this vision

- Any change to actual UO gameplay systems (skills, combat math, economy,
  housing) - this is entirely about interaction/presentation, per the
  north star and Design Manifesto principle 2.
- A specific implementation plan for any phase - each phase gets its own
  scoping pass (research + deliverable breakdown + approval) before
  implementation starts, same discipline as #21A/#21B/#22.

---

**Not started. This is a sequencing/vision document. Phase 0 needs its
own detailed scoping document before implementation begins.**
