# Hollowmere — Next Planning Session Starting Point

**Status as of `v0.2.0-mobile-foundation`.** This file is not a roadmap — it's a
summary of where the project stands and a list of areas that need evaluation before
the next milestone's scope gets decided. No priorities, sequencing, or commitments
below; that's the next planning session's job.

## Where the project actually is

- Hollowmere is a maintained ClassicUO engine fork (73 confirmed engine-level
  modifications across rendering, input, networking, asset loading, UI, mobile
  runtime, utility, and configuration), not a thin wrapper — see
  `docs/architecture/HOLLOWMERE_ENGINE.md` and `ENGINE_MODIFICATIONS.md`.
- Engine files are now tracked normally in git; the "local override, don't worry
  about it" framing is retired.
- A permanent, category-based diagnostics framework (`Hollowmere.Diagnostics.Diag`)
  exists and is wired into Input/UI/ItemHold/Actions/Networking call sites — off by
  default, toggleable per category, effectively free when disabled.
- The interaction primitive stack is in place: `GameActionDefinition` →
  `ActionButtonGump` → `ActionBar`, plus the context-sensitive Primary Action button
  (#24) built on `PrimaryActionResolver`/`PrimaryActionDefinitionFactory`.
- Mobile QoL basics landed opportunistically this cycle: world map pinch-zoom/pan,
  quick chat, vendor quantity, skills sizing, universal action picker, chat popup,
  long-press-to-close health bars.
- ~15 engine files remain flagged "needs manual review" in the inventory (unread,
  non-trivial diffs) — not blocking, but not verified either.
- One known, documented non-issue: the SpellbookGump symlink/index question was
  investigated and closed as a false alarm (`docs/known-issues/SPELLBOOK_SYMLINK.md`).

## Areas to evaluate next (unordered, undecided)

- **Gameplay interactions** — what beyond Primary Action needs a touch-native
  redesign vs. what's already good enough.
- **Inventory** — container/backpack UX on touch; still largely desktop-shaped.
- **Looting** — Primary Action covers single-corpse loot; multi-corpse/loot-all
  patterns not evaluated.
- **Spellcasting** — touch-complete per an earlier session, but not re-evaluated
  against the Primary Action/Action Bar system's newer conventions.
- **Vendors** — quantity selection improved earlier; buy/sell flow as a whole not
  reassessed.
- **Banking** — not touched this cycle at all.
- **HUD** — Primary Action button's position is explicitly temporary pending a real
  Persistent HUD design; nothing else HUD-related has been scoped.
- **Docking** — no docked-UI system exists yet; floating Gumps are the only model.
- **Tap-to-move** — not implemented; current movement is joystick-only (Mobile Input
  V2, a Stable/Protected system per `CLAUDE.md` — any change here needs explicit
  approval regardless of what this evaluation concludes).
- **Controller support** — not started, not scoped.
- **Accessibility** — not evaluated as its own topic; incidental touch-target-size
  issues have come up (e.g. small ground items) but no systematic pass has happened.
- **Artwork** — no custom art has been introduced; everything renders through
  stock ClassicUO assets.

## Also worth surfacing in that session

- The ~15 "needs manual review" engine files (see `ENGINE_MODIFICATIONS.md`'s summary
  table) — decide whether they get reviewed before or alongside the next milestone.
- Whether to pursue upstreaming the `DenyMoveItem` recovery fix to ClassicUO proper.
- The `.meta`-tracking policy for the ~325 still-symlinked (untouched) engine files —
  deliberately left open during the engine-baseline work.
- Real OpenDeepWiki/CodeDebrief indexing scoped to the full 73-file engine list
  (today's index only covers a hand-picked integration-point subset).
