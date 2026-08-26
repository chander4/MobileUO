# Hollowmere Development Process

This is the expected lifecycle for every milestone, whether human- or agent-led. It governs delivery discipline; a milestone's own scope belongs in its issue document.

```text
Research → Architecture → Approval → Implementation → Validation → Documentation → Commit → Update PROJECT_STATE → Update AI_HANDOFF → Update ADR (if architecture changed)
```

## Research

Read `AGENTS.md`, `PROJECT_STATE.md`, `DECISIONS.md`, relevant ADRs, and relevant architecture documents before proposing code. Use documented, scoped repository knowledge where available; do not infer architecture from stale conversation history.

## Architecture and approval

State the intended change, its owning layer, and rendering ownership for UI work. `ENGINE_STRUCTURE.md` defines placement and `HOLLOWMERE_UI_RENDERING_STRATEGY.md` defines UI ownership. Search ADRs before creating a new pattern. Obtain approval before significant implementation, especially for Protected systems, Stable-system refactors, and new or changed architecture. See `DECISIONS.md`, `CLAUDE.md`, and `CONTRIBUTING_AI.md`.

## Implementation

Implement the smallest approved change. Reuse established primitives rather than creating parallel systems. Engine work follows ADR-006: never edit an upstream symlink in place, and document each new tracked override.

## Validation

Compilation alone is insufficient. Validate runtime behavior proportionately: check Unity console and scene health, and use device/runtime validation for input, UI, networking, or gameplay-facing changes. Prefer deterministic runtime checks for recovery or timing-sensitive paths. Follow `TESTING_STRATEGY.md` and inspect tool output rather than trusting a success label.

## Documentation and commit

Update documents invalidated by the change in the same milestone: architecture material, engine inventory, relevant ADRs, and issue scope as appropriate. Review the staged diff, not merely its file list; each commit has one coherent purpose and accurately describes its contents.

## Living snapshots and ADRs

After the outcome is known, update `PROJECT_STATE.md` and `AI_HANDOFF.md` with release/branch state, completed scope, validation, debt, blockers, and next expected decision. If those updates require a follow-up documentation commit, keep it scoped and state why. If the milestone introduced, changed, or superseded an architectural decision, create or update its ADR before completion. Never make an architectural change silently.
