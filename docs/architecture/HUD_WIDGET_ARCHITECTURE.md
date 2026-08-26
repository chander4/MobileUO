# Hollowmere HUD Widget Architecture

**Status: accepted by ADR-008; implementation remains milestone-scoped and requires
the normal approval and validation process.**

This proposal designs the permanent mobile HUD foundation. It must be reviewed,
approved, and recorded in an ADR before any implementation starts. It follows the
rendering boundary in `HOLLOWMERE_UI_RENDERING_STRATEGY.md`: the HUD organizes
access to game state, while game-state content remains rendered and owned by its
existing system.

## 1. Vision

Hollowmere's HUD is a player-owned layout, not a fixed collection of controls.
A player can arrange compatible widgets for phone, tablet, portrait, landscape,
accessibility, controller, streaming, and desktop-oriented layouts without
rewriting a spell, packet, Gump, or gameplay rule.

“Widget” describes a layout participant, not a new rendering technology. An
Action Bar, Primary Action button, joystick, health bar, chat launcher, and
future minimap can all be widgets even though they do not share a renderer or
game-state model. A widget either presents existing game state through its
authoritative renderer or triggers existing `GameActionDefinition` values. It
never owns, duplicates, or interprets gameplay state.

## 2. Architectural goals

- Keep game-state widgets Gump-rendered and Hollowmere app chrome Unity-rendered.
- Make layout data independent of a widget's renderer and independent of screen
  pixels, so profiles survive rotation, resolution, safe areas, and device class.
- Make a widget's persistent identity, capabilities, and settings explicit.
- Treat each orientation/device/input variation as a layout variant, not a forked
  HUD implementation.
- Keep the current Action Bar's internal `AnchorManager` grouping intact while
  preventing it from becoming a second global HUD layout system.
- Make editing a deliberate mode with unambiguous input ownership, never a
  permanent race between Unity and Gump touch dispatch.
- Support profile export/import and migration through a versioned schema without
  serializing delegates, Unity object references, live `BaseGameObject` values,
  or server/gameplay state.

## 3. Widget model

The proposed model has three deliberately separate concerns.

| Concern | Proposed responsibility | Must not contain |
|---|---|---|
| `HudWidgetDefinition` | Registered, stable widget ID; display metadata; renderer kind; supported capabilities; factory for a runtime presenter. | Player placement, live gameplay state, a serialized delegate. |
| `HudWidgetState` | One widget's host-owned persisted visibility, lock state, size policy, and placement in a layout variant. | Widget-specific configuration, renderer references, delegates, world targets. |
| `HudWidgetSettings` | Widget-owned, versioned configuration interpreted and validated by its definition/presenter. | Placement, profile selection, or host policy. |
| `IHudWidgetPresenter` | Runtime bridge that creates/shows/hides one existing Gump or Unity view and applies a resolved rectangle. | Profile selection, drag policy, gameplay decisions. |

`HudWidgetDefinition` is a registry entry, not a subclass hierarchy for each
feature. Its namespaced ID is permanent persistence API and is never reused;
display names and icons may change without breaking saved layouts. Renames use an
explicit alias migration and removals retain a deprecated tombstone while supported
profiles need it. The definition declares whether
the widget is movable, hideable, resizable, dockable, and whether it supports a
given layout variant. Widgets may opt out of resize or hide, but the exception is
declared capability rather than an untracked special case.

`IHudWidgetPresenter` has two initial families:

- **Gump presenter:** owns placement of an existing Gump or atomic Gump group.
  The Gump keeps rendering game state and handling its established interaction.
- **Unity presenter:** owns placement of a Unity app-chrome root within the HUD
  Canvas and retains normal Unity lifecycle/accessibility integration.

The presenter receives geometry only. A Primary Action presenter continues to
obtain its verb through `PrimaryActionResolver` → `PrimaryActionDefinitionFactory`
→ `GameActionDefinition`; the host never chooses or executes that verb.

## 4. Widget lifecycle

1. **Register:** startup registers known definitions with a `HudWidgetRegistry`.
   Registration does not instantiate views.
2. **Resolve profile:** `HudLayoutService` selects a named profile and its best
   matching variant for the current viewport.
3. **Materialize:** `HudWidgetHost` creates presenters only when a game scene and
   their renderer's prerequisites exist. A Gump widget waits for `UIManager`/
   `GameScene`; a Unity widget waits for its Canvas root.
4. **Apply:** the host resolves safe-area-relative state into a rectangle and asks
   each visible presenter to apply it.
5. **React:** viewport, orientation, selected-profile, or edit-mode changes cause
   a new layout pass. Gameplay changes stay inside the widget's existing owner.
6. **Teardown:** scene exit disposes or detaches presenters without deleting their
   saved state. Re-entry materializes a fresh view from the same profile.

The host owns lifecycle coordination; widgets own their renderer-specific cleanup.
No `Update()` poll in one widget becomes the global source of layout truth.

## 5. WidgetHost responsibilities

`HudWidgetHost` is a renderer-neutral coordinator, ideally a small gameplay/
presentation boundary service with a Unity bootstrap only where Unity lifecycle is
needed. It is not a Canvas replacement and is not a Gump manager.

It is responsible for:

- registry lookup, profile selection, variant fallback, and schema migration;
- calculating one shared safe layout rectangle from viewport, safe area, and
  reserved regions;
- materializing presenters and applying resolved geometry;
- entering/exiting HUD Edit Mode and routing edit gestures to the layout model;
- enforcing each widget's declared capabilities and lock/visibility state;
- saving atomically after a completed edit, and emitting layout diagnostics.

It is not responsible for drawing health, querying world state, executing an
action, changing `AnchorManager` internals, or deciding who a primary action
targets. Those remain the responsibility of existing Gump/Unity/gameplay owners.

## 6. Proposed interfaces and classes

Names below are a design vocabulary, not an implementation prescription.

```text
HudWidgetRegistry
  the required single registry of HudWidgetDefinition instances by stable ID

HudLayoutService
  loads, validates, migrates, selects, saves, imports, and exports profiles

HudWidgetHost
  materializes presenters, resolves geometry, owns edit-mode coordination

HudWidgetDefinition
  id, renderer kind, capability flags, default state, presenter factory

HudWidgetState
  id, visibility, locked flag, size policy, placement

HudWidgetSettings
  widget-owned versioned configuration; never interpreted by the host

HudLayoutProfile
  schema version, profile metadata, per-variant widget states

HudLayoutVariantKey
  orientation + device class + optional input/layout mode

HudPlacement / HudResolvedRect
  renderer-neutral persistent geometry / runtime pixel geometry

IHudWidgetPresenter
  create, apply geometry, set visibility, enter/exit edit presentation, dispose

IHudLayoutStorage
  local persistence now; cloud or account-backed storage later
```

There is intentionally no generic `WidgetController` layer. The existing action
pipeline already separates domain resolution from presentation; add a narrowly
named controller only when a concrete widget demonstrates reusable state that
neither its existing domain pipeline nor a presenter can own.

The host communicates with a presenter through an explicit boundary such as
`Apply(HudResolvedRect)`, not by accessing Gump internals or Unity component
fields. A Gump presenter can move an existing Gump/group; a Unity presenter can
set its root `RectTransform`. Neither bridge exposes renderer-specific state to
the layout model.

## 7. Layout system

Layout is expressed in a **safe layout rectangle**, not raw screen coordinates.
The rectangle begins after platform safe-area insets and any explicitly reserved
system-owned region. A profile therefore does not need separate coordinates for a
notch, gesture area, or changed window size.

Each `HudPlacement` stores:

- an anchor point and pivot in normalized safe-rectangle coordinates;
- an offset in logical HUD units derived from the safe rectangle's scale;
- a size policy: intrinsic, discrete scale, or resizable bounds;
- optional dock target/edge and docking order;
- a z/layer hint valid only within the owning renderer's allowed layer.

Normalized anchors preserve intent across aspect ratios; logical offsets preserve
fine placement without baking in a device's pixels. Presenters convert the
resolved rectangle to their native coordinate system. The schema stores neither
`RectTransform` anchors nor Gump pixels directly.

The host owns **inter-widget** placement. `AnchorManager` remains the internal
layout mechanism for a Gump widget that already uses it. For example, an Action
Bar remains one atomic HUD widget whose buttons keep their existing anchored
group; the HUD host moves the bar origin/group as one unit. It must not create a
new global `AnchorManager` group containing unrelated HUD widgets.

## 8. Serialization strategy

Use a versioned, human-inspectable `HudLayoutProfile` document with an explicit
schema version and migration chain. Profiles are complete snapshots, not live
inheritance graphs: creation may copy another profile or variant, but later edits
never flow between them. Local storage is an `IHudLayoutStorage`
implementation; `PlayerPrefsActionBarPersistence` remains Action Bar-specific
until a migration is deliberately approved. Do not extend it into an unversioned
global layout bucket.

Export/import packages contain only profile metadata, variant states, and
widget-owned serializable settings. `HudWidgetSettings` is validated by its
registered widget rather than interpreted by the host. They exclude account credentials, server
addresses, character data, live game objects, delegates, and device identifiers.
Import validates schema version, known widget IDs, ranges, and capability
compatibility; unknown widgets are retained as inert entries when safe so a layout
round-trips across versions, rather than being silently erased.

Default layouts are registered in code/data and copied into a new profile once.
They are never overwritten after a player customizes a profile. A new widget gets
its default state only when no saved state exists for that widget.

## 9. Runtime versus Edit Mode

**Runtime Mode** prioritizes gameplay. Each visible widget's native renderer owns
its active pixels and receives only its normal input. Locked state prevents
accidental rearrangement; widgets do not expose drag handles during play.

**Edit Mode** is a Unity-owned app-chrome overlay. It draws selection outlines,
safe-area/dock guides, and handles; it captures layout gestures before Gump or
gameplay dispatch. The overlay changes `HudWidgetState` through the host, which
applies geometry to the selected presenter's native view. It never forwards a
layout drag as a Gump click or executes a widget action.

This is the intended resolution of hybrid input ownership: at a pixel and time,
either a native widget owns gameplay interaction (Runtime Mode) or the Unity edit
overlay owns layout interaction (Edit Mode), never both.

## 10. Dragging model

Dragging is an Edit Mode operation, not a universal `CanMove` behavior. On press,
the overlay identifies a visible, unlocked widget's resolved bounds, records the
pointer-to-pivot offset, and previews its candidate placement. On release, it
commits one validated placement and saves once. Cancel restores the original
state.

The initial model supports one selected widget and one pointer. Multi-select,
lasso, freeform resize handles, and runtime long-press-to-edit are deferred until
their accessibility and cross-renderer input semantics are proven. Existing
Gump dragging remains untouched until a widget is migrated to host-managed layout;
the migration must disable competing movement for that widget rather than let two
drag systems move it simultaneously.

## 11. Snapping model

Snapping is host policy applied to renderer-neutral rectangles:

- safe-area edges and corners;
- a logical spacing grid;
- compatible dock zones;
- optional alignment to other widgets' resolved edges/centers.

The nearest candidate within a documented tolerance wins, with stable tie-breaking
so placement does not jitter between renderer frames. Snapping may be temporarily
disabled by an explicit edit gesture, but it is not a hidden per-widget behavior.
`AnchorManager` snapping remains local to the internals of an Action Bar or
Assistant group; it is not consulted for unrelated widget-to-widget snapping.

## 12. Docking model

Docking is a higher-level relationship than snapping. A docked widget stores a
zone (safe edge/corner or named host region), order, spacing, and its independent
size policy; the host lays out the zone's children together. Undocking restores a
free placement. Zones are logical, not Unity panels or Gump containers, allowing a
Gump widget and Unity widget to occupy coordinated but separately rendered slots.

Initial docking should support only edge zones and a single ordered stack. Nested
docking trees, tab containers, overlapping dock stacks, and a desktop-window
manager are intentionally deferred.

## 13. Portrait versus landscape

Each profile has a base variant and explicit orientation variants. Selection uses:

1. exact device-class + orientation variant;
2. orientation variant;
3. device-class base;
4. profile base.

Editing portrait does not mutate landscape, and rotation does not destructively
copy one layout over another. A player may explicitly duplicate a variant as a
starting point. The host recomputes every resolved rectangle after rotation and
revalidates safe-area/dock constraints.

## 14. Tablet considerations

Tablet is a device-class override, not a scaled phone assumption. A tablet
variant may change available dock regions, discrete widget scale, or visibility
defaults while preserving the same widget IDs and action/game-state boundaries.
The profile still falls back safely when no tablet override exists. Split-screen,
resizable desktop windows, and ultrawide displays should use the same viewport
classification mechanism rather than separate implementations.

## 15. Future extensibility

- **Accessibility:** profiles can prioritize larger scale, reduced density,
  reachability, and high-contrast renderer settings without changing game state.
- **Controller:** a controller-oriented variant can place prompts/chrome while
  consuming the same `GameActionDefinition` and `PrimaryAction` pipeline. New
  controller input semantics require their own ADR before implementation.
- **Streaming and desktop:** named profiles can hide sensitive/nonessential
  widgets or prioritize information density, using the same serialization format.
- **Import/export:** portable profiles can be shared without coupling layout data
  to a scene, resolution, account, or renderer implementation.
- **New widgets:** register a definition and presenter; do not modify the host's
  placement, profile, drag, or serialization logic for a widget-specific rule.

## 16. Risks and mitigations

| Risk | Mitigation |
|---|---|
| Unity and Gump touch dispatch race | Edit overlay exclusively owns edit input; Runtime Mode assigns ownership by widget renderer before dispatch. |
| Global host leaks gameplay responsibilities | Presenter contracts accept geometry/visibility only; actions remain `GameActionDefinition` and game-state Gumps remain authoritative. |
| Existing `AnchorManager` groups break | Treat each legacy group as an atomic Gump widget; do not mix unrelated widgets into its group. |
| Profile corruption or incompatible imports | Versioned schema, validation, migration, atomic write, backup/last-known-good profile. |
| Per-device layout explosion | Explicit fallback order and optional overrides instead of mandatory copies for every device. |
| Layout editing causes accidental gameplay actions | Unity overlay consumes all edit gestures and disables native action dispatch beneath it. |
| Over-abstraction before real widgets | Start with a registry, host, profile, and two renderer presenters only; add capabilities only when a shipped widget needs them. |
| Engine changes become necessary | Prefer a Hollowmere adapter. If an engine hook is genuinely missing, stop for approval and follow ADR-006. |

## 17. Deliberate non-goals for the first implementation

Do not implement these until the foundation and first migrations prove the model:

- a wholesale Gump-to-Unity migration or a Unity-only HUD;
- a new global replacement for `UIManager` or `AnchorManager`;
- inventory, spellbook, vendor, health-bar, minimap, buff, or chat redesign;
- nested docking, tabs, arbitrary overlap/z-order management, or a desktop window
  manager;
- cloud sync, community layout browsing, or remote configuration;
- controller input capture, new gesture vocabulary, or changes to movement input;
- freeform per-frame layout animation, constraint solving, or pixel-perfect
  cross-renderer composition;
- serializing `GameActionDefinition`, delegates, live targets, or gameplay state.

## Proposed independently shippable milestones

### Issue #25 — HUD layout contract and safe no-op profile foundation

**Goal:** Approve the HUD ADR and add the renderer-neutral registry, versioned
profile schema, variant selection, storage boundary, and migration from current
fixed defaults without changing visible gameplay behavior.

**Expected files:** new `Assets/Scripts/Hollowmere/Hud/*` model/service/storage
files; a HUD architecture ADR; `PROJECT_STATE.md`; `AI_HANDOFF.md`; this document.

**Protected systems touched:** None expected. Do not edit `UIManager`, raw input,
networking, or asset loading.

**Validation strategy:** serialization round-trip and migration checks; profile
validation for unknown widget IDs/ranges; runtime login/logout lifecycle with all
existing HUD controls unchanged; `Diag` traces for profile selection only if real
diagnosis requires them.

**Stopping point:** an inactive/default profile can be loaded and selected, but
no player-facing widget has moved and no edit UI exists.

### Issue #26 — WidgetHost, Edit Mode, and Primary Action migration

**Goal:** Ship the first host-managed Gump widget: Primary Action. Add Unity Edit
Mode overlay, locking, move/hide where allowed, safe-area snapping, and separate
portrait/landscape placement for that one widget.

**Expected files:** `HudWidgetHost`, primary-action Gump presenter, Unity edit
overlay/prefab, profile integration, `PrimaryActionButtonBootstrap` adaptation,
documentation and ADR updates.

**Protected systems touched:** None expected. `UIManager` use must remain through
its existing public surface; any need to alter Engine input dispatch requires
explicit approval before proceeding.

**Validation strategy:** device verification of Runtime versus Edit Mode ownership;
tap/hover regression test for the #24 Primary Action path; rotation and safe-area
checks; login/logout recreation; save/reload position; `Diag.UI` tracing during
hybrid-input investigation.

**Stopping point:** Primary Action is movable and independently saved per
orientation; no Action Bar, joystick, or game-state Gump migration yet.

### Issue #27 — Action Bar as an atomic HUD widget

**Goal:** Move the main Action Bar under host placement while retaining its
existing internal `AnchorManager` group, catalog, and action behavior.

**Expected files:** Action Bar presenter/adapter, Action Bar bootstrap adaptation,
layout-profile migration, compatibility bridge for existing action-bar position
preferences, docs/tests.

**Protected systems touched:** None expected. `AnchorManager` is consumed, not
modified; an engine change is out of scope.

**Validation strategy:** add/remove/reorder/orientation behavior; bar drag does
not compete with Edit Mode; saved catalog contents and legacy position migrate;
Action Bar and Assistant anchored controls retain their current behavior.

**Stopping point:** Action Bar is one movable/dockable host widget. It does not
introduce global docking or change how individual action buttons execute.

### Issue #28 — Unity widget adapter and joystick layout migration

**Goal:** Prove the second renderer family by hosting the existing joystick as a
Unity widget with the same profile/edit semantics as the two Gump widgets.

**Expected files:** Unity presenter, joystick layout adapter, reserved-region
metadata, edit overlay refinements, documentation.

**Protected systems touched:** Mobile Input V2, `FloatingJoystick`, and raw touch
input are Stable. Explicit approval is required before modifying any of them. The
preferred scope is relocating presentation only, with no movement-behavior change.

**Validation strategy:** on-device movement, simultaneous UI exclusion, edit-mode
capture, accessibility scale, rotation, and regression of existing joystick
behavior across login/reconnect.

**Stopping point:** one Unity widget and two Gump widgets share a profile; no new
controller input or gesture system is introduced.

### Issue #29 — Dock zones, layout profiles, and import/export

**Goal:** Add named profiles, base/portrait/landscape/tablet variants, simple edge
dock zones, and validated local export/import for the proven widget set.

**Expected files:** profile manager/UI, import/export codec and validation,
dock-zone resolver, documentation, migration notes.

**Protected systems touched:** None expected.

**Validation strategy:** profile switching without gameplay restart; exact fallback
order; export/import round trip; malformed/unknown-widget input rejection or safe
retention; device checks on phone and tablet-class viewport.

**Stopping point:** profiles and one-level docking work for existing widgets; no
cloud sync, nested tabs, or remote sharing.

### Issue #30 — First game-state HUD widget study and migration

**Goal:** Select one existing game-state Gump (after a separate design review) and
prove that a Gump presenter can host it without duplicating its state or rendering.

**Expected files:** selected widget presenter, narrowly scoped existing Gump
adapter if needed, layout registration, docs/ADR update, runtime validation notes.

**Protected systems touched:** No Protected system expected. Any required ClassicUO
override follows ADR-006 and requires explicit approval before conversion.

**Validation strategy:** compare behavior with the pre-migration Gump; verify
server-driven updates, input ownership, target/selection state, and scene teardown
on device.

**Stopping point:** one game-state Gump participates in layout. No health/mana
redesign, new data model, or renderer migration is included unless separately
approved.

### Issue #31 — Accessibility and controller-layout evaluation

**Goal:** Evaluate large-target, reachability, high-contrast, and controller
layout variants using the established widget/profile model; implement only the
layout aspects that require no new input semantics.

**Expected files:** accessibility profile defaults/settings, controller-layout
proposal or ADR, validation matrix, documentation.

**Protected systems touched:** None for layout-only work. Any controller input,
movement, or raw-touch modification requires explicit approval and a dedicated ADR.

**Validation strategy:** representative phone/tablet tests, screen-reader/platform
accessibility review where Unity chrome is involved, and controller mapping review
before behavior implementation.

**Stopping point:** validated layout requirements and profile capability; controller
input itself remains a separate approved milestone.
