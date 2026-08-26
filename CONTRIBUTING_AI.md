# Contributing as an AI Coding Agent

This file is written for you regardless of which tool you are — Claude Code,
Cursor, ChatGPT, Gemini, Codex, or anything that comes after. It assumes no
conversation history. Everything you need is either in this file or one hop
away via a link.

## Before coding

Read, in order:

1. `AGENTS.md` — the repository constitution and reading map.
2. `PROJECT_STATE.md` — what's actually true right now. Trust this over your
   training data or any prior conversation's summary.
3. `DECISIONS.md` — short, declarative current rules. Check here before
   assuming something is undecided.
4. Whichever ADR(s) in `docs/adr/` cover the subsystem you're about to touch.
5. Whichever architecture document(s) are relevant — `docs/architecture/
   HOLLOWMERE_ENGINE.md`, `ENGINE_STRUCTURE.md`, `ENGINE_MODIFICATIONS.md`, or
   `HOLLOWMERE_UI_RENDERING_STRATEGY.md`, per `AGENTS.md`'s table.

Do not start implementing based on a partial read of one document. Architecture
decisions in this project are deliberately cross-referenced — `DECISIONS.md`
points to ADRs, ADRs point to architecture docs, architecture docs point back
to the product vision. Follow the links.

## Before changing architecture

**Search for an ADR first.**

- **If one exists that covers what you're about to do:** follow it. If you
  think it's wrong, say so explicitly and propose a change — do not quietly
  deviate from a documented decision while leaving the ADR looking still
  current.
- **If none exists:** you are about to make an architectural decision that
  isn't written down anywhere. Stop before implementing. Propose one — state
  the context, the decision you're about to make, the alternatives you
  considered, and the consequences — the same shape as the existing `ADR-001`
  through `ADR-007`. Get it confirmed before writing the code that depends on
  it.

**Never silently change architecture.** This applies even to changes that seem
small in isolation — a new rendering technology choice for one UI element, a
new persistence mechanism, a new gesture, a new engine override. If it's the
kind of thing an ADR in this repository already covers a similar case for,
treat a deviation as an architectural change, not an implementation detail.

## What counts as "architecture" vs. "implementation detail"

Architecture: which rendering technology a new UI surface uses, how a new kind
of player action is represented, whether a new engine file gets tracked as an
override, what a new persistence mechanism looks like, any new gesture or
input pattern, any new cross-layer (Engine/Gameplay/Presentation) dependency.

Implementation detail: the specific fields on a class, internal method
structure, variable naming, which existing pattern you apply to a new case
that clearly fits an existing rule.

If you're unsure which one you're looking at, treat it as architecture and
check for an ADR.

## Working inside the engine fork

Never edit a file under `Assets/Scripts/ClassicUO/src/` while it's still a
symlink — check first (`Is this a symlink or a real file?`), convert if
needed (remove symlink, copy content, then edit), and add an entry to
`docs/architecture/ENGINE_MODIFICATIONS.md` for anything newly tracked. Full
policy: `ADR-006`.

## Validation expectations

Follow `docs/development/DEVELOPMENT_PROCESS.md`'s validation section. Minimum bar: compiles
clean, console clean, and — for anything touching input/UI/networking —
verified at runtime, not just reviewed as source. Read tool results back;
don't trust a "success" message at face value.

## Diagnostics

If your change touches Input, UI, `ItemHold`, Actions, or Networking, use
`Hollowmere.Diagnostics.Diag` rather than `Debug.Log`. See `ADR-005` and
`DECISIONS.md`.

## Documentation

Update `PROJECT_STATE.md` and any relevant ADR/architecture doc as part of the
same change, not a follow-up. See `docs/development/DEVELOPMENT_PROCESS.md`'s documentation
section.

## Commit hygiene

Scope commits to one concern. Verify staged content matches stated intent
before committing — this project has a concrete, documented case of a
mis-scoped commit that had to be corrected after the fact (see
`PROJECT_STATE.md`'s known technical debt and `docs/development/DEVELOPMENT_PROCESS.md`'s
commit hygiene section). Don't repeat it.

## Before calling a milestone done

Run through `docs/development/REVIEW_CHECKLIST.md`. If any item is unchecked, the milestone
isn't finished, regardless of whether the code works.

## If you're unsure

Stop and ask, the same way this project's own development sessions have —
explaining exactly what's ambiguous, what the options are, and why you can't
resolve it from the existing documentation — rather than guessing and
proceeding. Guessing on an architectural question is the one thing this whole
document set exists to prevent.
