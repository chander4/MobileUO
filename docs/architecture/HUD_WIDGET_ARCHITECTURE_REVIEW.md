# HUD Widget Architecture Review

**Scope:** final architecture review before any Issue #25 implementation.

## Strengths

- The widget model correctly treats layout participation as independent of rendering technology. This preserves the durable Gump/Unity boundary instead of hiding a Unity migration inside the word “widget.”
- The proposed host owns shared concerns—safe geometry, profile selection, lifecycle, and Edit Mode—without owning action or world logic. That matches the existing separation of `PrimaryActionResolver`, `PrimaryActionDefinitionFactory`, and `GameActionDefinition`.
- Treating an Action Bar and its `AnchorManager` group as one atomic Gump widget is the right incremental bridge. It preserves an established primitive rather than replacing it with a global pixel layout system.
- Runtime/Edit Mode input ownership directly addresses the repository's known hybrid-input risk: a layout gesture cannot also become a Gump action.
- Variant fallback supports phones, tablets, portrait, and landscape without making every widget solve device geometry itself.

## Weaknesses found

1. The original widget ID proposal called IDs stable but did not state whether they are permanent persistence API or how renames/removals migrate.
2. `HudWidgetState` combined host-owned placement with optional widget settings, making the host a future interpreter of feature-specific configuration.
3. The original document named a registry but did not require it as the sole discovery boundary, leaving room for ad hoc bootstrap registration.
4. The host/presenter vocabulary could invite a generic controller layer without a demonstrated lifecycle problem.
5. The distinction between a Unity shortcut and the game-state Gump it opens needed to be explicit to prevent renderer-boundary leakage.

## Risks

- **ID churn:** tying identity to class names, labels, or renderer names would corrupt imported and older profiles. ADR-008 makes IDs permanent and migration explicit.
- **Configuration leakage:** a catch-all settings blob can turn the host into a feature service. Widget-owned, versioned settings contain this risk.
- **Profile complexity:** live inheritance seems convenient but makes deletion, import, migration, and “why did this move?” behavior expensive to explain.
- **Controller inflation:** adding a controller per widget would copy the action pipeline's separation without solving the same problem.
- **Input competition:** host-managed dragging must not coexist with legacy Gump dragging for a migrated widget.
- **Composite widgets:** a widget that spans renderer kinds must be modeled as coordinated widgets or a chrome shell plus a Gump widget, never as one renderer reaching into the other's internals.

## Recommended changes

The review recommends only these minimal refinements, incorporated in the architecture document and locked by ADR-008:

1. Permanent namespaced IDs, aliases, deprecation tombstones, and one-way migration; IDs are not semantic-versioned strings.
2. Separate `HudWidgetState` (layout) from `HudWidgetSettings` (widget-owned configuration).
3. Make `HudWidgetRegistry` mandatory and permit categories only as editor metadata; defer package mechanics.
4. Reject runtime profile inheritance; provide copy-on-create and existing orientation/device fallback instead.
5. Reject a generic controller layer; introduce a narrowly scoped controller only after a concrete, reusable lifecycle/state problem proves the need.
6. Clarify that shortcuts are Unity app chrome while opened game-state windows remain Gumps, with no shared private internals.

## Items intentionally rejected

- Unity-only HUD or Gump replacement.
- Global HUD ownership by `AnchorManager`.
- Universal widget controllers.
- Live parent/child profile inheritance.
- Widget packages/plugins and dynamic discovery.
- Nested docking, tabs, window-manager behavior, cloud sync, and arbitrary cross-renderer layering in the first foundation.

## Renderer-boundary assessment

The architecture conforms to the current permanent rule: game-state UI stays in ClassicUO Gumps; application chrome stays in Unity UI. The logical layout model is renderer-neutral, but it does not render or read game state. The only potential leakage was ambiguity around shortcuts; ADR-008 resolves it by treating a shortcut and the Gump it opens as separate widgets. No Unity view may render a duplicate health/inventory/spellbook state, and no Gump may host Unity content.

## Long-term extensibility assessment

The foundation supports health, buffs, party, minimap, inventory, spellbook, pet commands, controller prompts, accessibility widgets, and streamer overlays so long as each declares one authoritative renderer and uses an adapter rather than a host special case. Health, buffs, party, inventory, spellbook, and pet command state remain Gump candidates. Edit controls, accessibility chrome, controller prompts, and streamer controls are Unity candidates. A minimap or composite surface must be classified before implementation; the architecture deliberately makes that an ADR gate rather than guessing.

The remaining weakness is not structural but procedural: every new widget needs a renderer-ownership and input-ownership review. That is preferable to a premature taxonomy that would misclassify future designs.

## Complexity audit

**Appropriately simple:** one registry, host, layout service, profile model, and two presenter families. The existing action pipeline remains the action domain model; the HUD does not wrap it.

**Under-designed before review:** persistent identity, widget-settings ownership, and profile-copy rules.

**Over-engineered if added now:** controllers, package loading, live inheritance, constraint solvers, arbitrary z-order, and nested docking. None solve a current shipped-widget problem.

## Confidence assessment

**High confidence** in the rendering boundary, host/presenter split, atomic Gump-group approach, and full-snapshot profiles. These directly follow existing ADR-002 through ADR-007 and tested Action Bar/Primary Action patterns.

**Moderate confidence** in the exact layout geometry and docking ergonomics. The data model is sufficient to begin with one widget, but real device validation must determine snap tolerances, safe-area policy, and the first dock-zone details.

**Intentionally deferred confidence** for controller input, complex composite widgets, nested docking, cloud sync, and third-party packages; no design evidence exists yet to justify them.

## Implementation readiness

Ready to begin **only the narrow Issue #25 foundation** after its scope is approved: registry, versioned full-snapshot profile model, state/settings split, variant selection, storage boundary, and no-op migration from existing defaults.

It is not approval for Edit Mode, primary-action migration, joystick changes, global docking, or renderer/engine changes. Those remain later, separately validated milestones.
