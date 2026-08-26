# Hollowmere Architecture Review

A lead-architect pass over the current state of the codebase as of
`v0.2.0-mobile-foundation`. Opinionated by design — this is meant to be argued with
in the next planning session, not treated as settled fact.

## Becoming technical debt

- **Two parallel UI rendering surfaces.** ClassicUO's native Gump system (drawn
  through `Batcher2D`) and Hollowmere's native Unity Canvas UI (the joystick, chat
  popup, Action Bar's touch chrome) coexist on the same screen without a unifying
  strategy. Today this is manageable because the two surfaces mostly don't overlap
  spatially. It will stop being manageable the moment a Persistent HUD needs both
  systems to cooperate over the same screen region (docking a native Gump next to
  Unity UI, z-ordering between the two, consistent theming across both). This is
  the single largest piece of debt in the current architecture.
- **~15 engine override files marked "needs manual review."** Classified by size
  and role heuristics only, never actually read. Individually low-risk (mostly
  small diffs), but the whole point of the engine-baseline milestone was replacing
  "trust it, it's just an override" with actual visibility — these entries are the
  visibility work not yet finished.
- **The `.meta`-tracking gap for ~325 still-symlinked files.** Deliberately left
  open during the engine-baseline milestone. Not urgent, but it's a real,
  identified gap in git's ability to guarantee GUID stability across a fresh clone
  for anything not yet on the tracked-override list.
- **`PrimaryActionButtonBootstrap`'s hardcoded top-right position.** Explicitly
  documented as temporary pending a Persistent HUD design, but "temporary" code has
  a way of becoming load-bearing the longer it sits — worth timeboxing.

## Architectural decisions that should be revisited

- **The dual-rendering-surface question above** is the top candidate for a
  deliberate decision (not a default): either commit to a real bridging strategy
  between ClassicUO's Gump system and Unity UI, or commit to migrating one onto the
  other over time. Continuing to add features to both without a stated direction is
  the actual risk, more than either surface being individually wrong.
- **`ItemHold`'s optimistic-clear-then-recover pattern.** The `DenyMoveItem` bug
  (ADR history: #24) was caused by a recovery path that silently stopped working
  for years because it checked the wrong flag. That's a strong signal this general
  pattern — clear state optimistically, trust a recovery packet handler to fix it
  if the server disagrees — is fragile in ways that are easy to get subtly wrong
  and hard to notice until a specific edge case is hit. Worth an explicit audit for
  siblings of this pattern elsewhere in the networking layer, not just trusting
  that this was the only instance.
- **Movement input's Stable/Protected status.** Necessary caution today, but it
  will directly collide with the Tap-to-Move evaluation on the roadmap. That's not
  a reason to relax the protection casually — it's a reason to make sure "protected"
  doesn't quietly become "permanently off the table" without ever having the actual
  conversation.

## Areas where current abstractions are especially strong

- **`GameActionDefinition` as a universal "thing a player can do."** Everything
  from a spell to a pet command to Primary Action's context-sensitive verbs
  expresses itself the same way. This is the single highest-leverage abstraction in
  the codebase — nearly every future interaction feature (Controller Support,
  Player Expression, Combat Modernization) gets easier because this exists.
- **The Primary Action pipeline's enforced one-way dependency** (ADR-003). The
  resolver genuinely doesn't know what a button is. This was worth the extra
  ceremony at build time and is exactly the kind of boundary that pays for itself
  the first time a second consumer shows up.
- **`Diag`'s zero-cost-when-disabled design** (ADR-005). Small, but a good model
  for "permanent infrastructure that doesn't apologize for existing" — it doesn't
  try to be more than it needs to be.
- **`ActionBar` reusing `AnchorManager` instead of inventing parallel layout.** A
  quiet, correct decision that keeps Hollowmere's own UI and native Assistant
  Gumps behaviorally consistent for free.

## Areas where additional abstraction would be harmful

- **A generic UI framework wrapping ClassicUO's Gump system.** There's exactly one
  concrete need (bridge Gump rendering to Unity), not a general problem. Building
  an abstraction layer over it before the dual-rendering-surface question (above)
  is even decided would mean abstracting the wrong boundary.
- **A generic input-device abstraction** ahead of Controller Support actually being
  built. ADR-007 deliberately kept touch semantics concrete (tap, long-press) rather
  than building a gesture framework for gestures nothing needs yet. Controller
  Support is the right moment to revisit this, not before.
- **A plugin/strategy interface for the Action Pipeline** ahead of a second real
  consumer of `PrimaryAction`. The pipeline's reusability claim (ADR-003) is
  currently theoretical; formalizing it into a plugin architecture before it's
  been exercised once would be designing against imagined requirements.

## Things to intentionally leave simple

- **`ActionBar` persistence via `PlayerPrefs`.** Correct for a single-device,
  single-profile use case. Don't build a generic save-system abstraction until
  cloud sync or cross-device profiles are an actual, scoped feature.
- **`Diag`'s code-only category toggles.** No settings UI, no remote configuration.
  Fine for developer use; revisit only if field diagnosis from a live device
  becomes a real, recurring need.
- **`ActionCatalog`'s linear scan** over its action list. Cheap at current scale
  (~400 entries, once per login). Don't index it ahead of evidence that it's slow.
- **HealthBarGump's engine-level, per-Gump positioning.** Fine until Persistent HUD
  gives every screen element a shared positioning model — not worth unifying early.

## Future risks

- **Movement input's protected status vs. the Tap-to-Move roadmap item** — flagged
  above, repeated here because it's a real, foreseeable point of friction between
  "don't touch this" and "the roadmap asks a question this system has to answer."
- **The dual-rendering-surface debt compounding** if Modern Mobile UX / Persistent
  HUD work proceeds without first resolving the strategy question.
- **Diagnostic/root-cause debugging cost scaling with undiscovered native-gesture
  interactions.** #24 found two independent native touch gestures
  (click-and-hold-to-pick-up, click-and-drag-to-pick-up) that Hollowmere's own
  button-driven interaction hadn't accounted for. There may be others like them
  elsewhere in the engine surface that haven't collided with new mobile UI yet.
- **`ItemHold`-pattern siblings** elsewhere in networking, per the audit
  recommendation above.

## Potential engine bottlenecks

- **Whole-repository tooling limits.** CodeDebrief/OpenDeepWiki reliably fails or
  stalls against the full codebase and has to be scoped manually — a real,
  recurring cost on every architecture question that isn't already covered by the
  narrow existing scope.
- **`GameController.cs`'s size and centrality.** Already the largest single diff
  in the engine inventory (537 lines vs. upstream) and the file every touch input
  event passes through. It's a natural hot spot for future touch-dispatch bugs
  simply by virtue of being where all the branching logic lives — worth watching
  as a complexity concentration risk, not necessarily an immediate refactor target.
- **Platform-specific asset-loading branches** (the iOS case-sensitivity fix in
  `UOFileManager.cs`) could multiply ad hoc, per-platform, per-file if a cleaner
  platform-abstraction point isn't established before the second or third such fix
  is needed.
