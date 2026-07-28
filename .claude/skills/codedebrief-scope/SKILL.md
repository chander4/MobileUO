---
name: codedebrief-scope
description: Use before running `codedebrief update`, `codedebrief setup`, or otherwise refreshing CodeDebrief in this MobileUO2 repo. The full codebase (a complete ClassicUO client port) is too large for CodeDebrief to analyze in one pass and fails with MemoryError, so `source_roots` must be scoped to just the area under investigation. Trigger on "update codedebrief", "refresh codedebrief for X", "run codedebrief on Y", "widen codedebrief coverage", or when the `agent_context`/`workflow_slice` results are missing a file or symbol you need.
---

# CodeDebrief scoping for MobileUO2

This repo is a full ClassicUO client port (hundreds of thousands of lines across
`external/ClassicUO` and `Assets/Scripts/ClassicUO`). Running `codedebrief update` against
the whole repo (`source_roots = ["."]`) reliably fails with `MemoryError` while serializing
the model. Always scope `source_roots` to the specific files or directories relevant to the
current task instead of analyzing everything at once.

## Where the config lives

`codedebrief-out/codedebrief.toml`, `[codedebrief]` section, `source_roots` key. Paths are
relative to the repo root.

## Workflow

1. Open `codedebrief-out/codedebrief.toml`.
2. Set `source_roots` to just what the current task needs — see "Known-good scopes" below
   for a starting point, or list the specific files/directories the question touches.
3. Run `codedebrief update`.
4. Run `codedebrief validate` (or MCP `validate_artifacts`) to confirm the artifacts are in
   sync.
5. If it still fails with `MemoryError`, narrow `source_roots` further — prefer listing
   individual files over whole directories for anything under `external/ClassicUO` or
   `Assets/Scripts/ClassicUO`, since those directories alone are too large.
6. Once artifacts are refreshed, use the `codedebrief` skill's normal workflow
   (`agent_context`, `workflow_slice`, etc.) to answer the question.

## Known-good scopes

**Our own code** (small enough to run whole):
```toml
source_roots = ["Assets/Scripts/Hollowmere"]
```

**ClassicUO integration points** our Hollowmere code actually touches (list files
individually — do not scope to the whole `external/ClassicUO` directory):
```toml
source_roots = [
  "external/ClassicUO/src/Game/Managers/UIManager.cs",
  "external/ClassicUO/src/Game/Managers/AnchorManager.cs",
  "external/ClassicUO/src/Game/Managers/TargetManager.cs",
  "external/ClassicUO/src/Game/Managers/DelayedObjectClickManager.cs",
  "external/ClassicUO/src/Game/UI/Gumps/AnchorableGump.cs",
  "external/ClassicUO/src/Game/GameActions.cs",
  "external/ClassicUO/src/Game/SelectedObject.cs",
  "external/ClassicUO/src/GameController.cs",
  "external/ClassicUO/src/Client.cs"
]
```

Extend this list with more individual files as new integration points come up, rather than
widening to a whole directory.

## Guardrails

- Never set `source_roots` back to `["."]` or to a bare top-level directory like
  `external/ClassicUO` or `Assets/Scripts/ClassicUO` — both are known to exceed memory.
- Prefer the smallest scope that answers the current question. Re-scope again for the next
  question rather than trying to keep accumulating one giant scope.
- After changing scope and running `codedebrief update`, the previous scope's flows are
  replaced, not merged — `codedebrief.md`/`codedebrief.json` only reflect the current
  `source_roots`. If you need both areas covered simultaneously, list all the needed
  files/directories together in one `source_roots` array rather than running twice.
