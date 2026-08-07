# Hollowmere Engine — Modification Inventory

Every file under `Assets/Scripts/ClassicUO/src/` that is currently a **real file**
(not a symlink) and differs from `external/ClassicUO`. 73 files, confirmed by diffing
each one against the submodule (zero accidental identical copies — every file here
carries deliberate content). This is an **engine** inventory only — gameplay code under
`Assets/Scripts/Hollowmere/` is out of scope, see the architecture doc instead.

Columns: **diff** = changed lines vs. submodule (rough size signal, not a quality
signal) · **Mobile?** = touch/Unity/iOS-specific vs. general engine fix · **Upstream?**
= would this be worth contributing back to ClassicUO · **Confidence** = whether the
content was directly read this pass (`read`) or classified from size/keyword/role
inference only (`inferred`) — inferred entries are candidates for the manual-review
pass, not verified facts.

## Rendering

| File | Purpose | Why Hollowmere differs | Mobile? | Upstream? | Related issues |
|---|---|---|---|---|---|
| `Renderer/Batcher2D.cs` (36 diff) | XNA-style 2D sprite batching | Bridges ClassicUO's draw calls onto Unity's graphics pipeline | Unity-bridging, not mobile-specific | No — Unity-specific | — |
| `Renderer/UOTexture.cs` (35 diff) | Texture wrapper | Unity texture upload path instead of XNA's | Unity-bridging | No | — |
| `Renderer/Fonts.cs` (16 diff) | Font rendering | Unity font pipeline integration | Unity-bridging | No | — |
| `Renderer/RenderedText.cs` (8 diff) | Text layout/render | Unity-bridging | Unity-bridging | No | — |
| `Renderer/Texture2DCache.cs` (8 diff) | Texture cache | Unity texture lifecycle | Unity-bridging | No | — |
| `Renderer/XBREffect.cs` (6 diff) | Upscale shader effect | Unity shader integration | Unity-bridging | No | — |
| `Renderer/SpriteFont.cs` (3 diff) | Font metadata | Unity-bridging | Unity-bridging | No | — |
| `Game/GameCursor.cs` (83 diff, **read**) | Cursor rendering, held-item drag visual | Strips native SDL cursor-surface creation (`SDL_CreateRGBSurfaceWithFormatFrom`) — Unity draws its own cursor texture and never needs an SDL surface | Yes — SDL is desktop-only | No — inherently platform-specific | — |
| `Game/GameObjects/Land.cs` (9 diff, inferred) | Terrain tile GameObject | Likely rendering/draw-order adjustment | Inferred | Inferred | Needs manual review |
| `Game/CircleOfTransparency.cs` (5 diff, inferred) | Roof/wall fade-transparency effect around player | Small diff, referenced from `GameScene.Unload()`'s new `Dispose()` call | Inferred | Inferred | — |
| `Game/Managers/AnimatedStaticsManager.cs` (2 diff, inferred) | Animated tile management | Trivial size | Inferred | Inferred | — |
| `Game/Managers/AuraManager.cs` (5 diff, inferred) | Notoriety/target aura ring rendering | Not read | Inferred | Inferred | Needs manual review |
| `Game/World.cs` (2 diff, inferred) | Core world/land/mobile/item collections | Trivial size | Inferred | Inferred | — |

## Input

| File | Purpose | Why Hollowmere differs | Mobile? | Upstream? | Related issues |
|---|---|---|---|---|---|
| `GameController.cs` (537 diff, **read extensively**) | Top-level game loop, mouse/touch dispatch, window management | The single largest engine file: `LeanTouch` finger→`Mouse` translation (`MouseUpdate`, `SimulateMouse`, `UnityInputUpdate`), two-finger-tap gesture, `#24`'s touch-dispatch fix (forces a fresh `MouseOverControl` hit-test before the scene claims a touch-down — the double-tap root cause) | Yes — this file *is* the mobile input bridge | No — Unity/mobile-specific by nature | #24 |
| `Input/Keyboard.cs` (6 diff, inferred) | Keyboard state | Not read, small | Inferred | Inferred | Needs manual review |
| `Game/UI/Controls/WorldViewport.cs` (28 diff, inferred) | The 3D world render surface / tap target | Not read — this is the surface `SelectedObject`/world-tap resolution runs through, central to #24's investigation | Inferred | Inferred | **Needs manual review** — high-traffic file, worth reading fully given its role |
| `Game/Scenes/GameSceneInputHandler.cs` (18 diff, **read**) | World mouse-down/drag dispatch, native click-and-hold and click-and-drag pickup gestures | #24 diagnostics (`DiagCategory.Input`/`ItemHold`) added to trace the native pickup gestures that were racing against the new Primary Action button | No new mobile logic added here this pass — tracing only | N/A (diagnostics) | #24 |

## Networking

| File | Purpose | Why Hollowmere differs | Mobile? | Upstream? | Related issues |
|---|---|---|---|---|---|
| `Network/PacketHandlers.cs` (36 diff, **read, root-caused**) | All incoming packet handling | Two independent changes: (1) mobile-specific — `OpenURL` on Android/iOS instead of `Process.Start`, which doesn't exist there; (2) **genuine upstream bug fix** — `DenyMoveItem`'s recovery condition checked `AllowedToDraw`, a rendering-culling flag that's never set false in response to a pickup/drop, making item recovery after a denied move unreachable for any normal item. Fixed by checking `ItemHold.Enabled`/`Dropped` alone. Deterministically validated via a synthetic deny packet (see #24 investigation). | Partially — the `OpenURL` hunk is mobile-specific, the `DenyMoveItem` fix is a general engine bug independent of mobile | **Yes, strongly** — the `DenyMoveItem` fix is a correctness bug in stock ClassicUO, not a Hollowmere-specific need | #24 |
| `Network/Plugin.cs` (127 diff, **read**) | Native C-plugin loading/marshaling (`OnCastSpell`, `OnGetPacketLength`, function-pointer interop) | Strips native plugin marshaling — Unity/mobile can't load native plugin DLLs the way desktop ClassicUO does; Hollowmere has its own C# `Assets/Scripts/Assistant/*` reimplementation instead | Yes | No — architecturally mobile/Unity-specific | — |

## Asset Loading

| File | Purpose | Why Hollowmere differs | Mobile? | Upstream? | Related issues |
|---|---|---|---|---|---|
| `IO/UOFileManager.cs` (90 diff, **read**) | Resolves UO data file paths | Explicit, self-documented fix: `//NOTE: Potential fix file not found issues on iOS due to filesystem case sensitivity` — retries alternate casing when a file isn't found | Yes, iOS-specific | Maybe — a generic case-insensitive-fallback could be upstreamed, low risk | — |
| `IO/Resources/AnimationsLoader.cs` (82 diff, **read**) | Animation frame/sequence loading | Disposal/cache-clear logic rewritten — likely a leak or re-login/hot-reload fix | Inferred (could be either) | Inferred | Needs manual review |
| `IO/Resources/MapLoader.cs` (44 diff, inferred) | Map tile loading | Not read | Inferred | Inferred | Needs manual review |
| `IO/Resources/SoundsLoader.cs` (45 diff, inferred) | Sound asset loading | Not read | Inferred | Inferred | Needs manual review |
| `IO/UOFile.cs` (8), `IO/UOFileLoader.cs` (2) | Low-level file I/O base classes | Not read, trivial size | Inferred | Inferred | — |
| `IO/Resources/GumpsLoader.cs` (12), `ArtLoader.cs` (5), `FontsLoader.cs` (5), `HuesLoader.cs` (4), `LightsLoader.cs` (10), `TexmapsLoader.cs` (10), `TileDataLoader.cs` (6), `MultiLoader.cs` (6), `MultiMapLoader.cs` (11), `AnimDataLoader.cs` (7), `ProfessionLoader.cs` (5), `SkillsLoader.cs` (7), `SpeechesLoader.cs` (4) | Per-asset-type loaders | Not individually read; uniformly tiny diffs, consistent with small Unity-path/format adaptations rather than behavior changes | Inferred | Inferred | Group candidate for one batched manual review pass rather than 13 separate ones |
| `IO/Audio/Sound.cs` (12), `UOMusic.cs` (6), `UOSound.cs` (1) | Audio playback | Not read, trivial | Inferred | Inferred | — |
| `Game/Managers/AudioManager.cs` (11 diff, inferred) | Audio manager | Not read | Inferred | Inferred | Needs manual review |

## UI

| File | Purpose | Why Hollowmere differs | Mobile? | Upstream? | Related issues |
|---|---|---|---|---|---|
| `Game/Managers/UIManager.cs` (37 diff, **read, root-caused**) | Central Gump/Control dispatch, click routing, hover tracking | Pre-existing: `Dispose()` method, `Gumps.Clear()` + `_mouseDownControls` reset on scene teardown, `GetMouseOverControl` made `public`. #24: `HandleMouseInput()` made `internal` (was `private`) so `GameController` can force a fresh hit-test before the scene claims a touch — the actual double-tap fix; plus a `DiagCategory.UI` trace | #24 portion: yes, root-causes a touch-only bug | The `HandleMouseInput` fix is arguably upstream-worthy (desktop never hits this bug because continuous mouse-motion keeps state fresh) | #24 |
| `Game/UI/Controls/ScrollArea.cs` (34 diff) | Scrollable content area | Touch-drag scrolling (documented pre-existing override) | Yes | Maybe | — |
| `Game/UI/Controls/Control.cs` (25 diff, touch signal present) | Base UI control class | Touch input plumbing at the base-class level | Yes | Maybe | — |
| `Game/UI/Controls/Button.cs` (34 diff, touch signal present) | Button control | Touch tap handling | Yes | Maybe | — |
| `Game/UI/Controls/StbTextBox.cs` (8 diff) | Text input box | Touch keyboard integration (documented pre-existing override) | Yes | Maybe | — |
| `Game/UI/Controls/Checkbox.cs` (14 diff, inferred) | Checkbox control | Not read | Inferred | Inferred | Needs manual review |
| `Game/UI/Controls/HtmlControl.cs` (15 diff, some signal) | Rich text display | Some touch/Unity signal | Inferred | Inferred | — |
| `Game/UI/Controls/ItemGump.cs` (5 diff, inferred) | Draggable item icon | Trivial, not read | Inferred | Inferred | — |
| `Game/UI/Gumps/OptionsGump.cs` (37 diff, high touch signal) | Settings screen | Mobile settings additions (documented pre-existing override) | Yes | No — Hollowmere-specific settings | — |
| `Game/UI/Gumps/ShopGump.cs` (107 diff, **read**) | Vendor buy/sell UI | `GetTexture(id, true)` — an extra argument passed consistently across texture lookups, likely a caching/premultiply behavior change | Inferred (not confirmed mobile-specific) | Inferred | Needs manual review — largest Gump diff not fully read |
| `Game/UI/Gumps/SkillGumpAdvanced.cs` (25 diff) | Skills UI | Touch sizing (documented pre-existing override, this session added the "Skills tab shows chat popup" fix earlier) | Yes | No | #22 |
| `Game/UI/Gumps/SpellbookGump.cs` (14 diff) | Spellbook UI | Touch-adapted spellbook (documented pre-existing override) | Yes | No | — |
| `Game/UI/Gumps/WorldMapGump.cs` (179 diff, largest Gump file) | World map | Pinch-to-zoom/drag-to-pan added this session, plus pre-existing touch work | Yes | No | Pinch-zoom/pan work (this session, pre-#24) |
| `Game/UI/Gumps/Gump.cs` (40 diff, inferred) | Base Gump class | Not read | Inferred | Inferred | Needs manual review |
| `Game/UI/Gumps/Login/LoginGump.cs` (42 diff, some signal) | Login screen | Not fully read | Inferred | Inferred | Needs manual review |
| `Game/UI/Gumps/ModernBookGump.cs` (7 diff, signal present) | Book reading UI | Small, some touch signal | Inferred | Inferred | — |
| `Game/UI/Gumps/ResizableGump.cs` (8 diff, some signal) | Resizable Gump base | Small | Inferred | Inferred | — |
| `Game/UI/Gumps/SystemChatControl.cs` (4 diff) | Chat text control | Documented pre-existing override, trivial | Yes | No | — |
| `Game/UI/Gumps/WorldViewportGump.cs` (25 diff, some signal) | World viewport container Gump | Some touch signal | Inferred | Inferred | — |
| `Game/UI/Gumps/HealthBarGump.cs` (22 diff, **read**) | Mobile/target health bar | Long-press-to-close (touch has no right-click to close it otherwise) — **added this session, not #24, intentionally excluded from this milestone's tracking** | Yes | No | Health-bar-closing fix (this session) |

## Mobile Runtime

| File | Purpose | Why Hollowmere differs | Mobile? | Upstream? | Related issues |
|---|---|---|---|---|---|
| `Game/Scenes/GameScene.cs` (63 diff, **read, root-caused**) | Main game scene: draw loop, movement input processing, world update | Joystick-driven movement (`JoystickInput`/`JoystickRunThreshold`, follow-cancel), configurable minimum viewport size, `batcher.scale` save/restore around drawing, an extra `GraphicsDevice.Clear` (hall-of-mirrors fix around undrawn tiles), `DrawLights` disabled, `CircleOfTransparency.Dispose()` on unload, plus #24's `ItemHold`-category diagnostic on the native hold-pickup gesture | Yes, extensively | No — deeply mobile/Unity-specific | #24 (diagnostic only) |
| `Client.cs` (12 diff, inferred) | Client bootstrap | Not read | Inferred | Inferred | Needs manual review |
| `CUOEnviroment.cs` (2 diff, inferred) | Environment/build flags | Trivial | Inferred | Inferred | — |

## Utility

| File | Purpose | Why Hollowmere differs | Mobile? | Upstream? | Related issues |
|---|---|---|---|---|---|
| `Utility/UnsafeMemoryManager.cs` (148 diff, **read**) | Low-level unsafe pointer helpers | Strips Mono/SDL-platform-detection constructor logging (`SDL_GetPlatform`-driven), replaced with a Unity/IL2CPP-appropriate path | Yes | No — runtime-specific | — |
| `Utility/ZLib.cs` (94 diff, **read**) | Decompression | Strips SDL-platform-based native-compressor selection (Mac/Linux-specific unmanaged compressor binaries) — not applicable to Unity's build pipeline | Yes | No | — |
| `Utility/Exstentions.cs` (56 diff, inferred) | Extension methods | Largest un-sampled Utility file, not read | Inferred | Inferred | **Needs manual review** |
| `Utility/QueuedPool.cs` (2 diff, inferred) | Object pooling | Trivial | Inferred | Inferred | — |

## Configuration

| File | Purpose | Why Hollowmere differs | Mobile? | Upstream? | Related issues |
|---|---|---|---|---|---|
| `Configuration/Profile.cs` (14 diff, inferred) | Per-character profile settings | Not read | Inferred | Inferred | Needs manual review |
| `Configuration/ProfileManager.cs` (3 diff, inferred) | Profile lifecycle | Trivial | Inferred | Inferred | — |
| `Configuration/Settings.cs` (2 diff, inferred) | Global settings | Trivial | Inferred | Inferred | — |

## Files intentionally excluded from this inventory's "track now" scope

- **`Game/UI/Gumps/HealthBarGump.cs`** — real behavior added this session (long-press-to-close), but not part of Issue #24. Left out of this milestone's git-tracking step per explicit instruction; still listed above for completeness since it is a genuine engine modification.

## Summary

- **73 files total**, 0 accidental/identical copies.
- **~15 files directly read and confirmed** this pass (content sampled or fully reviewed).
- **~58 files classified by size/signal/role only** — flagged "Needs manual review" where the diff is non-trivial (`WorldViewport.cs`, `ShopGump.cs`, `Gump.cs`, `LoginGump.cs`, `Land.cs`, `AudioManager.cs`, `AuraManager.cs`, `Checkbox.cs`, `MapLoader.cs`, `SoundsLoader.cs`, `Exstentions.cs`, `Client.cs`, `Profile.cs`, `Keyboard.cs`) and treated as low-risk-by-size for the rest.
- **One confirmed genuine upstream bug** (`PacketHandlers.cs`'s `DenyMoveItem` recovery condition) worth a real upstream PR consideration, independent of anything mobile-specific.
- **No accidental or stale files found** — every sampled file (including all "zero mobile-keyword-signal" ones) turned out to be a deliberate, legible platform adaptation once actually read.
