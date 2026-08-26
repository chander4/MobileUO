# ADR-003: Action Pipeline (Primary Action)

## Context

Issue #24 needed a single, always-present button whose behavior changes based on
what the player has selected: Attack a hostile mobile, Talk to a friendly one, Loot
a corpse, Pick Up a ground item. The first instinct was to have the resolver that
figures out "what should this button do" hand back a ready-to-render
`GameActionDefinition` directly.

The user explicitly rejected that shape before implementation began, for a specific
reason: a resolver that returns a presentation object (labels, icons, tooltips,
`GameActions` calls) is coupled to *this one button*. The underlying question — "what
is the player's primary interaction with this object?" — is reusable far beyond a
single HUD button, but only if nothing about the answer knows what a button is.

## Decision

Split the pipeline into four stages with a strict one-way dependency:

```
SelectedObject
    -> PrimaryActionResolver   (pure classification, zero UI/GameActions knowledge)
    -> PrimaryAction           (an immutable domain value: Type + Target, nothing else)
    -> PrimaryActionDefinitionFactory  (the only place that knows about labels,
                                          icons, tooltips, and which GameActions
                                          call each verb makes)
    -> ActionButtonGump.SetDefinition()
```

`PrimaryActionResolver` never imports or references `GameActionDefinition`,
`ActionButtonGump`, or anything presentation-shaped. `PrimaryAction` is a pure data
value with no behavior. The factory is the *only* seam where "verb" becomes
"clickable button."

## Alternatives considered

- **Resolver returns `GameActionDefinition` directly.** Rejected by explicit design
  direction — this was the default first instinct, and it was rejected specifically
  because it couples classification to one presentation surface. Rebuilding a radial
  menu, a controller prompt, or an accessibility action list later would mean
  duplicating the classification logic instead of reusing it.
- **Single class doing classification and definition-building together, split only
  by private methods.** Rejected — private-method separation doesn't prevent the
  coupling; it just hides it. The point is an enforced dependency direction, which
  requires actual type boundaries.
- **Generic "action provider" interface with pluggable strategies.** Rejected as
  overbuilt for one consumer — the four-stage pipeline is already the right amount
  of separation; wrapping it in a plugin interface before a second consumer exists
  is speculative abstraction.

## Consequences

- Adding a new verb (e.g., "Open Door", "Mount") means adding one case to the
  resolver's classification and one builder method to the factory — no button code
  changes.
- The same `PrimaryAction` value could, in principle, drive a completely different
  presentation (a controller face-button prompt, a radial menu slice) by writing a
  new factory, without touching the resolver at all. This was the entire point of
  the split and hasn't been exercised by a second consumer yet.
- Debugging is easier because each stage answers exactly one question — when #24's
  investigation needed to trace "why does the button show the wrong verb," the
  resolver could be checked in isolation from the button's rendering/dispatch bugs
  (which turned out to be the real cause).

## Future implications

- The pipeline's reusability claim is currently theoretical — no second consumer
  exists yet. The first time something other than `ActionButtonGump` wants a
  `PrimaryAction`, this design gets its real test. If it doesn't fit cleanly,
  that's valuable signal to revisit the boundary, not a reason to have skipped the
  separation.
- `PrimaryAction`'s target is currently a live `BaseGameObject` reference, not a
  serializable value — fine for same-frame consumption, but would need rethinking
  if a future consumer needs to persist or transmit a `PrimaryAction` (unlikely, but
  worth flagging before it's assumed).
