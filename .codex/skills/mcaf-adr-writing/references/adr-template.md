# ADR-XXXX: Title

> TEMPLATE ONLY — remove this note and replace all placeholder text before saving as a real ADR under `docs/ADR/`.

Status: Proposed | Accepted | Implemented | Rejected | Superseded  
Date: YYYY-MM-DD  
Related Features: `docs/Features/...` (recommended)  
Supersedes: `docs/ADR/ADR-....md` (delete if none)  
Superseded by: `docs/ADR/ADR-....md` (delete if none)

Rules:

- This ADR is **self-contained** — avoid “as discussed”; include all critical context and links.
- At least **one Mermaid diagram is mandatory** (boundaries/modules/interactions for this decision).
- Once accepted, save as `docs/ADR/ADR-XXXX-title-in-kebab-case.md` (English, kebab-case). Keep this reference file unchanged and copy its structure into the real ADR.

---

## Implementation plan (step-by-step)

> TEMPLATE ONLY — replace these checkboxes with real implementation steps for this ADR and keep them updated while implementing.

- [ ] Analyze current state (facts)
- [ ] Plan the change (steps, files/modules, tests, docs)
- [ ] Implement the change (smallest safe increments)
- [ ] Add/update automated tests (happy + negative + edge; protect invariants)
- [ ] Run verification commands (build/test/format/analyze/coverage) and record results
- [ ] Update docs (ADR/Features/Architecture overview) and close the checklist

---

## Context

- Current situation (what exists today).
- Constraints (tech/legal/time/org constraints that matter).
- Problem statement (what is failing / what you must enable).
- Goals (what success looks like).
- Non-goals (what this ADR is not trying to solve).

---

## Stakeholders (who needs this to be clear)

| Role | What they need to know | Questions this ADR must answer |
| --- | --- | --- |
| Product / Owner | User/business impact, scope, rollout risk | What changes for users? What’s out of scope? |
| Engineering | Boundaries/modules, data/contract changes, edge cases | What do we change, where, and why? |
| DevOps / SRE | Deployability, config, monitoring, rollback | How do we ship safely and observe it? |
| QA | Test scenarios + environment assumptions | What must be proven by automated tests? |

---

## Decision

- One sentence decision statement.

Key points:

- Key point 1
- Key point 2

---

## Diagram

This section is mandatory.

> TEMPLATE ONLY — Mermaid often breaks with fancy syntax. Keep it simple and make sure it renders in the repo.

```mermaid
```

---

## Alternatives considered

### Option A

- Pros:  
- Cons:  
- Rejected because:

### Option B

- Pros:  
- Cons:  
- Rejected because:

---

## Consequences

### Positive

- Benefit

### Negative / risks

- Risk  
- Mitigation:

---

## Impact

### Code

- Affected modules / services:  
- New boundaries / responsibilities:  
- Feature flags / toggles (names, defaults, removal plan):

### Data / configuration

- Data model / schema changes:  
- Config changes (keys, defaults, secrets handling):  
- Backwards compatibility strategy:

### Documentation

- Feature docs to update:  
- Testing docs to update:  
- Architecture docs to update:  
- `docs/Architecture.md` updates (what must change):  
- Notes for `AGENTS.md` (new rules/patterns):

---

## Verification

This section is mandatory: describe how to prove the decision (tests + commands).

### Objectives

- What behaviour / qualities must be proven.
- Which invariants from this ADR must be encoded as tests (happy path + negative/forbidden + edge cases).
- Link each objective/scenario to the specific automated test(s) that prove it.

### Test environment

- Environment (local compose / staging / prod-like):  
- Data and reset strategy (seed data, migrations, rollback plan):  
- External dependencies (real / sandbox / test environment required):

### Testing methodology

- Core flows and invariants that MUST be proven:  
- Positive flows that MUST pass:  
- Negative / forbidden flows that MUST be rejected or fail safely:  
- Edge / boundary / unexpected flows that MUST be covered:  
- Required realism level (real dependencies, contracts, environments):  
- Coverage baseline requirement (must stay at least at the pre-change level or improve):  
- Pass criteria for considering the ADR implementation complete (all relevant tests green, new tests added, verification complete):

### Test commands

- build: (paste from `AGENTS.md`)
- test: (paste from `AGENTS.md`)
- format: (paste from `AGENTS.md`)
- coverage: (paste from `AGENTS.md`; delete if none)

### New or changed tests

| ID | Scenario | Level (Unit / Int / API / UI) | Expected result | Notes / Data |
| --- | --- | --- | --- | --- |
| TST-001 | Happy path / negative / edge | Integration | Observable outcome | Fixtures / seed data |

### Regression and analysis

- Regression suites to run (must stay green):  
- Static analysis (tools/configs that must pass):  
- Monitoring during rollout (logs/metrics/alerts to watch):
- Coverage comparison against baseline:

---

## Rollout and migration

- Migration steps:  
- Backwards compatibility:  
- Rollback:

---

## References

- Issues / tickets:  
- External docs / specs:  
- Related ADRs:

---

## Filing checklist

- [ ] File saved under `docs/ADR/ADR-XXXX-title-in-kebab-case.md` (not in `docs/templates/`).
- [ ] Status reflects real state (`Proposed`, `Accepted`, `Rejected`, `Superseded`).
- [ ] Links to related features, tests, and ADRs are filled in.
- [ ] Diagram section contains at least one Mermaid diagram.
- [ ] Testing methodology is filled in with positive, negative, and edge flows plus pass criteria.
- [ ] New or updated automated tests exist for the changed behaviour.
- [ ] All relevant tests are green and coverage did not fall below baseline.
- [ ] `docs/Architecture.md` updated if module boundaries or interactions changed.
