# The Hollowmere Design Bible

Visual source of truth for Hollowmere. Every future screen — Login, Server
Browser, Character Select, Inventory, Paperdoll, Spellbook, Settings, Chat,
Journal, Maps — is built against this document, not against taste-of-the-day.
If a design decision isn't here, it doesn't exist yet; add it here before
building it.

This is a design document. No code changes accompany it. It supersedes the
placeholder colors/names shipped in #20A's `HollowmereTheme` — implementing
this palette is future work (see *Implementation Guidance* at the end).

---

## 1. Design Philosophy

**What should Hollowmere feel like?**

You are holding a well-traveled journal, bound in dark leather, its pages
gone the color of old parchment, its margins inked with a steady hand by
candlelight. The world outside the page is vast, half-remembered, and a
little dangerous. The UI is the journal — never the landscape it describes.

Six words govern every decision:

- **Ancient.** Nothing here was manufactured yesterday. Materials show
  wear: leather creases, metal tarnishes, ink fades at the edges.
- **Warm.** Candlelight, not fluorescent light. Even the dark theme is a
  *warm* dark — obsidian and old leather, never cold slate-gray.
- **Legible.** Atmosphere never wins against readability. A player
  squinting at a spell name mid-fight is a design failure, not a mood.
- **Quiet.** The UI defers to the world. No component should be louder
  than the thing it lets you do.
- **Tactile.** Every surface reads as a material — leather, brass,
  parchment, stone — never as a flat vector rectangle with a drop shadow.
- **Earned, not decorated.** Ornamentation marks importance (a title
  screen, a legendary item) rather than being sprinkled everywhere. A
  settings toggle does not need a filigree border.

If a screen could be reskinned into a food-delivery app just by swapping
the icon set, it has failed this philosophy, regardless of how "clean" it
looks.

---

## 2. Mood Boards

### Direction A — Ancient Kingdom
Heraldic. Blue-and-gold, formal borders, castle stonework, wax seals,
crests. The visual language of a royal decree.

- **Pros:** Timeless, instantly reads as "fantasy kingdom," directly
  channels Ultima Online's Britannia heritage — a natural fit for a client
  descended from it.
- **Cons:** Risks reading as generic medieval-fantasy wallpaper (every
  RPG has a crest and a banner). Formality can feel cold and closed-off
  rather than adventurous. Doesn't especially suit mobile — heraldic
  frames are ornament-heavy and expensive to keep legible at small sizes.

### Direction B — Arcane Explorer
Mysterious. Deep violets and teals, floating runes, glowing sigils, the
UI of someone who reads dead languages for a living.

- **Pros:** Distinctive, magic-forward, gives Spellbook/Journal screens a
  natural home. "Hollow" in the name supports an otherworldly, half-seen
  read.
- **Cons:** Overused in modern fantasy UI (Diablo, most gacha RPGs) —
  harder to make ownable rather than derivative. Risks prioritizing glow
  effects over legibility, which conflicts with philosophy point 3.

### Direction C — Traveler's Journal
Intimate. Leather-bound, ink-on-parchment, hand-drawn maps, a compass
rose, a wax seal on a folded letter. The UI of someone *in* the world,
not looking down on it from a throne.

- **Pros:** Most distinctive and ownable of the three — few competitors
  use "handwritten journal" as their core metaphor rather than a one-off
  loading-screen flourish. Ties directly to the name Hollowmere (a place
  you'd write home about). Scales naturally from mobile (a journal fits
  in one hand) to desktop. Gives every screen a built-in narrative frame:
  the character select screen is "choosing which traveler's journal to
  open."
- **Cons:** Literal parchment (light, cream backgrounds) fights a
  dark-first, mobile-in-sunlight-and-in-bed UI unless handled carefully.
  Risks looking twee/storybook if over-illustrated.

### Recommendation: Direction C, with A as its accent language

**Traveler's Journal**, primary. The resolution to Direction C's main
weakness (light parchment vs. a dark-first UI) is to stop treating
"parchment" as a *background color* and start treating it as an *ink and
material* vocabulary instead: the journal's **cover** is dark leather and
obsidian (satisfies dark-theme, mobile-glare, and battery goals), its
**text** is parchment-cream ink, and its **accents** — borders on
important panels, the compass-rose motif, wax-seal buttons — borrow
Direction A's blue-and-gold heraldry sparingly, as the "kingdom" the
traveler is writing about, not the frame the UI lives in. Direction B's
arcane-violet is kept in reserve specifically for magic-context UI
(Spellbook, mana, buffs) rather than spent everywhere.

One sentence version: *a dark leather journal, its parchment pages lit by
candlelight, its margins occasionally touched with gold and a little
arcane ink.*

---

## 3. Color Language

No `Primary`/`Secondary`/`Accent`. Every color is a material or a place in
Hollowmere. Values below are intentionally dark-theme-first per the
Direction C resolution above.

| Name | Hex | Role | Why |
|---|---|---|---|
| **Obsidian** | `#14120F` | App background | The journal's cover in low light — warm near-black, never cool gray. |
| **Charcoal** | `#211D18` | Panel / surface background | One page back from the cover; where cards and dialogs sit. |
| **Ash** | `#4C453C` | Borders, dividers, disabled fills | Faded pencil lines; structure without shouting. |
| **Parchment** | `#E8DCC0` | Primary text | The page itself — warm cream, never pure white. |
| **Faded Ink** | `#A89C84` | Muted / secondary text | Ink a decade old. Captions, timestamps, placeholders. |
| **Traveler Gold** | `#C9A227` | Primary action, key highlights | Gold leaf on a compass rose. The one color that means "do this." |
| **Forge Iron** | `#726A5E` | Secondary buttons, utility chrome | Worked metal — present, not attention-seeking. |
| **Britannia Blue** | `#3E6284` | Links, informational state, kingdom heraldry accents | A direct nod to the client's Ultima Online lineage; used for "this is a fact," not "click me." |
| **Mage Violet** | `#7C5CC7` | Magic-context UI only (mana, spellbook, buffs) | Reserved. If it shows up outside a magic context it has been overused. |
| **Healing Emerald** | `#4C8B5C` | Success, healing, positive confirmation | Moss on old stone; calm, not neon. |
| **Warning Amber** | `#C97C2A` | Caution, low resources | A guttering candle flame. |
| **Danger Crimson** | `#A5342E` | Errors, destructive actions, death/danger states | A wax seal broken in anger. |

**Usage rules:**
- Obsidian/Charcoal/Ash are the *only* neutrals. Never introduce a raw
  gray (`#808080`-style) — every "gray" in Hollowmere has a warm cast.
- Traveler Gold is spent once per screen for the primary action. If two
  buttons on the same screen are gold, neither reads as primary anymore.
- Mage Violet is contextual, not decorative — it marks magic, not "cool
  things in general."
- Britannia Blue is informational, not interactive-primary — it should
  never be the only signal that something is clickable.

---

## 4. Typography

No `Header`/`Body`/`Caption`. Names describe the *voice* of the text.

| Old name (#20A) | Hollowmere name | Voice | Use |
|---|---|---|---|
| Header | **Title** | The illuminated first letter of a chapter | Screen titles, one per screen |
| Subheader | **Chapter** | A chapter heading | Section headers within a screen |
| Body | **Journal** | The traveler's own handwriting | All primary reading text |
| Button | **Inscription** | Words carved or stamped, meant to be acted on | Button labels, action text |
| Caption | **Rune** | Small marks in the margin | Timestamps, counts, footnotes, item flavor micro-text |
| Tooltip | **Whisper** | Something told to you quietly, in passing | Tooltips, hints, transient help text |

**Hierarchy:** Title > Chapter > Journal > Inscription > Rune ≈ Whisper.
Only one Title on screen at a time — it is the chapter you're currently
reading, not a running list of headlines. Rune and Whisper share a size
class but not a style: Rune is upright and muted (Faded Ink), Whisper is
italic and slightly warmer, since it's spoken rather than written.

A single serif-leaning display face for Title/Chapter (evokes manuscript
lettering) paired with a highly legible humanist sans for Journal/
Inscription/Rune/Whisper keeps the "ancient object, modern hand" feel
without ever sacrificing body-text readability. (No specific font is
licensed yet — this is a requirement for font selection, not a font
choice itself.)

---

## 5. Spacing Philosophy

Hollowmere breathes like a journal, not like a spreadsheet — but a
*traveler's* journal, filled edge-to-edge by someone who paid for every
page, not a designer's moodboard with acres of white space.

- **Narrative screens** (Login, Character Select, Journal, dialogue):
  generous room. These are moments, not tools — let Traveler Gold and
  Parchment text have space to be read.
- **Utility screens** (Inventory, Spellbook grid, Chat): tighter, denser.
  A traveler's satchel is packed. Density here is *in character*, not a
  compromise — it should still never force pinch-zooming to read a value.
- **Mobile vs. desktop:** the same spacing *tokens* (#20A's
  Tiny/Small/Medium/Large/ExtraLarge scale is kept — the token system was
  the right call and doesn't need reinventing) but different *token
  selection* per screen density: narrative screens lean on Large/
  ExtraLarge, utility screens lean on Tiny/Small. Desktop gets more
  breathing room at the same information density by using a larger
  reference resolution, not by inventing new tokens.
- **Touch targets never shrink for density.** A cramped inventory grid
  still needs ≥44px touch targets; density comes from tighter *spacing*
  between elements, never from shrinking the interactive element itself
  below a comfortable thumb size.

---

## 6. Panels

Every panel names a **material**, not a variant enum. (#20A's
`Card`/`Dialog`/`Toolbar`/`Sidebar`/`Popup`/`FloatingPanel`/
`ScrollablePanel` shape taxonomy is still correct — that's *layout*, kept
as-is. What changes is what those shapes are made *of*.)

| Material | Where | Description |
|---|---|---|
| **Leather** | Default panel background (replaces flat Charcoal fill) | Subtle worn-leather texture, near-black brown, faint grain visible only up close. The default "page cover" for Card/Dialog. |
| **Parchment Inset** | Reading-heavy panels (Journal, item descriptions, lore text) | A lighter, warm cream inset within a Leather frame — the *page*, distinct from the *cover*. Used sparingly; this is where Direction C's literal parchment lives, contained to a bounded area so it never fights dark-theme contrast globally. |
| **Iron-Bound** | Toolbar, HUD chrome, always-on-screen frames | Dark worked-metal edges and corner studs — communicates permanence ("this doesn't move"). |
| **Glass & Candlelight** | Popup, tooltip, transient overlays | Near-transparent Obsidian with a soft warm glow at the edge (never a cold blue glass — this isn't sci-fi). Communicates "temporary, will dismiss." |
| **Stone** | Modal/Dialog for high-stakes confirmations (delete character, PvP flag) | Heavier, cooler-toned, mortared-block texture. Weight signals consequence. |

**Shared rules across all materials:**
- **Borders:** a single hairline in Ash by default; Traveler Gold border
  reserved for the one emphasized element on screen (selected item,
  active tab). Never both a heavy border *and* a heavy shadow — pick one
  depth cue.
- **Corners:** small, consistent radius everywhere (a worn leather
  corner, not a sharp cut) — *except* Iron-Bound (square, riveted) and
  circular icon frames (Round, full circle). This directly resolves
  #20A's flagged "square corners" limitation: the fix is real 9-sliced
  leather-edge art at a small, subtle radius, not a stylistic square-vs-
  round debate.
- **Depth:** shadows are soft and warm-tinted (never pure black), and are
  used to indicate *stacking order* (this popup is above that panel), not
  decoration. A panel that doesn't overlap anything doesn't need a shadow.
- **Transparency:** reserved for Glass & Candlelight only. Leather/
  Parchment/Iron-Bound/Stone are opaque — they're physical objects.

---

## 7. Buttons

Every button state is described as a *physical* response, not a color
swap in the abstract.

- **Primary (Traveler Gold):** the one action you're meant to take.
  Solid gold-leaf fill, dark Inscription text for contrast. One per
  screen, ever.
- **Secondary (Forge Iron):** everything else actionable. Worked-metal
  fill, Parchment text.
- **Danger (Danger Crimson):** destructive/irreversible actions. A
  wax-seal red, always paired with a confirming Stone-material dialog for
  anything truly irreversible — the color alone is not the safety net.
- **Magic (Mage Violet):** spell/ability actions only — casting,
  spellbook entries, buff activation. Never used for a non-magic action
  no matter how "cool" it would look.
- **Disabled:** desaturates toward Ash and drops to ~50% opacity — reads
  as "faded ink," not as "broken."
- **Hover (mouse/desktop):** a slight warm glow brightening toward
  candlelight, no scale change — buttons don't jump at the cursor.
- **Pressed:** the button visibly *sinks* — darken + a 1px downward
  shift, like pressing a wax seal into paper. This is the one place a
  tiny bit of motion is welcome (see §8).
- **Selected:** a persistent thin Traveler Gold outline, distinct from
  hover — hover is transient attention, selected is a standing state
  (e.g. the active tab, the equipped item).
- **Touch feedback:** since there's no hover on touch, the "sink on
  press" treatment does double duty as the primary touch confirmation —
  it must be visible within one frame of finger-down, before any tap
  ambiguity (tap vs. scroll) is resolved.

---

## 8. Motion Language

**Nothing flashy. Everything communicates state. Weighty, not bouncy.**

Hollowmere's motion is the physical world responding to touch — paper
turning, a seal pressing, a page catching candlelight — never UI
"performing" at the player.

- **Every animation must answer "what changed?"** — appearing, dismissing,
  confirming, or erroring. If an animation doesn't answer that question,
  cut it.
- **Fast.** 120–200ms for most transitions. Fantasy, not sluggish —
  a heavy door still swings open in under a quarter second on screen.
- **Weighted easing, not bounce.** Ease-out on entrances (arrives with
  intent, settles without overshoot), ease-in on exits (leaves with
  purpose). No elastic/spring overshoot anywhere — that reads as "toy,"
  not "ancient and weighty."
- **Shared vocabulary every screen inherits:**
  - *Fade* — default for anything appearing/disappearing that isn't
    spatially anchored (tooltips, popups).
  - *Scale (subtle, 96%→100%)* — for anything confirming a choice
    (selecting a character, equipping an item) — a small "settling into
    place," not a cartoon pop.
  - *Slide* — for anything spatially anchored to an edge (a sidebar
    panel, a drawer) — it comes from where it lives.
  - *Press* — the button-sink from §7, shared by every interactive
    element, touch and mouse alike.
- **Magic is the one exception.** Spell-related UI may use a brief warm
  particle/glow flourish on cast confirmation — this is the single
  sanctioned "flashy" moment in the entire system, specifically because
  restraint everywhere else is what makes it land.

---

## 9. Iconography

**Style:** painted-metal line icons — a thin, slightly irregular
hand-inked outline (not a perfectly uniform vector stroke), filled with a
muted material tone rather than flat color. Think an engraving on a brass
instrument, not a Material Design glyph.

- **Not** pure outline (too thin/modern-flat), **not** fully painted
  illustration (too heavy/slow to produce at scale), **not** photoreal
  metal render (too expensive, wrong tone for UI chrome).
- **Monochrome by default** (Parchment or Faded Ink, matching surrounding
  text), tinted Traveler Gold/Mage Violet/Danger Crimson only to carry
  the same state meaning buttons carry (active, magic, destructive) —
  color is a state signal on icons, same as everywhere else in the
  system, never decoration.
- **Sizing/padding/states carry over unchanged from #20A's Icon
  wrapper spec** (that part of the plan was material-agnostic and
  doesn't need to change): consistent size steps, consistent padding, a
  muted/disabled tint identical to button disabled treatment.
- Future icon packs must ship as this single style consistently — a mix
  of flat-vector and painted-metal icons on the same screen is an
  immediate visual-identity failure.

---

## 10. Sound Design

Every interaction sound is something that would exist in a traveler's
satchel, not a synthesizer.

- **Button press (default):** a soft leather-and-paper tap — like a
  glove pressing a page flat. Quiet, warm, dry.
- **Primary/confirm action:** a small brass click — a clasp closing, a
  compass snapping shut.
- **Danger/destructive confirm:** a heavier, duller thud — a book
  slammed shut, not a glassy alarm.
- **Error/invalid:** two short dry taps (paper, not a buzzer) — firm,
  not harsh.
- **Success/positive:** a single warm chime, closer to a small bell than
  a digital "ding" — brief, never musical-jingle length.
- **Magic UI:** a soft arcane shimmer, the one place a non-physical sound
  is allowed — mirrors §8's "magic is the sanctioned exception" rule.
- **Panel open/close:** paper/page sounds for Leather/Parchment panels,
  a soft metal-latch sound for Iron-Bound panels — the sound should match
  the *material* named in §6, not be generic across all panels.
- **Global rule:** nothing electronic, nothing glassy/synthetic, nothing
  that would sound at home in a productivity app. If a sound could be
  mistaken for a Slack notification, it's wrong.

---

## 11. Accessibility

Atmosphere never gets a pass on accessibility — §1 already commits to
this ("legible" is non-negotiable), this section makes it concrete.

- **Color blindness:** every state that uses color (success/warning/
  danger/magic/selected) must carry a second non-color signal — an icon,
  a border-weight change, or text — never color alone. Danger Crimson vs.
  Healing Emerald must remain distinguishable under protanopia/
  deuteranopia simulation before either ships in a real screen.
- **Large fonts / dynamic type:** Journal and Inscription text must
  support at least a 130% scale step without truncation or overlap;
  layouts use flexible containers (as #20A's layout-group approach
  already does), not fixed-height text boxes.
- **High contrast mode:** an alternate token set (not a new theme
  architecture — #20A's `HollowmereTheme`/`HollowmereUIService` swap
  mechanism already supports this) that pushes Parchment-on-Obsidian
  contrast well past WCAG AA, and thickens Ash borders so panel edges
  don't disappear.
- **Mobile sunlight:** the dark-first palette (§3) already helps battery
  and OLED contrast, but Traveler Gold and Parchment text must be
  contrast-checked specifically at outdoor brightness, not just in a
  dim indoor test — this is a real QA step, not a one-time design review.
- **One-handed use:** primary actions on mobile utility screens
  (Inventory, Chat, Spellbook) live in the lower two-thirds of the
  screen wherever layout allows; nothing critical is thumb-unreachable
  when holding the device in one hand.

---

## 12. Screenshot Test

Someone should be able to see a single Hollowmere screenshot, with no UI
chrome labeled and no logo visible, and know it's Hollowmere because:

1. **The palette is warm-dark, never cold-gray** — Obsidian and Charcoal,
   not slate or navy. No screen looks like a code editor.
2. **Exactly one thing on screen is gold.** Traveler Gold marks the one
   primary action; nothing competes with it.
3. **Text reads like ink on a page**, not like a system font on a
   dashboard — Parchment-colored Journal text against a leather-dark
   panel, with a Chapter heading above it.
4. **A panel edge shows a material**, however subtly — a hairline of
   worn leather grain or a riveted iron edge, not a flat CSS-style
   rounded rectangle.
5. **Nothing is animating for its own sake.** If the screenshot were a
   video, motion would be rare, weighty, and purposeful — never a
   constantly-pulsing button or a spinning icon with no state behind it.
6. **Magic, when present, is the only "loud" element on screen** — a
   single violet glow or particle flourish, surrounded by restraint.

If a screenshot could be mistaken for a generic "fantasy UI kit" asset
pack, it has failed this test — the leather/parchment/brass material
language and the one-gold-action rule are what make it *ours*.

---

## Implementation Guidance

**Do not implement yet.** This section is critique and direction for
whoever (human or AI) builds #20C against this bible.

### Where #20A already got it right (keep, don't rebuild)

- The token architecture — `HollowmereTheme`/`HollowmereTypography`/
  `HollowmereMetrics`/`HollowmereUIService` — is sound. Nothing here
  requires new architecture, only new *content* in those tokens (new hex
  values, new named styles, real material sprites instead of flat
  color fills).
- The `Card`/`Dialog`/`Toolbar`/`Sidebar`/`Popup`/`FloatingPanel`/
  `ScrollablePanel` panel taxonomy is layout-correct — §6 maps materials
  onto it rather than replacing it.
- Wrapping Unity's `Button`/`Toggle`/`Slider`/etc. instead of
  reimplementing them remains the right call — the material system is a
  skinning problem (sprites, colors, sounds), not a rebuild-the-widget
  problem.

### Where the current UI Showcase is honestly generic (weaknesses)

- **Flat single-color fills everywhere.** Every panel and button is one
  solid color with no material read — the single biggest gap against
  this bible. This is expected (art was explicitly out of scope for
  #20A) but it's the first thing to fix.
- **Square corners on Small/Medium/Large radius**, already flagged as a
  known limitation in #20A — §6 resolves this with real leather-edge art
  at a small consistent radius rather than a bigger radius or a
  stylistic square/round argument.
- **Generic sans-serif system font at uniform weight** for every text
  role — nothing currently distinguishes a Title from Journal body text
  except size. §4's Title/Chapter voice needs an actual display face,
  not just a bigger size of the same font.
- **No border/material distinction between panel variants** — a Dialog
  and a Card currently look identical except for size. §6 gives each
  panel variant an actual reason to look different (Stone vs. Leather vs.
  Glass), which the current showcase can't demonstrate until that art
  exists.
- **No motion at all currently exists** (deferred to #20B/#20C by
  design) — when it lands, §8's "weighted, not bouncy" rule should be
  the acceptance criterion in review, not just a code review for
  correctness.

### Unnecessary complexity to simplify rather than carry forward

- **Icon and Toolbar button variants are visually redundant today** —
  both currently just render as `theme.Surface`-colored buttons with no
  distinguishing treatment. Once real materials exist, consider
  collapsing them into one `HollowmereButton` "chrome" variant
  distinguished by *size* (icon = square/compact, toolbar = wide) rather
  than by a separate enum case that implies a different visual language
  that doesn't actually exist yet. Don't invent the distinction in code
  before the design justifies it.
- **FloatingPanel/Sidebar/Popup already collapse to "Card with different
  anchoring"** per #20A's own plan — resist the urge to give them bespoke
  materials just because §6 lists five materials. Glass & Candlelight is
  for genuinely transient overlays (Popup, Tooltip); FloatingPanel and
  Sidebar should stay Leather like Card unless a specific screen proves
  otherwise. Don't multiply materials past what §6 actually specifies.
- **Don't build a general-purpose "texture library" abstraction.** §6
  names five materials because five real needs were identified, not
  because a config-driven N-material system is inherently good design.
  When a sixth material is genuinely needed, add it here in the bible
  first, then implement it — don't build speculative infrastructure for
  materials that don't exist yet.

### Suggested next milestone (#20C, not started)

1. Source or commission the leather/parchment/iron/stone/glass base
   sprites (9-sliced) called for in §6 — this is the actual unblock for
   everything else; every "generic" complaint above traces back to
   missing art, not missing code.
2. Repoint `HollowmereDarkTheme`'s color values to the §3 palette (a
   content change to an existing asset, not new architecture).
3. Select and license the Title/Chapter display face and the Journal/
   Inscription/Rune/Whisper body face called for in §4.
4. Re-skin the existing `HollowmereButton`/`HollowmerePanel` prefabs with
   the new sprites/colors/fonts — no new components should be required
   for this pass if #20A's architecture holds up, which is itself a good
   test of whether it does.
5. Only after that: proceed with #20B's remaining controls (Toggle,
   Dropdown, Slider, InputField, SearchBox, Icon, Animations) *against*
   this bible, not against the current placeholder look.
