# AGENTS.md — Hollowmere Repository Constitution

This is the entry point for every AI coding agent working on this repository —
Claude Code, Cursor, ChatGPT, Gemini, Codex, or any future tool. Read this file
first, every session, regardless of what conversation history you do or don't
have. Nothing here should require memory of a prior conversation to understand.

## What is Hollowmere?

Hollowmere is a mobile-first Ultima Online client built as a maintained
ClassicUO engine fork inside Unity. It is not "MobileUO with some improvements" —
it has a stated product identity, a documented architecture, and a philosophy
that every feature is measured against before it ships.

Full answer: **`HOLLOWMERE_PRODUCT_VISION.md`** — read it before proposing or
building anything that touches gameplay, UI, or interaction. Don't restate its
content elsewhere; link to it.

## Core principles (the short version — read the source for the real version)

1. Translate the interface, never the game. The sandbox, the economy, full-loot
   risk, skill-based progression — none of that is a UI problem to be solved.
2. Mobile is the primary target, not a shrunk-down desktop client.
3. Every engine modification is deliberate, tracked, and documented — never a
   silent "local override."
4. Architecture decisions are recorded, not folklore. If it isn't written down,
   it isn't a decision yet.

Source of truth: `HOLLOWMERE_PRODUCT_VISION.md`, `MOBILE_DESIGN_GUIDELINES.md`,
the project root `CLAUDE.md` (contains the original design manifesto and the
Protected/Stable systems list — still binding).

## What every agent should read first

In this order, before writing code:

1. **This file.**
2. **`PROJECT_STATE.md`** — the current snapshot: what's shipped, what's in
   progress, what's known debt. Don't trust your training data or a stale
   conversation summary over this file.
3. **`HOLLOWMERE_PRODUCT_VISION.md`** — why the project exists and what it's
   for.
4. **`DECISIONS.md`** — short, declarative "what is currently true" rules.
   Faster to check than reading every ADR.
5. **`docs/architecture/ARCHITECTURAL_PRINCIPLES.md`** — enduring rules that
   guide implementation across subsystem boundaries.
6. **The relevant ADR(s)** in `docs/adr/` for whatever subsystem you're
   touching (see the index in `DECISIONS.md` and the ADR list below).
7. **`docs/architecture/HOLLOWMERE_ENGINE.md`**, **`ENGINE_STRUCTURE.md`**,
   and **`ENGINE_MODIFICATIONS.md`** if you're touching anything under
   `Assets/Scripts/ClassicUO/src/`.
8. **`docs/architecture/HOLLOWMERE_UI_RENDERING_STRATEGY.md`** if you're
   building or changing any UI.

Then, before coding specifically: **`CONTRIBUTING_AI.md`**.

## How architectural decisions get made

Hollowmere has eight ADRs in `docs/adr/` (`ADR-001` through `ADR-008`) covering
the engine fork philosophy, the interaction layer, the action pipeline, Action
Bars, the diagnostics framework, the engine override policy, mobile UX philosophy,
and the HUD Widget System. A ninth, pre-existing ADR (`docs/adr/0001-hollowmere-ui-theme-
system.md`, numbered differently — see the repository recommendations in this
milestone's report) covers the UI theme system.

**The rule: never silently change architecture.** Before making an architectural
choice — not an implementation detail, an architectural one — search `docs/adr/`
for an existing ADR. If one exists, follow it. If none exists and you're about to
make a decision an ADR should have covered, propose one instead of deciding
ad hoc. See `CONTRIBUTING_AI.md` for the exact procedure.

## Source of truth files

| Question | Authoritative file |
|---|---|
| Why does Hollowmere exist, what should it become | `HOLLOWMERE_PRODUCT_VISION.md` |
| What's true right now, what's shipped, what's debt | `PROJECT_STATE.md` |
| What's currently decided (short form) | `DECISIONS.md` |
| Why a specific architectural choice was made | `docs/adr/ADR-*.md` |
| Overall engine/gameplay/presentation architecture | `docs/architecture/HOLLOWMERE_ENGINE.md` |
| Where new code belongs | `docs/architecture/ENGINE_STRUCTURE.md` |
| What's modified vs. upstream ClassicUO, and why | `docs/architecture/ENGINE_MODIFICATIONS.md` |
| Gump vs. Unity UI technology choice | `docs/architecture/HOLLOWMERE_UI_RENDERING_STRATEGY.md` |
| Concrete mobile UI rules | `MOBILE_DESIGN_GUIDELINES.md` |
| Long-term themes (not a backlog) | `ROADMAP_VISION.md` |
| What to evaluate next (not decisions) | `ROADMAP_NEXT.md` |
| Candid architectural self-critique | `docs/architecture/ARCHITECTURE_REVIEW.md` |

If two documents appear to conflict, `PROJECT_STATE.md` and `DECISIONS.md` win —
they're the living snapshot and are expected to be kept current; everything else
is either a point-in-time record (an ADR, a review) or a longer-form document
that changes less often. Two older, pre-`v0.2.0-mobile-foundation` documents —
`HOLLOWMERE_MOBILE_MODERNIZATION_VISION.md` and `SESSION_CONTEXT.md` — predate
this documentation set and should be treated as historical context, not current
direction, until formally reconciled (see this milestone's repository
recommendations).

## What should never be changed without explicit approval

From the project root `CLAUDE.md` (still binding, not superseded by anything
written this milestone):

- **Protected systems**: `ClientRunner` movement logic, Networking, Packet
  serialization, Asset loading pipeline.
- **Stable systems**: Mobile Input V2, `FloatingJoystick`, LeanTouch
  integration, `ClientRunner` movement, Raw touch input, Networking, Mobile
  Settings.

Full list and reasoning: `DECISIONS.md`'s Protected Systems section, and
`CLAUDE.md` itself.

Also never change silently: anything with an existing ADR (propose a new one
instead), and the sandbox-identity principles in `HOLLOWMERE_PRODUCT_VISION.md`'s
"What should NEVER change" section.

## How new milestones should be developed

Full process: **`docs/development/DEVELOPMENT_PROCESS.md`**. Short version: research the
relevant architecture before touching code, get the approach approved before
implementing, validate at runtime (not just "it compiles"), update documentation
in the same milestone as the code that invalidates it, keep commits scoped and
audited, and update an ADR if the milestone changed architecture rather than
just extended it.

Before starting implementation work, also read **`docs/development/REVIEW_CHECKLIST.md`** — it's
what the milestone will be checked against before it's considered done.

For a concise continuation snapshot, read **`AI_HANDOFF.md`** after
`PROJECT_STATE.md`. It never overrides the authoritative documents above.
