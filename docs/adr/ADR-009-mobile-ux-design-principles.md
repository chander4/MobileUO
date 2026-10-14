# ADR-009: Mobile UX Design Principles

## Status

Accepted. This ADR establishes a durable design language; it authorizes no
implementation by itself. Individual widgets/screens still apply these rules
during their own review, same as any other ADR.

## Context

Hollowmere's HUD has been growing one widget at a time (#26 Primary Action,
#27 Action Bar, #29 Vitals, #34 Target Vitals) with each milestone making its
own local calls about visibility, locking, and layout. That's produced a
working HUD but no shared answer to questions like "does a gated feature stay
visible?" or "how much HUD weight does an NPC nameplate get?" — each future
widget would otherwise re-derive its own answer, the same gap ADR-007
identified for touch gestures before it existed.

**Provenance.** These principles were informed by black-box UX observation of
several existing mobile MMORPGs (screenshots and live interaction only — no
decompiled code, extracted assets, or reproduced implementation details). The
rules below are Hollowmere's own, written against Hollowmere's own systems;
where an observed pattern is named for context, it is described behaviorally,
never as something copied.

## Decision

Eight rules, each stated as something a future widget/screen can be checked
against:

1. **Signal density scales with actionability, not with available data.**
   How much a HUD element shows (full bar+name+level vs. a name only vs.
   nothing) should track what the player can *do* about the thing, not how
   much data the engine happens to expose. `PrimaryActionResolver` already
   classifies entities this way (Attack/Talk/Loot/PickUp/None) for the
   Primary Action button; nameplate/overlay weight should read from the same
   classification rather than inventing a second one. Not yet applied to
   nameplates — tracked as follow-up work (see Future implications).

2. **World-space overlays and screen-space HUD are two layers, never one.**
   `HudWidgetHost`/`HudLayoutService` own screen-anchored HUD. Anything
   attached to a world position (a target's distance readout, a quest
   marker) is a different layer with different rules: it moves with the
   camera, it can be occluded, and it does not participate in
   `HudLayoutProfile` placement math. `TargetVitalsGump` already floats a
   screen-anchored bar at a HUD-assigned position rather than tracking the
   target in world-space — that boundary should stay deliberate as more
   overlays are added, not blurred because two things happen to look similar.

3. **A gated feature stays visible; only the action is blocked.** When a
   widget/screen/action is unavailable (level gate, quest state, not yet
   unlocked), show it with its unlock condition rather than hiding it.
   `HudWidgetState.IsVisible` (`HudLayoutProfile.cs`) is currently binary —
   every existing presenter (`VitalsPresenter`, `ActionBarPresenter`,
   `TargetVitalsPresenter`, etc.) only ever shows or hides. "Locked" is a
   third state this rule says must never collapse into "hidden" — a gap the
   current model doesn't yet represent, flagged here before the first
   gated widget has to invent its own answer.

4. **Locked state has one shared treatment, reused everywhere it applies.**
   Same convention every time: a lock affordance plus a short "how to unlock"
   string, not a bespoke look per screen. Same discipline as ADR-007's single
   long-press threshold — one answer, reused, not reinvented per feature. No
   shared component exists yet; the first two consumers should extract one
   rather than copy-pasting a third bespoke version (ADR-008's
   "wait for a second consumer before abstracting" pattern applies here too).

5. **Contextual actions surface near their target, not behind a global
   menu.** `NearestThreatFinder` and the Primary Action pipeline (ADR-003)
   already exist so the player doesn't have to precisely tap a small, moving
   target — that reasoning generalizes: when a widget needs to expose an
   action tied to a specific world entity, prefer surfacing it at/near that
   entity over adding a new global menu entry.

6. **Touch targets have a floor, regardless of visual size.** No new
   tappable widget's `GetIntrinsicSize()` should resolve smaller than
   88×44 — the size `ActionBarPresenter.ButtonWidth/ButtonHeight` and
   `PrimaryActionPresenter.ButtonWidth/ButtonHeight` already ship today, the
   smallest device-validated touch target currently in the HUD. Making it an
   explicit floor turns a coincidence (both happen to agree) into a rule new
   widgets are checked against.

7. **Three content tiers, chosen deliberately per widget:** always-visible
   (Vitals, Stats — persistent player state), contextual (Target Vitals —
   visible only while a target exists), and drill-down (the Options menu,
   the Server Browser — behind an explicit open action). A new widget states
   which tier it's in before layout/anchor is decided; "always-visible" is
   not the default just because it's simplest to build. The in-flight
   Backpack work is this rule's next real test case: it should land as
   drill-down, not always-visible.

8. **Empty states are designed, not absent.** Any filtered/listable UI must
   define what renders when there is nothing to show — a stated message, not
   a blank region. Hollowmere's one existing filterable list, the Server
   Browser's search filter (#19A, `ServerConfigurationListItemView.cs`), has
   no defined empty-result state today — the first concrete place this rule
   applies is fixing that gap, not a hypothetical future screen.

## Alternatives considered

- **Adopt an observed competitor's visual style/terminology directly.**
  Rejected — conflicts with the Design Manifesto's "preserve the soul of
  Ultima Online" principle (`CLAUDE.md`) and is exactly the
  feature-accumulation-without-a-language problem this ADR exists to avoid.
  Behavior can inform a rule; visual style and wording stay Hollowmere's own.
- **An unstructured "UX notes" doc instead of an ADR.** Rejected — Hollowmere
  already has a convention (Architectural Principle #10: "propose an ADR
  before introducing or changing a durable boundary") and these eight rules
  are exactly that kind of durable, cross-cutting boundary. A free-text notes
  doc wouldn't be something future work is actually checked against.
- **A full design system, style guide, or token library.** Rejected for now
  — no second visual-styling consumer exists yet beyond the ADR-0001 theme
  system; introducing a parallel design-system layer before one is needed
  repeats the mistake ADR-008 explicitly rejected for widget packages.
- **Wait for more competitor research before codifying anything.** Rejected
  — the immediate problem is features accumulating without a shared
  language; these eight rules can be refined incrementally as more research
  happens, but having a checkable baseline now is better than none.

## Consequences

- Distance-to-target (#34) is the first concrete instance of rules 2 and 5:
  world-native data (`GameObject.Distance`) surfaced through the existing
  screen-anchored Target Vitals widget rather than a new overlay system.
- Nameplate tiering (paused, next up after this ADR) is the next candidate
  for rule 1 — it should read from `PrimaryActionResolver`'s existing
  classification, not add a second one.
- Locked-state and empty-state handling are not yet shared components; the
  next screen that needs either should extract one rather than adding a
  third bespoke implementation.
- The Server Browser's search filter (#19A) has an undefined empty-result
  state today (rule 8) — a small, pre-existing gap this ADR surfaces rather
  than one it introduces; fixing it is follow-up work, not required by this
  ADR alone.
- Existing widgets (Vitals, Stats, Action Bar, Primary Action) are not
  retrofitted by this ADR — it governs new work going forward, same as
  ADR-007 did for touch gestures.

## Future implications

- If a real design-system/token layer becomes justified (second visual
  consumer beyond the ADR-0001 theme), it should absorb rules 3/4/8 as
  reusable components rather than each screen re-implementing them.
- Controller-oriented layouts (ROADMAP_VISION.md) will need their own answer
  to rule 6 (touch target floor has no meaning for a D-pad) — revisit then
  rather than generalizing prematurely now.
