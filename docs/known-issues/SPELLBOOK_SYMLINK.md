# SpellbookGump "symlink/index anomaly" — investigated, not an issue

**Status: closed as non-issue.** This document exists because a prior audit pass
flagged a suspected problem and the milestone that produced this repo audit asked for
it to be investigated and documented either way. The investigation found nothing wrong.

## The original claim

During the repository audit that preceded this milestone, a git-porcelain check showed
`Assets/Scripts/ClassicUO/src/Game/UI/Gumps/SpellbookGump.cs` tracked in git's index at
mode `100644` (a regular file). That observation was correct. From it, the audit
*inferred* — without actually checking the working-tree file — that the file must
currently be a symlink on disk (since most files under `Assets/Scripts/ClassicUO/src/`
are symlinks into `external/ClassicUO`), and that git's tracked content and the disk
content must therefore have silently diverged. That inference was never verified and
turned out to be wrong.

## What's actually true

Checked directly:

```
$ ls -la Assets/Scripts/ClassicUO/src/Game/UI/Gumps/SpellbookGump.cs
-rw-r--r-- 1 chase 197609 43534 ... SpellbookGump.cs      # regular file, not a symlink

$ git show HEAD:.../SpellbookGump.cs | diff - Assets/Scripts/.../SpellbookGump.cs
(no output — identical)
```

`SpellbookGump.cs` is a regular file on disk, its content is byte-identical to what git
has tracked since it was converted from a symlink and force-added, and it diverges from
`external/ClassicUO`'s version by ~2,486 diff lines (substantial, intentional
touch-adapted spellbook UI work). Nothing is stale, nothing is mismatched. It's in
exactly the state every other already-tracked override file (`GameController.cs`,
`ScrollArea.cs`, `OptionsGump.cs`, `ShopGump.cs`, `SkillGumpAdvanced.cs`,
`WorldMapGump.cs`) is in.

## Root cause of the false alarm

The earlier audit read `git ls-files -s` output, saw mode `100644`, and treated that as
suspicious relative to a mental model of "most files here are symlinks." Mode `100644`
for an already-converted, already-tracked override file is exactly what's expected — it
was pattern-matched against the wrong baseline instead of being checked directly.

## Takeaway

No fix needed. Recorded here per the milestone's request for transparency, and as a
reminder to verify file-type claims (`ls -la` / `readlink`, not inference) before
treating them as a repository defect in future audits.
