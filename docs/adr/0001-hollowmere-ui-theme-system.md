# 0001 — Hollowmere UI Theme System (Foundation)

## Status

Accepted (Issue #20A). #20B (Toggle, Dropdown, Slider, InputField,
SearchBox, Icon, Animations) is a follow-up, not covered by this decision.

## Problem

MobileUO2 is transitioning into "Hollowmere," which will add many new
screens (character select, inventory, paperdoll, spellbook, chat, journal,
maps, on top of the existing login/server browser/settings). The existing
menu UI hardcodes colors directly in C# (e.g.
`ServerConfigurationListItemView.cs` — `new Color(0.6f, 1f, 0.6f, 1f)`;
`OptionEnumView.cs` — `Color.black` / `Color.gray`) with no shared
typography, spacing, or component vocabulary. Building N more screens the
same way multiplies that inconsistency and makes a future visual pass
(or a second theme) a full rewrite instead of a config change.

## Decision

Build a thin theming layer over Unity's existing UGUI (the project's
existing UI toolkit — no TextMeshPro package installed, no UI Toolkit in
use):

- A `HollowmereTheme` ScriptableObject holds named colors (Primary,
  Secondary, Accent, Background, Surface, Border, Success, Warning, Danger,
  Magic, Text, MutedText, Disabled).
- A `HollowmereTypography` ScriptableObject holds named text styles
  (Header/Subheader/Body/Caption/Button/Tooltip), using legacy `Font` +
  `UnityEngine.UI.Text` to match every existing screen.
- `HollowmereMetrics` is a plain static class for spacing/corner-radius/
  shadow tokens — these are physical layout constants shared by every
  theme, not something a theme swaps, so they don't need to be an asset.
- `HollowmereUIService` is a small `DontDestroyOnLoad` MonoBehaviour
  exposing the active theme/typography and a `ThemeChanged` event.
- `HollowmereButton`/`HollowmerePanel` wrap Unity's `Button`/`Image` +
  `Outline`/`Shadow` and drive their existing color/effect properties from
  the active theme. They do not reimplement pointer handling or rendering.
- Assets live under `Assets/Hollowmere/` (not `Resources/`), referenced via
  serialized fields, so a future Addressables migration doesn't require
  restructuring.

Scope was split into #20A (this decision: Theme, Typography, Metrics,
Service, Button, Panel, showcase scene) and #20B (remaining controls +
animations) to keep each milestone reviewable.

## Alternatives considered

- **Full custom UI framework.** Rejected — Unity's UGUI already solves
  layout, input, and rendering; reimplementing any of that would be a
  parallel system fighting the one already in every existing screen.
- **UI Toolkit (UIElements) migration.** Rejected for now — it's a
  different retained-mode system from UGUI; adopting it would mean
  touching every existing screen to get any benefit, which is explicitly
  out of scope (existing screens are not being redesigned).
- **TextMeshPro adoption.** Rejected for this pass — zero existing usage
  in the project to justify the new package dependency; legacy `Text`
  already serves every current screen. Typography styles are defined
  data-first (`HollowmereTypography`) so swapping the renderer later is a
  contained change, not a redesign.
- **Procedurally generated placeholder art for rounded corners.** Rejected
  — reuses only what's already in `Assets/Content/UI/Textures/`
  (`circle.png`, `white32.png`, `white8.png`). Corner-radius tokens exist
  as data now; real 9-sliced art can drop in later with no code change.
  See the known-limitation note in `Assets/Scripts/Hollowmere/UI/README.md`.

## Consequences

- Every future screen built against `Hollowmere.UI` gets theming for free
  and stays consistent by construction rather than by convention.
- Adding a second real theme is additive (new asset + array entry), not a
  code change.
- `HollowmereUIService` must stay small by design — new cross-cutting
  concerns (accessibility, localization, notifications, UI scaling) get
  their own service rather than growing this one.
- Corner radius is currently square except `Round`; visually incomplete
  until real art is dropped in, tracked as a known limitation rather than
  hidden behind generated placeholder art.
- No existing screen was touched or migrated in this milestone — adopting
  `Hollowmere.UI` in Login/Server Browser/Character Select/etc. is future
  work, deliberately not started here.
