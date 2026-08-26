# ADR-006: Engine Override Policy

## Context

Following ADR-001 (Hollowmere is a maintained fork, not a thin wrapper), the
project needed an actual mechanism and set of rules for how an engine file moves
from "unmodified upstream symlink" to "tracked, intentional Hollowmere override" —
and for making that transition impossible to skip accidentally.

Two concrete failures motivated this: a `.gitignore` blanket rule
(`Assets/Scripts/ClassicUO/src/*`) silently hid 64 genuinely modified files from git
for an unknown period, relying entirely on someone remembering to `git add -f`; and
a nested nested `.gitignore` (a leftover generic Visual-Studio template) was
separately blocking every `.meta` file in the same tree, which would have quietly
broken Unity GUID stability across a fresh clone if left unnoticed.

## Decision

1. **Never edit a file while it's still a symlink.** Doing so writes through into
   `external/ClassicUO` (the upstream submodule) instead of Hollowmere's own tree.
   Convert first: remove the symlink, copy the real content, *then* edit.
2. **A converted file is tracked in git immediately**, via an explicit `.gitignore`
   negation (`!path/to/file.cs` + `!path/to/file.cs.meta`), not left to accumulate
   as an untracked "I'll add it later." The negation is the actual mechanism that
   makes `git add` work normally afterward — no standing reliance on `-f`.
3. **Every tracked override is documented** in
   `docs/architecture/ENGINE_MODIFICATIONS.md`: purpose, why it differs from
   upstream, whether it's mobile-specific, whether it's upstream-worthy.
4. **A commit that touches an override file states, in its own diff, exactly what
   changed and why** — engine changes get the same commit hygiene as gameplay
   changes, not a "trust me, it's just an override" pass.

## Alternatives considered

- **Track the entire `external/ClassicUO` mirror as regular files, no symlinks.**
  Rejected — this makes every file in the entire upstream client look identically
  "modified" in git's eyes, destroying the ability to tell an intentional override
  from an untouched file at a glance. The symlink *is* the signal.
- **A single blanket "trust the .gitignore, force-add when needed" policy** (the
  status quo before this ADR). Rejected — this is exactly what produced 64
  untracked files and a `.meta`-blocking nested gitignore going unnoticed. A policy
  that depends on someone remembering an exception every time isn't a policy.
- **Recursive `**` ignore that re-includes entire directories.** Considered and
  rejected in favor of file-level negation — re-including a directory wholesale
  would also silently un-ignore any *symlinks* that happen to live alongside real
  overrides in the same folder, defeating the "symlink means untouched" signal.

## Consequences

- Converting a file now has an unambiguous two-step ritual: convert to a real file,
  add its `.gitignore` negation (root file + nested `.meta`-blocking file if
  applicable) in the same change.
- `docs/architecture/ENGINE_MODIFICATIONS.md` is now a living document, not a
  point-in-time audit artifact — it needs updating whenever a new file crosses the
  symlink-to-override line.
- The blanket `.meta` ignore in the nested `Assets/Scripts/ClassicUO/.gitignore`
  (a leftover, largely-irrelevant Visual Studio template) still exists for anything
  *not* on the negation list — a deliberate, narrow fix rather than un-ignoring
  `.meta` wholesale, which would have re-tracked hundreds of still-symlinked files'
  metadata for no reason.

## Future implications

- The ~325 still-symlinked files' `.meta` tracking status remains an open question
  (see ROADMAP_VISION.md, Engine Improvements) — deliberately not resolved here,
  since it's a separate, larger decision about GUID stability across clones that
  deserves its own review rather than being bundled into this policy.
- If the override count keeps growing, consider whether the manual negation-list
  maintenance in `.gitignore` becomes unwieldy enough to warrant a generated or
  scripted approach — not needed at 73 files, worth watching past a few hundred.
