# Issue #21 — Modern Gameplay Controls: Architectural Audit

Analysis only. Nothing in this document has been implemented. Findings are
based on static code research (Unity MCP was offline for most of this
audit); anywhere the underlying game state wasn't independently confirmed
at runtime, that's noted explicitly rather than assumed.

---

## 1. Architecture Summary

Confirms the three-layer model from the brief against what's actually in
the codebase:

- **Engine (stable, don't modify without approval):** `ClientRunner.cs`,
  networking/packet serialization, `GameScene.cs`/`GameController.cs` (the
  Unity-customized fork of ClassicUO's game loop), and the bulk of
  `Assets/Scripts/ClassicUO/src/**` — much of which are filesystem symlinks
  into `external/ClassicUO/src/...`, i.e. **unmodified upstream ClassicUO
  code**, not MobileUO2-authored. A smaller set of files are *real,
  Unity-specific overrides* of the same names (`GameScene.cs`,
  `GameController.cs`, `WorldMapGump.cs`, `SystemChatControl.cs`,
  `StbTextBox.cs` confirmed as real overrides, not symlinks) — these are
  where MobileUO2-specific input adaptation already lives, and are the
  most appropriate touch points for further adaptation.
- **Input Layer (active development area):** `Assets/Scripts/MobileInput/`
  (`MobileInputController.cs`, `FloatingJoystick.cs` — explicitly
  protected) plus `GameController.UnityInputUpdate()`, which is the single
  chokepoint translating LeanTouch finger data into simulated mouse events
  (`SimulateMouse`) consumed by the untouched Engine layer. **This is the
  key architectural fact for #21: touch input today is not a first-class
  input model, it's mouse emulation.** Any new input method (keyboard
  movement, gamepad, Steam Deck) should follow the same
  translate-to-existing-engine-primitives pattern rather than teaching the
  Engine layer about new input types directly.
- **Presentation Layer:** the Hollowmere UI Foundation from #20A
  (unaffected by this milestone) plus every ClassicUO Gump. Gumps mix
  both content (what a container/spellbook/shop *is*) and interaction
  (mouse-drag thresholds, click/double-click handlers) in the same files
  — there is no separate "input adapter" per Gump today, which is why
  touch support for any given Gump is really "does `GameController`'s
  mouse-simulation happen to be sufficient for this Gump," not a designed
  property.

**Confirmed input methods that exist today:** touch (LeanTouch, full
finger tracking, two-finger tap→right-click, two-finger pinch→zoom, all in
`GameController.cs`), mouse, keyboard (text entry + UI modifier buttons
only). **Confirmed input methods that do not exist at all:** any gamepad/
controller path (no `UnityEngine.InputSystem`, no `Input.GetAxis`, no
`Gamepad` reference anywhere in the project), any keyboard movement
(no WASD/arrow-key handling anywhere in `Game/`), any device-class
detection beyond `Application.isMobilePlatform` (no tablet/Steam
Deck/orientation/screen-size branching anywhere).

---

## 2. Gameplay Interaction Audit

Format per interaction: current implementation → pain points → why it
feels outdated → complexity → impact → regression risk → dependencies →
recommended solution. Complexity/Impact/Risk are **Low/Medium/High**.

### Movement
- **Current:** `MobileInputController` (touch, left-half-of-screen) drives
  `FloatingJoystick.Input`, read every frame by `ClientRunner.Update()`,
  written to `GameScene.JoystickInput`, consumed by `GameScene.Update`
  → `World.Player.Walk(...)`.
- **Scripts:** `MobileInputController.cs`, `FloatingJoystick.cs`,
  `ClientRunner.cs`, `GameScene.cs`.
- **Pain points:** No keyboard movement (WASD/arrows) at all. No gamepad
  analog-stick movement at all.
- **Why outdated:** Every modern handheld PC (Steam Deck, ROG Ally, Legion
  Go) has a physical analog stick; forcing an on-screen virtual joystick
  on a device with a real one is worse than doing nothing.
- **Complexity:** Medium — needs a second, parallel input source (keyboard
  and/or gamepad left-stick) feeding the same `Vector2` `ClientRunner`
  already reads, not a movement-system rewrite.
- **Impact:** High — every session, every moment.
- **Regression risk:** Medium — touches `ClientRunner` (explicitly
  protected), but only additively (a new input source next to the
  existing joystick read, not a replacement).
- **Dependencies:** None blocking; establishes the pattern later gamepad
  work reuses.
- **Recommended solution:** Add an input-source abstraction that can feed
  `ClientRunner`'s existing joystick-input slot from keyboard WASD and/or
  a gamepad stick, gated by device detection, without touching
  `FloatingJoystick` itself.

### Camera
- **Current:** No dedicated camera class — a fixed isometric view, scaled
  via `GameScene.Scale`. Zoom exists (`GameScene.ZoomIn/ZoomOut`, 1 step
  per call) but is wired only to two-finger pinch in `GameController`,
  gated by an `EnableMousewheelScaleZoom` preference whose name implies a
  scroll-wheel path was intended — **no scroll-wheel handler was found in
  the codebase**, which reads as either dead/misnamed config or a gap
  worth confirming at runtime before building anything.
- **Scripts:** `GameScene.cs`, `GameController.cs`.
- **Pain points:** No mouse-wheel zoom despite a preference implying one
  exists. No keyboard or gamepad zoom.
- **Why outdated:** Zoom-by-pinch-only means desktop mouse users and any
  future gamepad users have no zoom control at all.
- **Complexity:** Low — `ZoomIn`/`ZoomOut` already exist; this is wiring
  new triggers to an existing call, not new zoom logic.
- **Impact:** Medium.
- **Regression risk:** Low-Medium — `GameScene`/`GameController` are
  Engine-layer (treat as needing the same approval as explicitly-listed
  protected files even though not named individually), but the change is
  additive.
- **Dependencies:** Should confirm at runtime (with Unity MCP now back)
  whether `EnableMousewheelScaleZoom` is genuinely dead code before
  building on top of it.
- **Recommended solution:** Wire mouse-wheel and gamepad trigger/bumper to
  the existing `ZoomIn`/`ZoomOut`, after confirming the current state of
  the mousewheel preference at runtime.

### Targeting
- **Current:** Stock `TargetManager`/`SelectedObject` (both unmodified
  upstream). Hit-testing is single-point, mouse-coordinate-based;
  `ItemGump` additionally does per-pixel alpha hit-testing. Touch reaches
  this system purely by `GameController` converting one finger into a
  simulated mouse point/click — identical downstream path to a real mouse.
- **Scripts:** `TargetManager.cs`, `SelectedObject.cs`, `GameScene.cs`,
  `GameController.cs`.
- **Pain points:** No touch-specific affordance at all — no larger target
  reticle, no disambiguation for overlapping/stacked objects, no hover
  preview (touch has none). "Target last" exists in the engine
  (`TargetManager.TargetLast`/`LastTargetInfo`) but is reachable only via
  macro/script, not any Gump/button.
- **Why outdated:** Precision mouse-point targeting on a crowded battle
  screen, with no hover feedback, is the single hardest touch interaction
  in the game today.
- **Complexity:** Medium for a first pass (expose `TargetLast` as a
  button); High for real touch-target-assist (nearest-enemy snapping,
  bigger hit boxes).
- **Impact:** High — combat-critical.
- **Regression risk:** Medium — Engine-layer, but exposing an existing
  engine capability via a new additive UI button is low-risk; changing
  the hit-test loop itself is higher-risk.
- **Dependencies:** Benefits from the Hotbar work (§Hotbars) landing
  first, since "Target Last" naturally belongs on the same button bar.
- **Recommended solution:** Ship an on-screen "Target Last" button calling
  the existing `TargetManager` API before attempting any hit-test/reticle
  changes — highest-value, lowest-risk slice of this problem.

### Inventory
- **Current:** The player's backpack is just a `ContainerGump` like any
  other — see Containers below; no separate inventory-specific system
  exists.
- **Scripts:** Same as Containers.
- **Pain points / Why outdated / Recommended solution:** Identical to
  Containers — treat as the same interaction, not a separate feature.
- **Complexity / Impact / Risk:** See Containers.
- **Dependencies:** None beyond Containers.

### Containers
- **Current:** `ContainerGump` positions items at server-sent pixel
  coordinates; pickup/drag uses `ItemGump`'s `Mouse.LButtonPressed` +
  5px-drag-threshold (`Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS`) to
  distinguish click from drag; window minimize/restore uses the same
  threshold. No per-window resize (only a global `ContainersScale`
  preference); no found cap on simultaneously open container windows.
- **Scripts:** `ContainerGump.cs`, `ItemGump.cs`, `Constants.cs`.
- **Pain points:** Small item icons at native/desktop pixel density are
  hard to grab precisely on touch. Multiple overlapping container windows
  on a phone-sized screen have no snapping/tiling help. The 5px drag
  threshold itself is not the problem (trivially exceeded by a real
  finger) — icon size and window management are.
- **Why outdated:** UI built for ~1024×768 mouse precision, unmodified for
  touch icon density or small-screen window layout.
- **Complexity:** Medium — a touch-friendly display mode (bigger spacing/
  icon size, or a list-view alternative on small screens) is additive; it
  does not require touching the `ItemHold` drag transaction protocol.
- **Impact:** Critical — used in essentially every play session, every
  loot/trade/craft interaction.
- **Regression risk:** Low-Medium — `ContainerGump`/`ItemGump` are Engine-
  layer and need approval, but a new opt-in touch layout mode is additive
  and doesn't change the underlying pickup/drop protocol.
- **Dependencies:** Shares `ItemHold` with Dragging, Paperdolls, and
  Looting — any change to the shared drag system itself (not recommended
  here) would need to be validated across all four.
- **Recommended solution:** A profile-toggle "touch container layout"
  (larger icon grid / more spacing) rather than changing pickup mechanics.

### Looting
- **Current:** No separate system — corpses are `ContainerGump` with
  `Graphic == 0x0009` special-cased for empty-corpse auto-close suppression
  and bad-layer filtering. Looting = dragging items out of a container,
  identical to Containers.
- **Scripts:** `CorpseManager.cs` (serial-tracking only, no loot logic),
  `ContainerGump.cs`.
- **Pain points:** Whether a "loot all" shortcut exists was **not
  confirmed** by this audit — worth a targeted follow-up check before
  scoping any loot-specific work, since if one already exists it may
  already solve most of the friction here.
- **Why outdated:** Same as Containers — this is the same interaction
  wearing a different graphic.
- **Complexity / Impact / Risk:** Same as Containers, once "loot all"
  existence is confirmed.
- **Dependencies:** Containers.
- **Recommended solution:** Fold into the Containers work rather than
  scoping separately; confirm loot-all status first.

### Spellcasting
- **Current:** `SpellbookGump` — double-click a spell icon → `TargetManager`
  cursor-target flow. A `CastSpellsByOneClick` preference exists but is
  wired only to Assistant hotkey/macro buttons, **not** the spellbook
  itself. `TargetLast` exists in the engine, reachable only via
  macro/script today.
- **Scripts:** `SpellbookGump.cs`, `TargetManager.cs`, `MacroManager.cs`.
- **Pain points:** Two-step double-click-then-precision-target under
  combat time pressure; the one-click-cast preference doesn't actually
  apply to the spellbook, which reads like an inconsistency/gap.
- **Why outdated:** Small double-click targets plus mouse-precision
  targeting is the hardest possible combination for touch, in the game's
  most time-pressured moment.
- **Complexity:** Low for exposing `CastSpellsByOneClick` in the spellbook
  itself + a "Target Last" button; higher for anything beyond that.
- **Impact:** High — core loop for every spellcaster build.
- **Regression risk:** Low — both changes expose existing engine
  capability through new/extended UI, no protocol changes.
- **Dependencies:** Shares "Target Last" exposure with Targeting.
- **Recommended solution:** Make `CastSpellsByOneClick` actually govern
  `SpellbookGump`, and ship the same "Target Last" button used for
  Targeting.

### Skills
- **Current:** `SkillGumpAdvanced` — 20px-tall rows, small use-button and
  lock-status icon, single-click (not double) to use a skill. Drag-out to
  a floating `SkillButtonGump` already exists.
- **Scripts:** `SkillGumpAdvanced.cs`, `MacroManager.cs`.
- **Pain points:** 20px rows and small icons are well under comfortable
  touch-target size (~44pt convention).
- **Why outdated:** Desktop-density list UI, unmodified for touch.
- **Complexity:** Low — a touch-mode row-height/icon-size increase is
  presentation-only.
- **Impact:** Medium — frequent for crafting/gathering playstyles, less
  for pure combat.
- **Regression risk:** Low — purely visual/hit-box sizing, existing
  drag-out-to-button flow is untouched.
- **Dependencies:** None.
- **Recommended solution:** Increase row height/touch target size in a
  touch-mode variant of this Gump; the existing drag-to-`SkillButtonGump`
  flow already gives players a good path to a personal skill hotbar.

### Dragging
- **Current:** Single shared static `ItemHold` class used identically by
  Containers, Paperdolls, Trading, hotbar counters, and the world map —
  not duplicated per window. Pickup trigger is `Mouse.LButtonPressed` held
  across frames + the same 5px drag threshold. Touch already reaches this
  via `GameController`'s single-finger→simulated-mouse translation.
- **Scripts:** `ItemHold.cs`, `ItemGump.cs`, `PaperDollInteractable.cs`,
  `GameController.cs`.
- **Pain points:** The drag *protocol* already works on touch (via
  simulation); the friction is icon-size precision (see Containers/
  Paperdolls), not the drag mechanism itself.
- **Why outdated:** Not outdated as a mechanism — it's a reasonable shared
  system. The complaint belongs to the screens that consume it, not to
  `ItemHold`.
- **Complexity:** Low for target-size fixes on top; Medium-High if
  pursuing a genuinely different interaction (e.g. tap-to-pick-up /
  tap-to-place as a one-handed alternative to sustained-drag).
- **Impact:** High — universal.
- **Regression risk:** Medium — `ItemHold` is consumed by ~8 different
  Gumps; any change to the core protocol needs regression-testing across
  all of them, which is exactly why the recommendation below avoids
  touching it.
- **Dependencies:** Containers, Paperdolls, Looting all inherit from this.
- **Recommended solution:** Don't touch `ItemHold`. Fix consumers (icon
  size in Containers/Paperdolls) first; only consider an alternate
  tap-to-pick-up interaction mode as later, opt-in, additive work.

### Context menus
- **Current:** Server-driven `PopupMenuGump`, triggered strictly by
  right-click (`UIManager.OnRightMouseButtonUp`). **Touch equivalent
  already exists**: `GameController` detects a two-finger tap on the same
  control and synthesizes a right-click.
- **Scripts:** `PopupMenuGump.cs`, `GameActions.cs`, `UIManager.cs`,
  `GameController.cs`.
- **Pain points:** Two-finger-tap-on-a-small-object is fiddly and has zero
  discoverability/visual affordance — nothing teaches the player this
  gesture exists.
- **Why outdated:** Modern touch convention for a context menu is
  long-press, not two-finger tap; the current gesture is a mouse-emulation
  artifact, not a touch-first design choice.
- **Complexity:** Low — add a long-press detector (LeanTouch already
  provides finger-duration data) that calls the same
  `SimulateMouse(rightDown/rightUp)` sequence already used for two-finger
  tap.
- **Impact:** Medium-High — used for a large fraction of world/NPC/item
  interactions.
- **Regression risk:** Medium — `GameController` is Engine-layer, but the
  change is additive alongside the existing two-finger-tap path (which
  should be kept, not replaced, for players who've learned it).
- **Dependencies:** None.
- **Recommended solution:** Add long-press-to-context-menu as a second,
  parallel gesture to the existing two-finger tap.

### Paperdolls
- **Current:** Drag-to-equip/unequip via the same `ItemHold` system.
  Hit-areas equal the *rendered equipment art bounds* — for small items
  (rings, earrings) that's a small texture region, not a generous slot
  rectangle.
- **Scripts:** `PaperdollGump.cs`, `PaperDollInteractable.cs`.
- **Pain points:** Precision-dependent hit targets on small equipment art.
- **Why outdated:** Same class of problem as Containers/Skills — desktop
  pixel-precision UI unmodified for touch.
- **Complexity:** Low-Medium — add a padded invisible hit-box overlay per
  slot, sized independently of the equipment art itself.
- **Impact:** Medium — equipping is periodic, not moment-to-moment, but
  frustrating when it fails.
- **Regression risk:** Low-Medium — additive hit-box padding doesn't
  change equip logic or the shared `ItemHold` protocol.
- **Dependencies:** Dragging/`ItemHold` (consumed, not modified).
- **Recommended solution:** Padded per-slot hit-boxes independent of
  equipment-art silhouette.

### NPC interaction
- **Current:** **Not independently researched in this pass** — general
  NPC dialogue/interaction wasn't covered by the research agents (only
  vendor/shop interaction was). Expected to largely inherit the Context
  Menus and Targeting/Object Selection findings (NPC interaction is
  primarily context-menu- and click-driven in stock UO), but this should
  not be assumed without a dedicated look.
- **Scripts:** Unknown — needs follow-up research.
- **Recommended solution:** Scope a short follow-up research pass before
  committing to any NPC-interaction-specific work; likely folds into
  Context Menus rather than being a standalone milestone.

### Vendor interaction
- **Current:** `ShopGump` — double-click to add an item to the buy/sell
  list; quantity adjusted via ±1 buttons with hold-to-repeat (500-tick
  delay, then accelerating), which relies on a continuous `MouseOver`
  while held — **whether this fires reliably under the touch-simulated
  motion event was flagged as untested/unconfirmed** by the research, not
  verified broken. Window resize uses a drag-handle. Stack-splitting
  elsewhere (`SplitMenuGump`) already has both a slider *and* a text-box
  fallback.
- **Scripts:** `ShopGump.cs`, `SplitMenuGump.cs`.
- **Pain points:** Buying large quantities (e.g. 100 arrows) via ±1
  stepper is slow even in the best case, and the hold-to-repeat reliability
  under touch is an open question.
- **Why outdated:** A precise numeric-entry pattern already exists
  elsewhere in the same codebase (`SplitMenuGump`) and simply wasn't
  reused here.
- **Complexity:** Low — add the same slider+textbox pattern already proven
  in `SplitMenuGump` to `ShopGump`'s quantity control.
- **Impact:** Medium — economic gameplay, frequent for trade-focused play.
- **Regression risk:** Low-Medium — isolated to one Gump, reuses an
  already-shipped pattern.
- **Dependencies:** None; should first confirm at runtime whether the
  hold-to-repeat actually misbehaves on touch before deciding this is
  urgent vs. merely worth doing.
- **Recommended solution:** Reuse `SplitMenuGump`'s slider+textbox pattern
  for shop quantity entry.

### Corpses
- **Current:** Same system as Looting/Containers — `CorpseManager` only
  tracks serial mapping, no loot-specific UI.
- **Scripts / Pain points / Recommendation:** Identical to Looting.
- **Complexity / Impact / Risk:** Same as Containers.
- **Dependencies:** Containers, Looting.

### Chat
- **Current:** `SystemChatControl` (real override) + `StbTextBox` (real
  override) already calls `UnityEngine.TouchScreenKeyboard.Open(...)` —
  mobile virtual keyboard support **already exists**. No quick-phrase/
  preset-message/emote-shortcut layer exists on top of it.
- **Scripts:** `SystemChatControl.cs`, `StbTextBox.cs`, `JournalGump.cs`
  (history display only).
- **Pain points:** Typing full sentences on a phone keyboard mid-combat
  is impractical; nothing in the codebase offers a faster alternative.
- **Why outdated:** Not outdated as text entry (that part's solved) — the
  gap is the absence of any quick-response affordance modern mobile games
  provide.
- **Complexity:** Low — a preset-phrase/quick-command picker calling the
  same existing `GameActions.Say`/`PChatMessageCommand` path.
- **Impact:** Medium — social/coordination QoL, not core combat loop.
- **Regression risk:** Low — purely additive new UI, no changes to text
  entry or networking.
- **Dependencies:** None.
- **Recommended solution:** Ship a quick-phrase picker on top of the
  already-working text entry, not a text-entry replacement.

### Hotbars
- **Current:** `AssistantHotkeyButtonGump`/`AssistantMacroButtonGump`
  already exist at a genuinely touch-reasonable 88×44px, single-click
  (when `CastSpellsByOneClick` is set) or double-click otherwise, gated
  by `ENABLE_INTERNAL_ASSISTANT` — **confirmed active on Android/iPhone
  builds already**. The gap is entirely in *arrangement*: buttons must be
  manually placed one at a time from the Assistant editor at the mouse
  cursor position; there is no auto-populated grid/bar.
- **Scripts:** `AssistantHotkeyButtonGump.cs`, `AssistantMacroButtonGump.cs`,
  `Assistant.cs`.
- **Pain points:** No real "hotbar" exists — just individually-placed
  buttons, which is a poor mobile-authoring experience even though the
  buttons themselves are already well-sized for touch.
- **Why outdated:** The hard part (a touch-sized, functional trigger
  button) is already solved; the missing part (an organized container for
  many of them) is comparatively simple and hasn't been built.
- **Complexity:** Medium — a hotbar container Gump that auto-arranges
  existing button instances in a grid/row is compositional, not a
  rewrite of the trigger mechanism.
- **Impact:** High — this is how efficient/power-user play happens,
  especially in combat.
- **Regression risk:** Low-Medium — Assistant subsystem, additive
  container around already-shipped, already-working buttons.
- **Dependencies:** Benefits from Targeting's "Target Last" button landing
  on the same bar.
- **Recommended solution:** Build an auto-arranging hotbar container
  around the existing hotkey/macro button Gumps — don't rebuild the
  buttons themselves.

### Macros
- **Current:** Text/script-based authoring (`Interpreter.cs`,
  `Commands.cs`, `Lexer.cs`) via `ScriptTextBox.cs` — a scripting-language
  text editor, no visual macro builder.
- **Scripts:** `Assets/Scripts/Assistant/Scripts/*.cs`.
- **Pain points:** Authoring a macro requires typing script syntax on a
  phone keyboard — a genuinely awkward mobile experience, distinct from
  *using* a macro (already fine via Hotbars).
- **Why outdated:** Desktop power-user tooling with no touch-authoring
  concession.
- **Complexity:** High — a real visual macro builder is a substantial UI
  feature, not a small fix.
- **Impact:** Medium-High for power users, but a smaller slice of the
  player base than Movement/Targeting/Containers.
- **Regression risk:** Low — purely additive authoring UI that emits the
  same script text the interpreter already consumes; doesn't touch
  execution.
- **Dependencies:** Hotbars (macros are triggered the same way once
  authored).
- **Recommended solution:** Defer past the first milestone; worth doing,
  but the effort-to-player-impact ratio is worse than everything above.

### Map usage
- **Current:** `WorldMapGump` (real override) — zoom is 10 discrete
  mouse-wheel steps; pan requires Alt+left-drag or middle-mouse-drag.
  **Confirmed zero pinch/touch code anywhere in this file** via direct
  grep, despite the pinch-zoom pattern already existing and shipping
  elsewhere in the exact same codebase (`GameController`'s two-finger
  pinch for world-view zoom).
- **Scripts:** `WorldMapGump.cs`, `GameController.cs` (as the existing
  pattern to copy).
- **Pain points:** The world map is effectively unusable via touch alone
  today — no pan, no zoom, both gated behind desktop-only input
  (Alt-modifier, middle-mouse-button) that doesn't exist on touch.
- **Why outdated:** This is the starkest, most demonstrable gap in the
  entire audit — a fully solved pattern (pinch-to-zoom) sits unused two
  files away from where it's needed.
- **Complexity:** Low-Medium — copy the already-proven `LeanGesture`
  pinch pattern from `GameController` into `WorldMapGump`, add
  single-finger drag-to-pan.
- **Impact:** High for exploration/travel gameplay, and disproportionately
  high-visibility (an unusable map is an obvious, easily-reproduced bug
  report waiting to happen).
- **Regression risk:** Low — `WorldMapGump` is already a MobileUO2-owned
  real override, not upstream/protected code, and the pattern being
  copied is already shipped and proven elsewhere in this project.
- **Dependencies:** None.
- **Recommended solution:** **Best risk/impact ratio in this entire
  audit.** Port the existing `GameController` pinch-zoom pattern into
  `WorldMapGump`, add single-finger pan.

### Object selection (general precision)
- **Current:** No project-wide minimum touch-target standard exists
  anywhere. Confirmed small targets: 20px skill rows, native-gump-art-
  sized spell icons (unmeasured), small paperdoll equipment hit-boxes.
  Confirmed *good* targets: 44×88px Assistant hotkey/macro buttons, a
  44px grid used internally by those same buttons.
- **Scripts:** Cross-cutting — every Gump.
- **Pain points:** This is a **root cause**, not a standalone feature —
  it's the underlying reason Skills, Paperdolls, and parts of Containers
  feel imprecise on touch.
- **Why outdated:** Original ~1024×768 mouse-era pixel dimensions carried
  through unmodified in most places.
- **Complexity:** Medium for a shared solution (a touch-target-padding
  utility/profile-driven scale factor); High if attempted as a single
  project-wide sweep.
- **Impact:** Medium-High — compounds across everything else in this
  audit rather than being independently high-impact.
- **Regression risk:** Medium — touches many individual hit-test call
  sites cumulatively, even though each is individually small.
- **Dependencies:** This is exactly the kind of systemic fix the Root
  Cause Policy says to prefer *after* a pattern is proven on 2-3 concrete
  screens (Skills, Paperdolls), not before — building a shared utility
  speculatively, before those concrete fixes exist to generalize from,
  risks the wrong abstraction.
- **Recommended solution:** Fix Skills and Paperdolls individually first
  (already scoped above); only then evaluate whether a shared
  touch-target utility is justified by the pattern actually observed.

### Mobile ergonomics
- **Current:** Already the most mature area of the whole audit —
  `FloatingJoystick`, LeanTouch integration, opacity/dead-zone settings,
  left-handed mode, per-preference customization all exist and are
  explicitly protected/stable.
- **Recommended solution:** No new work; the only risk here is regressing
  it while building the items above. Every recommendation in this
  document is written to avoid touching this system.

### Tablet ergonomics
- **Current:** **Confirmed zero tablet-specific code** — no
  `Screen.orientation`, no size-breakpoint branching, nothing beyond the
  binary `Application.isMobilePlatform` check. Tablets today just get
  whatever layout phones or desktop get, scaled.
- **Pain points:** No use of the extra screen real estate tablets offer
  (e.g. showing hotbar + inventory + map without overlap).
- **Why outdated:** Treating "mobile" as one undifferentiated category
  ignores that a 10" tablet and a 6" phone have very different usable
  space.
- **Complexity:** Medium — genuine responsive layout work.
- **Impact:** Medium.
- **Regression risk:** Low — Presentation-layer, additive.
- **Dependencies:** Should follow the Input Layer work above, not precede
  it — a responsive layout is more valuable once more interactions
  (targeting, hotbars, map) are already touch-solid, so there's more
  worth arranging well.
- **Recommended solution:** Defer until after the first milestone's
  Input Layer fixes land.

### Steam Deck ergonomics
- **Current:** **Confirmed zero native support** — no gamepad input
  anywhere in the project. Steam Deck's own OS-level Steam Input can
  *remap* trackpads/gyro/buttons to emulate mouse or touch, which might
  produce a partially-working experience today, but this is an
  unconfirmed, unofficial fallback, not a supported path — not verified
  in this audit.
- **Pain points:** No first-class gamepad path exists for any interaction
  in this document.
- **Why outdated:** This device class is named explicitly in the
  project's own priorities and has literally zero bespoke support today.
- **Complexity:** High — a real gamepad input layer needs analog-stick
  movement, face-button targeting/context-menu triggers, trigger-based
  zoom, and a virtual-cursor mode for precise UI hit-testing (menus,
  containers, spellbook) where a d-pad/stick can't substitute for a
  pointer.
- **Impact:** High if this device class matters to the project (it's
  named twice), but it is the single largest, highest-effort gap
  identified in this entire audit.
- **Regression risk:** Medium — can be built as a wholly new, additive
  `GamepadInputController` parallel to `MobileInputController`, feeding
  the same `ClientRunner`/`SimulateMouse`-style entry points rather than
  replacing anything — but wiring into `ClientRunner` still requires
  explicit approval since it's protected.
- **Dependencies:** Directly reuses the Movement input-source pattern and
  the Targeting/virtual-cursor concept — should follow those, not precede
  them.
- **Recommended solution:** Defer to a dedicated follow-up milestone
  (§Roadmap, #21C) after the additive-input-source pattern is proven via
  simpler Movement/Camera work.

### Controller ergonomics
- **Current:** Identical finding to Steam Deck — zero support. The
  distinction: ROG Ally/Legion Go are effectively Windows handhelds, so
  they don't get Steam Input's automatic mouse/touch emulation the way
  Steam Deck does. A controller-less session on those devices today would
  fall back entirely to the touchscreen path (which does work, same as
  phone/tablet).
- **Scripts / Complexity / Impact / Risk / Dependencies:** Same as Steam
  Deck — this is the same feature (a gamepad input layer), not a separate
  one; both entries exist here because the brief asked for them
  separately.
- **Recommended solution:** Build one gamepad input layer that serves
  Steam Deck, ROG Ally, Legion Go, and any standard Xbox/PlayStation
  controller identically — do not scope these as separate features.

---

## 3. Prioritized Roadmap

Ranked by **high impact + low regression risk** first, per the stated
philosophy (better interaction over better appearance; never redesign
what can first be made easier to use).

**Tier 1 — do first (low risk, no protected-file changes, high visible impact):**
1. Map usage — pinch/pan for `WorldMapGump` (best risk/impact ratio found)
2. Context menus — long-press gesture, additive to existing two-finger tap
3. Spellcasting — one-click-cast + "Target Last" button
4. Vendor interaction — numeric quantity entry (reuse `SplitMenuGump` pattern)
5. Skills — touch-target sizing pass
6. Chat — quick-phrase picker

**Tier 2 — next (some Engine-layer touch, still additive, needs approval to proceed):**
7. Hotbars — auto-arranging container around existing buttons
8. Containers/Inventory/Looting/Corpses — touch-friendly layout mode
9. Paperdolls — padded hit-boxes
10. Camera — wheel/gamepad zoom hookup (after confirming mousewheel-pref status)
11. Targeting — beyond "Target Last": bigger hit boxes / disambiguation

**Tier 3 — larger lifts, sequenced after Tier 1/2 patterns are proven:**
12. Movement — keyboard + gamepad input source (touches `ClientRunner`, protected)
13. Object selection — shared touch-target utility, only once justified by Tier 1/2 fixes
14. Tablet ergonomics — responsive layout, benefits from Tier 1/2 landing first
15. Macros — visual authoring on top of existing script engine

**Tier 4 — dedicated follow-up milestone, largest scope:**
16. Steam Deck / Controller ergonomics — full gamepad input layer, reusing the
    input-source pattern established in Movement (#12) and the virtual-cursor
    concept implied by Targeting (#11)

**Needs research before scoping, not yet estimated:**
- NPC interaction (dedicated follow-up pass recommended)
- Looting "loot all" — confirm existence/absence before scoping

---

## 4. Recommended First Milestone

### Issue #21A — "Touch-Complete the Gaps"

Bundle the five Tier-1 items that share a property: each is an isolated,
additive fix to a single Gump/system, exposes engine capability that
already exists rather than building new mechanics, and **touches none of
the explicitly protected files** (`ClientRunner`, `FloatingJoystick`,
Mobile Input V2, LeanTouch integration, Networking, packet serialization):

1. `WorldMapGump` pinch-to-zoom + drag-to-pan
2. Long-press → context menu (parallel to existing two-finger tap)
3. Spellbook one-click-cast + "Target Last" button
4. `ShopGump` numeric quantity entry
5. `SkillGumpAdvanced` touch-target sizing

Why this bundle specifically: it's the highest-impact, lowest-risk subset
identified, it requires zero approval-gated changes to protected systems,
and completing it validates the "expose/extend existing engine capability
via additive UI" approach before Tier 2/3 work needs to touch anything
protected. Recommend confirming two open questions at runtime (with Unity
MCP back online) before or during #21A: whether
`EnableMousewheelScaleZoom` is dead code, and whether a "loot all"
affordance already exists.

**Not started. Awaiting approval before any implementation begins**, per
the milestone's own instructions.
