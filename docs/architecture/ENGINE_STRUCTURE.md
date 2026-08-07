# Hollowmere Engine Structure — where new code belongs

Read this before adding a new file. If you're not sure which layer something belongs
in, find its concern below.

## Layer ownership

| Concern | Owner | Location | Notes |
|---|---|---|---|
| **Rendering** | Hollowmere Engine | `Assets/Scripts/ClassicUO/src/Renderer/*` | XNA→Unity draw-call bridging (`Batcher2D`, `UOTexture`, `Texture2DCache`, `XBREffect`). Never bypass this from Gameplay/Presentation to draw ClassicUO-owned content. |
| **Input (raw)** | Hollowmere Engine | `Assets/Scripts/ClassicUO/src/GameController.cs`, `Game/Scenes/GameSceneInputHandler.cs` | Touch→mouse translation (LeanTouch fingers → `Mouse`/`SDL`-shaped state), the `MouseUpdate`/`SimulateMouse`/`UnityInputUpdate` dispatch chain. This is where a touch becomes a `OnLeftMouseButtonDown()` call. |
| **Interaction (gameplay-facing input)** | Gameplay | `Assets/Scripts/Hollowmere/Interaction/*` | Once a raw click/tap has already been dispatched to a Gump/control by the Engine layer, everything about *what that means for the player* (context-sensitive actions, action bars, long-press) is Gameplay's job, not Engine's. |
| **Networking** | Hollowmere Engine | `Assets/Scripts/ClassicUO/src/Network/*` | Packet handlers, `GameActions`, `ItemHold`. Gameplay code calls into `GameActions`/reads `SelectedObject`/`ItemHold` — it never constructs or parses packets itself. |
| **Asset loading** | Hollowmere Engine | `Assets/Scripts/ClassicUO/src/IO/**` | Art/gump/sound/map/animation loaders, plus platform-path handling (the iOS case-sensitivity fix lives here). |
| **Mobile runtime** | Hollowmere Engine (bridging) + Gameplay (product features) | Engine: touch dispatch in `GameController.cs`. Gameplay: `Assets/Scripts/MobileInput/*`, `FloatingJoystick`, `ClientRunner.cs` | Movement input is a **Stable/Protected system** per the project root `CLAUDE.md` — do not refactor without approval regardless of which layer it looks like it belongs in. |
| **Gameplay** | Gameplay | `Assets/Scripts/Hollowmere/*` (excluding `Interaction`'s pure-dispatch pieces) | Feature logic: Primary Action, Action Bar, Diagnostics framework, catalog/resolver/factory patterns. Consumes the Engine layer's public surface; does not modify Engine files to add a feature. |
| **Presentation** | Presentation | Unity scenes/prefabs, `HollowmereUI` theme/typography/button/panel system | Visual composition on top of Gameplay. ClassicUO Gumps render through the Engine's `Batcher2D`; native Unity UI (joystick, chat popup, action bars) is a separate Canvas-based surface. Both currently coexist. |
| **UI (ClassicUO Gump system)** | Hollowmere Engine (the Gump/Control base classes and dispatch) + Gameplay (Hollowmere's own Gumps, e.g. `ActionButtonGump`) | Engine: `Game/UI/Controls/*`, `Game/UI/Gumps/*`, `Game/Managers/UIManager.cs`, `AnchorManager` (symlinked, unmodified). Gameplay: anything under `Assets/Scripts/Hollowmere/Interaction/` that subclasses a Gump/Control. | A new Gump/Control that's Hollowmere-authored (not a modified upstream one) belongs in Gameplay even though it participates in the Engine's Gump system. |
| **Utility** | Hollowmere Engine | `Assets/Scripts/ClassicUO/src/Utility/*` | Low-level helpers (`ZLib`, `UnsafeMemoryManager`, `Exstentions`, `QueuedPool`) — mostly Unity/IL2CPP-compatibility adaptations of upstream utility code. |
| **Configuration** | Hollowmere Engine | `Assets/Scripts/ClassicUO/src/Configuration/*` | `Profile`, `ProfileManager`, `Settings` — the same config system upstream uses, with Hollowmere-specific fields layered in. Mobile-specific *product* settings (e.g. `UserPreferences`) live in Gameplay instead. |
| **Diagnostics** | Gameplay | `Assets/Scripts/Hollowmere/Diagnostics/Diag.cs` | Permanent, toggleable, category-based logging. Any Engine-layer file may call into it (see `ENGINE_MODIFICATIONS.md` for which currently do) without that making the calling file "Gameplay" — it's a cross-cutting utility. |

## The rule of thumb

- **Does this file exist because ClassicUO's assumption (mouse, desktop filesystem, XNA
  rendering, native plugin loading) doesn't hold on mobile/Unity, or because of a genuine
  upstream bug?** → Hollowmere Engine (`Assets/Scripts/ClassicUO/src/...`), real file,
  not a symlink, tracked normally.
- **Does this file exist to give players a new capability that upstream ClassicUO never
  had (an action bar, a context-sensitive button, a diagnostics framework)?** → Gameplay
  (`Assets/Scripts/Hollowmere/...`).
- **Does this file exist purely to compose/theme/lay out what's already there?** →
  Presentation (Unity scenes/prefabs, `HollowmereUI`).

## What "first-class engine file" means going forward

Per the engine baseline milestone: there is no such thing as a "local override" file
anymore. A modified `Assets/Scripts/ClassicUO/src/*` file is a Hollowmere Engine file —
tracked in git normally (no `git add -f`), documented in
[ENGINE_MODIFICATIONS.md](ENGINE_MODIFICATIONS.md), and maintained with the same care
as any other engine code. "It's just an override, don't worry about it" is no longer a
valid framing for any file in that tree.

## Protected/Stable systems still apply

This document does not relax anything in the project root `CLAUDE.md`. Movement input,
networking, and packet serialization remain Protected/Stable regardless of which layer
they live in structurally — changes there still need explicit approval.
