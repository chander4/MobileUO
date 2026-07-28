# Hollowmere AI Development Guide

This document is the authoritative guide for AI agents working on Hollowmere.

If documentation conflicts with assumptions, trust the documentation.
If runtime state conflicts with documentation, trust runtime state.

---

# Project Goal

Hollowmere is a modern Ultima Online client built on Unity.

Goals:

- Mobile-first UX
- Modern architecture
- Clean, maintainable code
- AI-assisted development
- Zero unnecessary technical debt

The project values long-term maintainability over short-term hacks.

---

# Hollowmere Design Manifesto

Every UI decision should satisfy these principles:

1. Preserve the soul of Ultima Online.
2. Modernize interactions, not gameplay.
3. Never obscure the game world unnecessarily.
4. Touch-first, desktop-quality.
5. Consistency over novelty.
6. Every animation communicates state.
7. Performance before visual effects.
8. Accessibility is a feature, not an afterthought.

# Development Workflow

Before modifying any code:

1. Query OpenDeepWiki for the subsystem.
2. Read the relevant architecture documentation.
3. Inspect the live Unity scene using Unity MCP.
4. Summarize:
   - Current architecture
   - Files that will change
   - Potential regressions
5. Only then begin implementation.

Do not rediscover architecture manually if OpenDeepWiki already contains it.

---

# Definition of Done

A task is NOT complete because it compiles.

Every feature must pass:

- Compile validation
- Scene validation
- Runtime validation
- Gameplay validation
- Device validation (when applicable)

---

# Unity Validation Checklist

Before completing any Unity task verify:

✓ No Console errors

✓ No Missing Script components

✓ No MissingReferenceException

✓ No NullReferenceException

✓ No missing serialized references

✓ Required GameObjects enabled

✓ Scene hierarchy intact

✓ Gameplay verified manually

Never trust tool success messages.

Always inspect Unity state directly.

---

# Root Cause Policy

Never work around bugs.

Always:

1. Find the root cause.
2. Fix the root cause.
3. Add validation to prevent regression.

Do not disable systems to hide errors.

Do not leave TODOs instead of fixes.

---

# Stable Systems

These systems are considered production-stable.

Do not refactor without approval.

- Mobile Input V2
- FloatingJoystick
- LeanTouch integration
- ClientRunner movement
- Raw touch input
- Networking
- Mobile Settings

---

# Protected Systems

Require explicit approval before modification.

- ClientRunner movement logic
- Networking
- Packet serialization
- Asset loading pipeline

---

# Existing Architecture

Movement

Touch
↓

LeanTouch
↓

MobileInputController
↓

FloatingJoystick
↓

ClientRunner
↓

Movement

Networking is intentionally independent of the input system.

---

# Project Standards

Prefer:

- Small incremental commits
- Backwards-compatible changes
- Existing architecture
- Reuse over replacement
- Runtime validation
- Self-healing systems where appropriate

Avoid:

- Large rewrites
- Duplicate systems
- Parallel architectures
- Manual Inspector setup when automation is practical

---

# Verification Requirements

Every completed task must include:

1. Files modified
2. Root cause (if fixing bugs)
3. Validation performed
4. Remaining risks
5. Suggested follow-up work

Do not report success without evidence.

---

# Current Milestone

Modern Server Browser

Current focus:

- Favorites
- Search
- Default Server
- Validation

Do not redesign the login scene without approval.

---

# Last Stable Commit

1cac726

---

# AI Development Philosophy

Optimize for:

- Reliability
- Maintainability
- Testability
- Small reviewable changes

Never optimize solely for fewer lines of code.

Code that is easy to understand is preferred over clever code.

When uncertain:

Stop.

Summarize options.

Ask for direction instead of guessing.

<!-- codedebrief:instructions:start -->
## CodeDebrief

This project uses CodeDebrief to keep decision flows synchronized with the source code.

For codebase questions about behavior, decisions, workflow structure, or changed-code context:

1. Prefer the CodeDebrief MCP `agent_context` tool before broad file-by-file searches.
2. Use `agent_context` for substantial changes, passing changed files, selected code,
   current file, flow id, symbol, or dependency path when available; inspect
   its returned `workflow_slice` before answering.
3. When the user asks to show a workflow, flusso, visual flow, canvas, or
   `workflow_slice`, prefer the canonical Mermaid visual: render
   `workflow_slice.presentation.canonical_visual.diagram` exactly as returned only when
   the client renders Mermaid inline. If the client cannot render Mermaid inline, or if
   Mermaid would appear as a raw code block, call `snapshot_slice` with
   `include_svg=false` and provide `artifact.mermaid_path`,
   `artifact.mermaid_markdown_path`, or `artifact.mermaid_open_command` as the visual
   result before prose. Do not paste a long Mermaid code block as the primary visual
   unless the user explicitly asks for raw or copyable Mermaid. Do not render
   `snapshot.svg` inline by default; SVG artifacts are for explicit SVG requests or local
   inspection because their layout can differ from Mermaid.
   Keep CodeDebrief visuals vertical/top-to-bottom by default. Use a horizontal layout
   only when the user explicitly asks for a compact horizontal overview.
   Inspect the full returned `workflow_slice` before deciding what to show. Choose the
   first visible depth yourself: show the clearest useful subset, then say that the
   displayed diagram is a bounded summary and can be expanded.
   After the visual, include a short high-level written flow in the user's language,
   derived only from returned ordered steps, primary/supporting flows, decisions, domain
   logic, source ranges, and focused follow-up payloads. Keep it compact and explain the
   happy path first, adding only the branches needed by the request.
   If the CodeDebrief result is too large, saved externally, truncated, or missing the exact
   canonical visual, retry with a smaller `token_budget` and narrower `flow_id`, `symbol`,
   `current_file`, or `scope`; do not recover by listing flows and hand-building a
   diagram.
   Do not synthesize a new Mermaid diagram and do not add limits, error codes, branches,
   or service steps that are absent from the `workflow_slice` payload. Do not read source
   files to rebuild, relabel, or extend the diagram; source reads are only follow-up
   explanation after the deterministic visual is shown and must not change displayed
   nodes, edges, labels, or branches. If neither exact canonical Mermaid nor a returned
   Mermaid artifact can be used, say so and provide `viewer_targets` instead of creating
   a replacement Mermaid diagram. If the user asks for a more language-friendly version,
   rewrite the technical block labels and the high-level written flow in simple wording
   using the language of the user's request. This is allowed only as a separate
   presentation layer derived from returned node, edge, decision, step, and source fields.
   End visual answers with concise options in the user's language: simplify labels and
   written flow, expand omitted nodes/branches/adjacent flows, or explore a related area.
   Show raw JSON or YAML only when explicitly requested.
4. Use `expand_slice`, `workflow_path`, `snapshot_slice`, `explain_flow`, `explain_node`,
   or `explain_edge` only when the first slice needs more precise context.
5. Use `codedebrief view ...` only when a human wants the manual UI flowchart.

When helping a user set up or learn CodeDebrief:

1. Start with `codedebrief --help`, then use `codedebrief <command> --help` for the specific
   command you plan to run or recommend.
2. Use `codedebrief doctor` when install, dependency, or parser capability issues are
   unclear.
3. Do not ask for LLM provider keys for the primary workflow. Language-friendly labels and
   high-level written flows are presentation layers derived from deterministic workflow
   facts.
4. `codedebrief setup <target>` updates only that target's files. Run the command
   separately for each agent surface you want to configure, preserving any target-specific
   frontmatter and local notes.

After code or workflow-relevant changes:

1. Treat CodeDebrief artifacts as part of done. After every meaningful source, route,
   config, or agent-instruction change, run `codedebrief update` before finalizing or
   committing so MCP answers and `codedebrief view` use current graphs. Skip only changes
   that cannot affect the modeled code logic, such as unrelated copy edits or images.
2. Use `codedebrief update --full` after analyzer upgrades, parser/dependency changes,
   large refactors, or when cached file models should be ignored.
3. Run `codedebrief validate --check-sync`; this checks both current-source JSON sync and
   whether `codedebrief-out/codedebrief.md` was rendered from that JSON.
4. Commit synchronized changes to:
   - `codedebrief-out/codedebrief.json`
   - `codedebrief-out/codedebrief.md`
   - `codedebrief-out/codedebrief.hash.json`
5. Use CodeDebrief MCP `agent_context` to inspect affected entry points and callers when
   explaining or reviewing the change.
6. Ground the explanation in the returned `workflow_slice`; expand it through MCP only
   when the initial slice omits relevant callers, callees, domain states, or paths.

For viewer/UI changes:

1. Run `npm run viewer:typecheck`, `npm run viewer:test`, and `npm run viewer:build`.
2. Regenerate HTML artifacts with `codedebrief update` and
   `codedebrief view --render-only --no-open`.
3. Check the generated viewer with a cache-buster URL.

<!-- codedebrief:local-notes:start -->
<!-- Add project-specific local notes here. This section is preserved by `codedebrief setup`. -->
<!-- codedebrief:local-notes:end -->

CodeDebrief is a comprehension and navigation tool for source-grounded workflows. Use it
to explain modeled logic, not to present possible defects.
<!-- codedebrief:instructions:end -->
