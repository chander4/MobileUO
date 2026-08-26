# Mobile Design Guidelines

Concrete rules for every future mobile UI in Hollowmere. Where `HOLLOWMERE_PRODUCT_
VISION.md` answers "why," this document answers "how." A design or code review
should be able to point at a specific rule here and say "this violates X" without
relitigating the philosophy each time.

## Thumb reach

Design for the bottom two-thirds of the screen, biased toward whichever edge the
player holds the device from. Reach is asymmetric, not a centered arc from the
screen's middle — a control centered on-screen is not automatically "reachable."
The single most-used control on screen at any moment (the Primary Action button is
the current example) gets priority placement in the easy-reach zone; secondary and
rarely-used controls can live further out.

## Gesture consistency

Exactly two primary gestures, used the same way everywhere (ADR-007):

- **Tap** = the primary/default action for whatever's under the finger.
- **Long-press** (500ms, <5px movement tolerance — the established constant, not a
  per-feature choice) = secondary/destructive action: dismiss, remove, open an
  alternate option.

A new feature does not get to invent a third gesture, or reuse long-press for
something that isn't secondary/destructive, without a documented reason and an ADR.
Multi-touch gestures (pinch, two-finger tap) are reserved for cases with a genuine
spatial meaning (zoom, pan) and must always have a one-handed alternative.

## Touch target sizing

No interactive element smaller than a comfortable fingertip contact area,
regardless of how precisely it's rendered visually. When the *thing being
represented* is small (a ring on the ground, a single coin in a stack), the
*tappable area* around it still isn't — pad the hit region past the visual bounds
rather than shrinking the requirement to match small art. This is not optional
polish; it's the direct fix for a real, already-reported problem (small ground
items being difficult to tap precisely).

## Context awareness

Prefer a control that already knows what the player wants (Primary Action's model)
over a menu the player has to navigate to find the same option. Before adding a new
button, ask whether the existing selection/context already implies the action — if
it does, extend the Primary Action pipeline (ADR-003) instead of adding chrome.

## Never require pixel precision

If an interaction's success depends on hitting an exact point (splitting a stack at
a precise pixel, dragging an item onto a one-tile-wide target), it needs a
touch-native alternative: a stepper/slider for quantities, tap-to-select instead of
drag-to-target, generous drop zones instead of exact ones. This is the single most
common way a desktop-shaped interaction survives unmodernized — treat "does this
require aiming" as a hard gate a feature has to pass, not a nice-to-have.

## Minimal modal dialogs

A modal blocks the world and demands full attention — appropriate for rare,
high-stakes confirmations (irreversible actions), inappropriate as the default way
to ask a quick question. Prefer inline controls, contextual popups anchored to
what they're about, or the Primary Action button itself over a dialog that steals
the whole screen for a small decision.

## Progressive disclosure

Show the common case by default; make the deeper option reachable, not
pre-displayed. A vendor's buy screen shows quantity controls without a submenu; a
character's full stat breakdown lives one tap past the at-a-glance summary. This is
"simplify the path to information," not "delete the information" — see the product
vision's explicit warning against confusing the two.

## Always preserve game information

Chrome is a tax on seeing Britannia (product vision, Core Principle #2) — but
*information* the player needs (health, notoriety, what's targeted, what's
selected) is not the same as chrome, and must never be sacrificed for a cleaner-
looking screen. When space is tight, reduce decoration before reducing information.

## Landscape-first

Design primary gameplay screens landscape-first — it's the natural orientation for
an isometric world view and matches how most players will actually hold the device
during active play. Portrait support (where offered) is a deliberate secondary
layout, not landscape squeezed into a taller frame.

## Tablet scaling

Don't assume phone-sized proportional scaling holds at tablet size — more screen
space changes the "one thumb" constraint the phone layout is built around (see
ROADMAP_VISION.md's Tablet Experience theme). Treat tablet as a layout worth
explicit evaluation, not a bigger phone.

## One-handed interaction

Every core gameplay action must be reachable using one hand's thumb alone. A
two-handed interaction is acceptable only as an optional enhancement (pinch-to-zoom
on the world map) that always has a one-handed fallback (zoom buttons, a slider) —
never as the only way to perform a core action.

## Consistency with existing ActionBar and Primary Action architecture

A new tappable "thing a player can do" should be expressed as a
`GameActionDefinition` and rendered through `ActionButtonGump`/`ActionBar`, not a
bespoke button with its own `OnMouseDown` handler. This isn't just code reuse — it
guarantees the new control automatically inherits the established gesture
semantics (tap, long-press-to-remove where applicable), touch-target sizing, and
visual language, instead of a new one-off risking a subtly inconsistent feel. A
proposal to build a new interactive element outside this pipeline needs a stated
reason, the same way deviating from the long-press convention does.
