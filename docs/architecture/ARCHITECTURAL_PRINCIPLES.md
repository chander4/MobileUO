# Hollowmere Architectural Principles

This is Hollowmere's durable architectural constitution. It summarizes enduring rules; ADRs remain the authoritative record of individual decisions and their rationale.

1. **Translate the interface, never the game.** Preserve Ultima Online's sandbox identity; modernize the path between intent and input. See `HOLLOWMERE_PRODUCT_VISION.md`.
2. **Gameplay belongs to the engine; mobile UX belongs to Hollowmere.** Put work in the layer that owns it, as defined by `ENGINE_STRUCTURE.md`.
3. **Favor composition over modification.** Prefer established extension points and reusable behavior over direct engine edits or specialized subclasses.
4. **Engine bugs are fixed once.** Make an intentional override only when needed, track it, document its divergence, and consider upstream value. See ADR-001 and ADR-006.
5. **Reusable behaviors become reusable primitives.** Extend action and interaction primitives rather than creating one-off controls or pipelines.
6. **One rendering owner per state.** Unity owns application chrome; ClassicUO Gumps own game state. Do not duplicate state or render it in parallel. See `HOLLOWMERE_UI_RENDERING_STRATEGY.md`.
7. **Runtime validation precedes completion.** Compilation is not player-facing evidence; validate the real path in proportion to risk.
8. **Protected systems require explicit approval.** Stable systems are not casual refactor targets. The authoritative lists are in `DECISIONS.md` and `CLAUDE.md`.
9. **Diagnostics use the Diag framework.** Permanent tracing is category-based, off by default, and guarded for negligible disabled cost. See ADR-005.
10. **Architecture is recorded, not implied.** Search ADRs before deciding; propose an ADR before introducing or changing a durable boundary.
