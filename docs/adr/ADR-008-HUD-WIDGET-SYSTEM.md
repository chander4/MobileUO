# ADR-008: HUD Widget System

## Status

Accepted. This ADR turns the reviewed HUD Widget Architecture into a permanent Hollowmere architectural foundation. It authorizes no implementation by itself; each milestone still follows the development process and protected-system rules.

## Context

Hollowmere needs a customizable, durable HUD across phones, tablets, orientations, accessibility layouts, controller-oriented layouts, streaming, and future desktop use. Existing HUD-adjacent elements use both Gumps and Unity UI: the Action Bar and Primary Action are Gump-side, while input capture and app chrome use Unity. A fixed-position HUD or a renderer-specific layout system would make each new surface a special case and would eventually duplicate gameplay UI.

The review of `HUD_WIDGET_ARCHITECTURE.md` found the basic host/presenter/profile model sound, but identified four gaps that must be explicit before implementation: widget identity longevity, separation of layout from widget configuration, profile-copy complexity, and the temptation to introduce a generic controller or package system before either has a demonstrated need.

## Decision

Hollowmere adopts a renderer-neutral HUD Widget System.

1. A widget is a registered layout participant, not a new rendering technology. It presents existing state through its authoritative renderer or triggers an existing `GameActionDefinition`; it does not own gameplay.
2. `HudWidgetRegistry` is the single source of known widget definitions. A definition has a stable ID, renderer kind, capabilities, default state, and presenter factory. Categories are registry metadata for editor discovery only.
3. Widget IDs are permanent persistence API. IDs are namespaced, never reused, and never changed merely for display naming. A renamed/replaced widget retains an alias migration map; a removed widget leaves a deprecated tombstone until old supported profile versions no longer require it. Layout schema versioning and widget-ID migration are separate concerns.
4. `HudWidgetState` contains host-owned layout state only: identity, visibility, lock state, placement/dock relation, and size policy. Widget-specific, versioned configuration belongs to `HudWidgetSettings`, validated and interpreted by that widget's definition/presenter, never by the host.
5. `HudLayoutProfile` stores full snapshots. Runtime profile inheritance is not introduced. A profile may be copied from another profile or variant at creation time, but later changes do not flow between them. Variant fallback remains a host-defined resolution rule, not inheritance.
6. `HudWidgetHost` coordinates registry lookup, profile selection, lifecycle, geometry, Edit Mode, and persistence. `IHudWidgetPresenter` applies resolved geometry to a Gump group or Unity root. Do not add a generic `WidgetController` layer: the host/presenter boundary is sufficient until a concrete widget has reusable non-rendering state that neither its existing gameplay pipeline nor presenter can own.
7. A Gump widget remains Gump-rendered and a Unity widget remains Unity-rendered. The host supplies geometry only. Existing `AnchorManager` groups remain internal to an atomic Gump widget; they are not the global HUD layout engine.
8. Runtime pixels are owned by their native renderer. In HUD Edit Mode, a Unity-owned overlay exclusively owns layout gestures; it must not forward them into a Gump or gameplay action.
9. Widget packages, plugin discovery, live profile inheritance, nested docking, and a global window manager are not part of the foundation. They require a demonstrated problem and a later ADR if needed.

## Alternatives considered

### Fixed, renderer-specific HUD

Rejected. It cannot support device/orientation profiles cleanly and would encode new position rules into every widget.

### Unity-only HUD or replacing Gumps

Rejected. It would duplicate protocol-connected game-state rendering and violate the established rendering strategy. Unity owns app chrome and layout organization; ClassicUO Gumps own game-state UI.

### Reuse `AnchorManager` as the global HUD layout engine

Rejected. It is valuable for existing Gump-internal grouping, but it cannot own Unity widgets, profile variants, safe areas, or shared cross-renderer Edit Mode without becoming a parallel and renderer-biased layout system.

### Generic `WidgetController` for every widget

Rejected. Hollowmere already separates reusable action domain logic from presentation through `PrimaryActionResolver`, `PrimaryActionDefinitionFactory`, and `GameActionDefinition`. A controller inserted between every presenter and renderer would duplicate that pattern without a second concrete lifecycle problem. Introduce a narrowly named controller later only when actual shared state proves the host/presenter split insufficient.

### Live profile inheritance

Rejected for the foundation. It creates hard-to-explain override, deletion, migration, import/export, and conflict rules. Full snapshots are larger but simple, portable, and predictable. Copy-on-create preserves the useful workflow without turning a player layout into a dependency graph.

### Widget packages

Rejected for the foundation. Central registration plus category metadata serves known built-in widgets. Packages introduce compatibility, dependency, versioning, security, and load-order concerns before there is a third-party distribution need.

## Consequences

- New widgets register once and participate in the same profile/edit/layout path.
- Saved layouts survive widget renames and device changes through explicit policy, not incidental class names or screen pixels.
- Widget configuration cannot leak renderer- or feature-specific semantics into layout code.
- Existing Gump groups can be migrated incrementally as atomic widgets without changing their action or game-state logic.
- Full-profile copying may duplicate values, but avoids opaque inheritance behavior.
- The first implementation remains deliberately small: registry, profile model, host, two presenter families, and one proof widget before advanced docking or packages are reconsidered.

## Permanent architectural rules

- HUD widgets are presentation/layout participants; they never own or duplicate gameplay state.
- Game-state UI remains a ClassicUO Gump. Application chrome, including HUD Edit Mode, remains Unity UI. A chrome shortcut and the game-state Gump it opens are separate widgets with separate ownership.
- Every widget definition is registered through `HudWidgetRegistry` and has a permanent, namespaced ID with explicit alias/deprecation migration policy.
- `HudWidgetState` is layout-only. `HudWidgetSettings` is widget-owned, versioned configuration.
- A renderer-neutral profile stores no delegates, Unity references, live world objects, account credentials, or server/gameplay state.
- `HudWidgetHost` owns shared layout/lifecycle policy; presenters own only their renderer bridge. No universal controller layer exists by default.
- `AnchorManager` may arrange controls within an existing Gump widget, but never owns global HUD layout.
- At any screen point and time, exactly one system owns input. Edit Mode consumes layout input before native widget dispatch.
- Profiles are full snapshots with explicit variants and fallback. Live inheritance and packages remain out of scope until separately justified and decided.
