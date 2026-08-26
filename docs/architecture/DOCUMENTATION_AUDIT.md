# Documentation Audit Recommendations

This is a recommendation report, not a roadmap or architectural decision. It identifies documentation hygiene work observed during the repository-maturity audit. No recommendation here is approved implementation scope.

## Duplicate or overlapping material

1. Keep `docs/development/DEVELOPMENT_PROCESS.md` and `docs/development/REVIEW_CHECKLIST.md` canonical. Root compatibility links should remain short pointers so process material cannot diverge.
2. The product vision, `CLAUDE.md` manifesto, and mobile guidelines intentionally overlap on philosophy. Preserve the hierarchy: vision explains why, guidelines give concrete UI rules, and `CLAUDE.md` carries binding legacy protections. Cross-link rather than restate principles in each file.
3. `ROADMAP_VISION.md`, `ROADMAP_NEXT.md`, and issue documents have distinct roles (themes, questions, and scoped history). Label future additions by role so they do not become duplicate backlogs.

## Conflicting or stale references

1. `README.md` still presents the upstream MobileUO identity and links. When Hollowmere is ready for public-facing documentation work, replace or prefix it with a concise Hollowmere overview and links to the vision; preserve licensing and upstream attribution.
2. `CLAUDE.md` names an older current milestone and stable commit. Its binding protection lists remain authoritative, but its snapshot sections conflict with `PROJECT_STATE.md`. Move its mutable snapshot content to clearly historical wording or refresh it whenever state changes.
3. `HOLLOWMERE_MOBILE_MODERNIZATION_VISION.md` and `SESSION_CONTEXT.md` predate the current documentation set. Mark them clearly historical, or reconcile their surviving decisions into current documents, to reduce accidental use as current direction.
4. The earlier theme ADR is named `0001-...`, while later ADRs use `ADR-001` through `ADR-007`. Adopt a documented naming convention for future ADRs and add a short index or alias note; do not renumber history solely for aesthetics.

## Missing cross-links

1. Add a compact documentation map to the public README when its identity is refreshed, pointing contributors to `AGENTS.md`, product vision, project state, and contribution guidance.
2. Add reciprocal links between the UI theme README/ADR and rendering strategy so contributors can distinguish Unity app chrome from Gump-owned game state before extending the theme system.
3. Add a small ADR index in `docs/adr/` or `DECISIONS.md` that includes the theme ADR alongside the numbered series, with subject and status.
4. Link completed issue reviews to governing ADRs where a later ADR records their outcome; retain issue documents as historical implementation evidence rather than duplicating decision rationale.

## Documentation maintenance recommendations

1. Keep `PROJECT_STATE.md` and `AI_HANDOFF.md` current after every completed milestone; they are the only intentionally volatile contributor snapshots.
2. During manual engine-override review, replace inferred inventory notes with observed facts and update the architecture review's debt count.
3. Keep audit reports recommendation-only until planning scopes and approves a change; no recommendation here changes rendering, gameplay, roadmap priority, or engine policy.
