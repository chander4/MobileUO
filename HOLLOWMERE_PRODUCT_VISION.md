# Hollowmere Product Vision

> If Ultima Online were designed for phones in 2026 instead of PCs in 1997, while
> remaining unmistakably Ultima Online, what would it look like?

This is Hollowmere's north star. Every feature, every design review, every "should we
build this" conversation gets measured against the answer below. If a proposal doesn't
serve this vision, it doesn't ship — regardless of how easy it is to build or how many
other mobile games do it.

## The one-paragraph answer

It would look like UO played through a smaller, smarter window: the same
skill-driven, full-loot, player-run sandbox that made the original dangerous and
alive, but reachable with one thumb, readable at arm's length, and forgiving of a
touchscreen instead of a mouse's pixel-perfect precision. Nothing about *what the
world lets you do* would change. Nearly everything about *how your hand does it*
would.

## Design philosophy

Hollowmere modernizes the **interface**, never the **game**. UO in 1997 was not
badly designed — it was designed for different hardware. A mouse gives you a cursor
with pixel precision and unlimited screen real estate for chrome; a phone gives you a
blunt fingertip and a screen you're often holding with the same hand you're tapping
with. The skill system, the economy, the risk of full-loot PvP, the absence of level
gates, the fact that a tailor and a dragon-slayer are both legitimate ways to spend a
life in Britannia — none of that is a UI problem. It doesn't get "modernized." The
*distance between the player's intent and the game's response* is the only thing on
the table.

## Core principles

1. **Translate, don't replace.** Every mechanic that exists today has a touch-native
   expression. Find it before assuming it needs to be cut or dumbed down.
2. **The world is the interface.** Chrome is a tax on screen space the player is
   paying to see Britannia. Every pixel of permanent UI needs to justify its rent.
3. **One thumb is the default case, not the edge case.** Design for someone standing
   on a train holding a rail with their other hand. If it doesn't work there, it's not
   done — it's a desktop feature wearing a mobile costume.
4. **Precision is not a skill the player consented to test.** A mouse cursor rewards
   fine motor control few people brought to a phone. Where UO's original interaction
   depended on pixel accuracy (clicking a tiny ground item, dragging one arrow off a
   stack of nineteen), the *skill being tested* should move from "can you aim" to
   "did you mean to do this" — same decision, more forgiving input.
5. **Context beats chrome.** A button that already knows what you want (see: Primary
   Action) is worth more than ten buttons the player has to choose between. Prefer
   inferring intent from what's selected over asking the player to navigate a menu.
6. **Never hide the danger.** UO's tension comes from stakes that are real and
   visible. Any modernization that quietly insulates the player from a full-loot
   death, a failed negotiation, or a genuine mistake is not a UX improvement — it's
   deleting the game.
7. **Sessions are shorter and get interrupted.** A phone call, a bus stop, a low
   battery. State needs to survive that. Punishing an interruption harder than a
   desktop client would is a mobile-specific failure, not a neutral one.
8. **Every animation and state change earns its frame.** Feedback has to be
   immediate and legible at a glance, because the player's other hand and other eye
   might be doing something else entirely.
9. **Ship the cheap, honest win before the expensive, flashy one — but never mistake
   "improved" for "mobile-native."** A long streak of individually-safe patches to a
   mouse-shaped interaction (bigger hit boxes, a scrollbar that also responds to
   drag) does not add up to a touch-native interaction on its own. If a fix only
   makes the existing desktop paradigm easier to reach with a finger, name what the
   actual mobile-native version would look like — even if it ships later.

## What should NEVER change

- **Skill-based character progression.** No levels, no classes, no gated content by
  "power level." A weaver and a warrior remain equally valid characters.
- **The sandbox economy.** Player-crafted goods, player-run vendors, resource
  gathering as a real profession — none of this becomes a currency-purchase shortcut.
- **Full-loot, flaggable PvP and real consequence.** Death matters. Risk is a design
  tool, not a bug to be smoothed away.
- **Player housing as a real, placed, ownable thing in the world** — not an
  instanced menu screen.
- **Freedom of choice over authored content.** No quest-marker theme-parking. No
  forced tutorial funnel into "the meta build." The world doesn't tell you what to do
  next; it lets you find out.
- **World persistence and player impact.** Actions have lasting, shared consequences.
- **Systemic depth over scripted spectacle.** Emergent stories (a guild war, a market
  corner, a heist) stay more important than any single authored questline.

## What SHOULD modernize

- **Input model.** Mouse-and-keyboard assumptions (right-click context menus,
  hover tooltips, pixel-precise dragging, modifier-key shortcuts) get touch-native
  replacements: long-press, context-sensitive primary actions, generous hit targets.
- **Information density and legibility.** Desktop UO could pack ten overlapping
  status windows onto a 4K monitor. A phone can't, and shouldn't try — it should
  surface less, more legibly, more often.
- **Onboarding friction.** The original client assumed a manual, a friend, or a
  decade of MMO literacy. That assumption doesn't survive a phone.
- **Accessibility.** Never optional, never an afterthought bolted on at the end.
- **Session shape.** Quick-in, quick-out actions (check on a vendor, respond to a
  guild message, log a quick gathering run) should be genuinely quick, not a
  scaled-down version of a 45-minute desktop session.
- **Feedback and affordance.** Icons, motion, and state should read instantly at a
  glance, the way desktop UO could rely on a stationary mouse cursor and full
  attention to communicate less.

## Mobile-first philosophy

Mobile is not "desktop UO, but smaller." It's the primary design target. Every new
system gets designed for a phone first and verified on desktop second — not the
reverse. If a feature only works because the player has a keyboard, a large screen,
or a precise pointer, it isn't finished; it's half of a feature with the mobile half
still owed.

## Accessibility philosophy

Accessibility is a design constraint from the first sketch, not a pass at the end.
Concretely, that means: touch targets sized for real thumbs and imprecise motor
control (not the smallest hit box that technically works), color never as the *only*
signal (notoriety, health state, danger), text that scales without breaking layout,
and interactions that don't require sustained fine motor precision or split-second
timing to access core gameplay. A player with limited dexterity, low vision, or a
cracked screen protector is a player Hollowmere is designed for, not one it happens
to also support.

## One-handed play

The single hardest and most honest test of a mobile UI: can you play holding the
phone in one hand, thumb doing all the work, for five real minutes? Concretely:

- Reachable zone is the bottom two-thirds of the screen, biased toward whichever
  edge the player holds from (thumb reach is asymmetric, not a centered arc).
- No interaction should require two simultaneous touch points except an explicit,
  optional power-user gesture (e.g. pinch-to-zoom on the world map) that always has
  a one-handed alternative.
- The most-used action on screen at any moment (see: Primary Action) should be
  reachable without a hand repositioning.

## Social interaction

UO's social fabric — guilds, parties, player economies, spontaneous world chat — is
part of the sandbox, not decoration on top of it. Modernization here means removing
friction from *expressing* that social layer (quick chat phrases, a faster path to
party/guild actions, chat that doesn't eat the screen) without changing what the
social systems *are*. Hollowmere does not add matchmaking, does not add algorithmic
"friend suggestions," and does not gamify social interaction with points or streaks.

## Sandbox preservation

This is worth stating on its own, because it's the principle most at risk of erosion
one reasonable-sounding compromise at a time: **Hollowmere's job is to make the
sandbox reachable, not to make it safer, faster, or more directed.** Every proposal
that trades player agency for convenience — auto-pathing that removes the need to
know the map, auto-loot-everything that removes inventory triage as a decision,
quest arrows that remove the need to read — needs to survive the question "does this
still feel like *finding your own way* in Britannia?" If the honest answer is no, the
feature doesn't ship, no matter how much friction it removes.

## Examples of good modernization

- **The context-sensitive Primary Action button (#24).** Replaces a right-click
  context menu (a desktop-only affordance) with a single button whose meaning is
  inferred from what's selected. Same decision space, zero pixel-precision cost, no
  menu navigation.
- **Long-press to close health bars / long-press to remove an Action Bar entry.**
  Touch has no right-click. Long-press is the touch-native equivalent, applied
  consistently rather than invented per-feature.
- **The Universal Action Picker.** Replaces "memorize which hotkey does what" with
  "search and tap," while still landing on the same underlying `GameActionDefinition`
  every other entry point uses — the picker doesn't invent a parallel action system.
- **Diagnostics as a permanent, toggleable framework** rather than debug code that
  gets written once and deleted. Not player-facing, but it's the same philosophy
  applied to how the team builds: durable infrastructure over one-off patches.

## Examples of bad modernization

- **Auto-pathing to quest objectives.** Deletes the skill of knowing the world.
- **Auto-loot-everything with no player decision point.** Deletes inventory
  management as a real, meaningful choice under pressure.
- **Energy systems, login timers, or anything that gates play by a clock instead of
  by the player's own time.** UO's sessions are the player's to shape.
- **A battle pass, gacha, or any monetization pattern that turns character
  progression or itemization into a purchase.** The economy is player-driven or it
  isn't UO.
- **Removing full-loot PvP risk "to be more mobile-friendly."** This mistakes
  *comfortable* for *good* — it would remove the exact tension the sandbox depends on.
- **A generic "streamlined" UI that hides system depth behind a simplified summary
  view with no way to go deeper.** Simplifying the path to information is good;
  deleting the information is not the same thing.
