# Hollowmere Milestone Review Checklist

Every milestone must satisfy this checklist before it is complete.

- [ ] Read `AGENTS.md` and `PROJECT_STATE.md`.
- [ ] Reviewed the relevant ADRs and architecture documents.
- [ ] Confirmed the work follows an existing decision, or obtained approval for an ADR-worthy change.
- [ ] Introduced no duplicate architecture, rendering path, action primitive, persistence mechanism, or diagnostics mechanism.
- [ ] Obtained explicit approval for Protected-system changes and Stable-system refactors.
- [ ] Validated at runtime in proportion to risk; touch/input work includes device validation where applicable.
- [ ] Reviewed diagnostics; durable tracing uses `Diag`, and disposable investigation scaffolding is removed.
- [ ] Updated affected documentation, including engine inventory where applicable.
- [ ] Updated `PROJECT_STATE.md`.
- [ ] Updated `AI_HANDOFF.md`.
- [ ] Created or updated an ADR if architecture changed.
- [ ] Reviewed staged commit scope against its stated intent.
