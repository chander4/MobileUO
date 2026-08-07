# Hollowmere Engine — Architecture Baseline

**Status:** baseline established (this document), engine surface not yet fully
reorganized on disk. See [ENGINE_MODIFICATIONS.md](ENGINE_MODIFICATIONS.md) for the
full file inventory and [ENGINE_STRUCTURE.md](ENGINE_STRUCTURE.md) for where new code
belongs going forward.

## What Hollowmere actually is

Hollowmere was scoped, early on, as "a thin Unity wrapper around ClassicUO" — pull in
the upstream client via symlinks, override the handful of files that need touch/mobile
adaptation, and build gameplay on top. That description stopped being accurate some
time ago and nobody wrote down when.

A full audit (2026-08-06) found **73 files** under `Assets/Scripts/ClassicUO/src/`
that are real, on-disk, intentionally-modified copies of upstream ClassicUO source —
not symlinks. Every one of them differs from `external/ClassicUO` (zero accidental
identical copies). They span rendering, input dispatch, networking/packet handling,
asset loading, UI controls and Gumps, and low-level utility code. Only ~10 of them were
ever documented as "known overrides"; the rest accumulated without anyone updating that
list.

That's not a thin wrapper. That's a maintained engine fork. This document names that
reality so the next few years of development can be built on an honest foundation
instead of a "just a few overrides" mental model that stopped matching reality a while
ago.

## Layered architecture

```
                    ┌─────────────────────┐
                    │      ClassicUO       │   external/ClassicUO (git submodule)
                    │   (upstream, vendor)  │   Untouched. Symlinked into Assets/ for
                    └──────────┬───────────┘   files Hollowmere does not modify.
                               │
                               ▼
                    ┌─────────────────────────────────────────┐
                    │           Hollowmere Engine               │
                    │  Assets/Scripts/ClassicUO/src/* (real files) │
                    │                                             │
                    │   Rendering        Input                    │
                    │   Networking       Asset Loading             │
                    │   Mobile Runtime   UI Runtime                │
                    └──────────────────────┬──────────────────────┘
                                            │
                                            ▼
                    ┌─────────────────────────────────────────┐
                    │             Gameplay Layer                │
                    │      Assets/Scripts/Hollowmere/*           │
                    │  (Interaction, Diagnostics, UI theme, ...)  │
                    └──────────────────────┬──────────────────────┘
                                            │
                                            ▼
                    ┌─────────────────────────────────────────┐
                    │           Presentation Layer               │
                    │   Unity Scenes, Prefabs, HollowmereUI       │
                    │   (Theme/Typography/Button/Panel), Gumps    │
                    │   rendered via ClassicUO's own draw path     │
                    └─────────────────────────────────────────┘
```

### Relationship to ClassicUO (`external/ClassicUO`)

The submodule is vendor code — pinned upstream, never edited directly. Every `.cs` file
under `Assets/Scripts/ClassicUO/src/` starts life as a **symlink** into the submodule.
Unity compiles through the symlink as if the file were local; as long as a file stays a
symlink, Hollowmere is running unmodified upstream code for that file.

The moment a file needs a real change (touch input instead of mouse, an iOS filesystem
quirk, a Unity-specific rendering bridge, a genuine engine bug fix), the workflow is:
remove the symlink, copy the real content in, edit it. From that point it's a
first-class Hollowmere Engine file, not a "local override" — see
[ENGINE_STRUCTURE.md](ENGINE_STRUCTURE.md) for the naming and tracking discipline that
implies.

### Relationship to Unity

Unity is the host runtime and rendering surface, not a passive container. ClassicUO's
own XNA/MonoGame-style rendering, input, and file-loading assumptions get bridged to
Unity's equivalents inside the Engine layer:
`GameController.cs`/`Batcher2D.cs`/`UOTexture.cs` translate XNA drawing calls onto
Unity's texture/graphics pipeline; `LeanTouch`-driven finger input gets translated into
ClassicUO's `Mouse`/`SDL`-shaped input model; `UOFileManager.cs` compensates for
platform filesystem differences (documented inline: the iOS case-sensitivity fix).
Gameplay and Presentation code never talk to Unity's low-level rendering/input APIs
directly for anything ClassicUO already owns — they go through the Engine layer.

### Relationship to Hollowmere gameplay code

`Assets/Scripts/Hollowmere/*` (currently: `Interaction/`, `Diagnostics/`, plus the
existing UI theme system) is gameplay/product code, not engine code. It consumes the
Engine layer's public surface (`GameActions`, `SelectedObject`, `UIManager`,
`AnchorManager`, `ItemHold`, ...) the same way any ClassicUO Gump would, and adds
Hollowmere-specific behavior on top (the Primary Action button, the diagnostics
framework, the Action Bar). Gameplay code should never need to modify an Engine-layer
file to add a feature — if it does, that's a signal the Engine layer is missing a hook,
not that the feature belongs in `ClassicUO/src/`.

### Presentation

Unity scenes, prefabs, and the `HollowmereUI` theme/typography/button/panel system sit
on top of Gameplay. ClassicUO's own Gump rendering (drawn via the Engine layer's
`Batcher2D`) and Hollowmere's native Unity UI (Canvas-based joystick, chat popup, action
bars) currently coexist as two separate rendering surfaces sharing the same screen —
documented as-is here, not something this milestone changes.

## What this milestone does and does not change

This is a documentation and repository-hygiene milestone. It does not touch gameplay,
networking, rendering, or input behavior. See the final report for the complete list of
files created/modified and the validation performed.
