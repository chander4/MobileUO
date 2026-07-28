---
name: codedebrief
description: Use when answering codebase logic, behavior, workflow/flusso, decision, state/status, changed-code context, testing, or visual workflow/canvas questions in a project that uses CodeDebrief. Prefer the CodeDebrief MCP agent_context tool before broad searches, and use snapshot_slice, the canonical workflow_slice Mermaid visual, or viewer_targets when the user asks to show, visualize, render, diagram, canvas, workflow, flusso, or workflow_slice.
---

# CodeDebrief

Use CodeDebrief as the first path for code-logic questions in projects with CodeDebrief
configured.

## Default Workflow

1. Call MCP `agent_context` before broad file-by-file search. Pass the user question plus
   changed files, current file, selected code, flow id, symbol, or dependency path when
   available.
2. Inspect `workflow_slice`. Answer from deterministic fields: presentation,
   primary/supporting flows, ordered steps, decisions, source ranges, calls, and visuals.
3. Use `expand_slice`, `workflow_path`, `explain_flow`, `explain_node`, or `explain_edge`
   only when the first slice is too narrow.
4. Treat CodeDebrief artifacts as part of done for workflow-relevant changes. After each
   meaningful source, route, config, or agent-instruction change, refresh the graph before
   finalizing: run MCP `update_codedebrief` when available, otherwise run
   `codedebrief update`, then run `validate_artifacts` or
   `codedebrief validate --check-sync`; this checks both current-source JSON sync and
   whether `codedebrief-out/codedebrief.md` was rendered from that JSON. Keep
   `codedebrief-out/codedebrief.json`, `codedebrief-out/codedebrief.md`, and
   `codedebrief-out/codedebrief.hash.json` synchronized when they change.

## Visual Workflow Requests

When the user asks to show a workflow, workflow_slice, diagram, visual flow, canvas,
flusso, or similar code path:

1. Call `agent_context` with `include_visual=true` when available. Use a stable token
   budget for similar requests, but inspect the full returned `workflow_slice` before
   deciding what to show.
2. If the tool result is too large, saved externally, truncated, or missing the exact
   `workflow_slice.presentation.canonical_visual.diagram`, retry with a smaller
   `token_budget` and a narrower `flow_id`, `symbol`, `current_file`, or `scope`. Do not
   recover by listing flows and hand-building a diagram.
3. If the first slice omits relevant callers, callees, branches, adjacent flows, or paths,
   use the returned slice handles with `expand_slice` or `workflow_path` before answering.
4. Choose the first visible depth yourself: show the clearest useful subset of the
   selected workflow, not every low-signal implementation node, but do not remove facts
   needed to understand the logical path.
5. Render `workflow_slice.presentation.canonical_visual.diagram` exactly as the default
   chat visual only when the client renders Mermaid blocks inline. It is the canonical
   top-to-bottom Mermaid graph and should be preferred over SVG snapshots for repeated
   chat answers.
6. Call `snapshot_slice` using `workflow_slice.id`, `workflow_slice.handle.flow_ids`, and
   any `workflow_slice` handles returned by CodeDebrief to persist local artifacts. In
   clients that cannot render Mermaid inline, or when Mermaid would appear as a raw code
   block, call `snapshot_slice` with `include_svg=false` and provide
   `artifact.mermaid_path`, `artifact.mermaid_markdown_path`, or
   `artifact.mermaid_open_command` as the visual result before prose. Do not paste a long
   Mermaid code block as the primary visual unless the user explicitly asks for raw or
   copyable Mermaid.
7. Do not render `snapshot.svg` inline by default. SVG/HTML snapshot artifacts are for
   explicit SVG requests or local inspection only, because their layout may differ from
   Mermaid and can overlap text in some clients. Keep the returned `diagram_hash` visible
   when useful. Do not synthesize a new Mermaid diagram and do not add limits, error
   codes, branches, or service steps that are absent from the `workflow_slice` payload.
   CodeDebrief Mermaid visuals are vertical/top-to-bottom by default. Use a horizontal
   layout only when the user explicitly asks for a compact horizontal overview.
8. Do not read source files to rebuild, relabel, or extend the diagram. Source reads are
   allowed only as follow-up explanation after the deterministic CodeDebrief visual is
   shown, and they must not change the displayed nodes, edges, labels, or branches.
9. If neither exact canonical Mermaid nor a returned Mermaid artifact can be used, say
   that the deterministic visual cannot be shown in this client and provide `viewer_targets`;
   never create a replacement Mermaid diagram from prose or source reads.
10. After the visual, include a short "High-level flow" section in the user's language.
   Derive it only from `ordered_steps`, primary/supporting flows, decisions,
   domain logic, source ranges, and focused follow-up payloads. Keep it to a compact
   happy-path walkthrough with only the branches needed by the question.
11. Say that the displayed diagram is a bounded summary of the selected logic and can be
   expanded.
12. If the user asks for a more language-friendly version, rewrite both the technical
   block labels and the high-level written flow in simple wording using the language of
   the user's request. Present that as a human-friendly translation derived only from
   returned node, edge, decision, step, and source fields.
13. End with concise follow-up choices in the user's language: simplify the labels and
   written flow into language-friendly wording, expand omitted nodes/branches/adjacent
   flows, or explore a related area or deeper path.
14. Also provide the `viewer_targets` command and hash
   target so the user can open the same visual in `codedebrief view`.
15. Treat `workflow_slice.presentation` as supporting context for this request, not as the
   primary output.
16. Keep the written flow short and secondary to the deterministic visual. Do not answer
   with raw JSON or YAML unless
   the user explicitly asks for it.

## Guardrails

- MCP is local-first and deterministic; do not ask for provider keys for the primary
  workflow.
- Treat language-friendly labels and high-level written flows as presentation layers
  derived from deterministic workflow facts.
- Use CodeDebrief to explain modeled code logic, not to present possible defects.
- Use `codedebrief view` only for the human manual UI.
