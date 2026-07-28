# Hollowmere.UI (Issue #20A — Foundation)

A thin theming layer over Unity's existing UGUI, not a new UI framework.
Every type here wraps a standard Unity component and drives it from a
`HollowmereTheme` asset. Nothing in this namespace touches gameplay,
networking, or any existing production screen.

## Folder structure

```
Assets/Scripts/Hollowmere/UI/
  Theme/
    HollowmereTheme.cs        ScriptableObject: theme colors
    HollowmereTypography.cs   ScriptableObject: named text styles
  HollowmereMetrics.cs        static class: spacing / corner radius / shadow tokens
  HollowmereUIService.cs      MonoBehaviour: active theme + ThemeChanged event
  Controls/
    HollowmereButton.cs       wraps Button, themes its ColorBlock
  Panels/
    HollowmerePanel.cs        wraps Image + Outline/Shadow

Assets/Hollowmere/
  Themes/HollowmereDarkTheme.asset
  Typography/HollowmereTypographyDefault.asset
  Prefabs/HollowmereButton.prefab, HollowmerePanel.prefab
  UI/UIShowcase.unity         demo scene, visual documentation
```

## How theming works

`HollowmereUIService` is a `DontDestroyOnLoad` MonoBehaviour holding the
active `HollowmereTheme`/`HollowmereTypography` and a `ThemeChanged` event.
It's marked `[DefaultExecutionOrder(-1000)]` so its `Awake` (which sets the
static `Instance`) always runs before any `HollowmereButton`/
`HollowmerePanel` `OnEnable` in the same scene load — Unity does not
otherwise order `Awake`/`OnEnable` across sibling GameObjects, and without
this a control instantiated before the service would silently render with
default Unity colors instead of the theme (caught during Play Mode
verification of this milestone).

`HollowmereButton`/`HollowmerePanel` subscribe to `ThemeChanged` in
`OnEnable`, apply once immediately, and unsubscribe in `OnDisable`. No
`Update()` loop, no runtime material creation — theming is `Graphic.color`/
`sprite` swaps only.

## Adding a new themed control

1. Add a component that `[RequireComponent]`s the Unity control it themes.
2. In `OnEnable`, read `HollowmereUIService.Instance`, apply once, subscribe
   to `ThemeChanged`. Unsubscribe in `OnDisable`.
3. Expose a `Variant` enum if the control has stylistic variants; keep the
   public API to properties/methods, not the raw computed `ColorBlock` or
   equivalent.

## Adding a new theme

Create a new `HollowmereTheme` asset (`Assets/Hollowmere/Themes/`), add it
to `HollowmereUIService`'s `availableThemes` array, and call
`SwitchTheme(theme.ThemeName)`. No code changes required. (This milestone
ships one real theme, Dark — see the ADR for why.)

## Known limitation: corner radius

`HollowmereMetrics.CornerRadius` defines Small/Medium/Large as pixel values
for layout math, but no existing project sprite has baked-in rounded
corners and this milestone deliberately does not generate placeholder art
(see ADR). They currently render as square corners. `Round` is the one
variant with real visual rounding, via the existing `circle.png`. Swap in
real 9-sliced art later with no code change.

## Scope

This is #20A only: Theme, Typography, Metrics, UI Service, Button, Panel,
and the showcase scene. Toggle, Dropdown, Slider, InputField, SearchBox,
Icon wrapper, and animations are #20B — not started.
