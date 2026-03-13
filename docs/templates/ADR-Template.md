# ADR-XXXX: Title

> TEMPLATE ONLY — remove this note and replace all placeholder text before saving as a real ADR under `docs/ADR/`.

Status: Proposed | Accepted | Implemented | Rejected | Superseded  
Date: YYYY-MM-DD  
Related Features: `docs/Features/...` (recommended)  
Supersedes: `docs/ADR/ADR-....md` (delete if none)  
Superseded by: `docs/ADR/ADR-....md` (delete if none)

Rules:

- This ADR is self-contained. Avoid "as discussed"; include all critical context and links.
- At least one Mermaid diagram is mandatory.
- Once accepted, save as `docs/ADR/ADR-XXXX-title-in-kebab-case.md` and keep this reference file unchanged.

---

## Implementation Plan (Step-by-Step)

> TEMPLATE ONLY — replace these checkboxes with real implementation steps for this ADR and keep them updated while implementing.

- [ ] Analyze current state (facts)
- [ ] Plan the change (steps, files or modules, tests, docs)
- [ ] Implement the change (smallest safe increments)
- [ ] Add or update automated tests (happy, negative, and edge; protect invariants)
- [ ] Run verification commands (build, test, format, coverage) and record results
- [ ] Update docs (ADR, features, architecture overview) and close the checklist

---

## Context

- Current situation (what exists today).
- Constraints (technical, legal, time, or organizational constraints that matter).
- Problem statement (what is failing or what must be enabled).
- Goals (what success looks like).
- Non-goals (what this ADR is not trying to solve).

---

## Stakeholders (Who Needs This To Be Clear)

| Role | What they need to know | Questions this ADR must answer |
| --- | --- | --- |
| Product / Owner | User or business impact, scope, rollout risk | What changes for users? What is out of scope? |
| Engineering | Boundaries or modules, data or contract changes, edge cases | What do we change, where, and why? |
| DevOps / SRE | Deployability, config, monitoring, rollback | How do we ship safely and observe it? |
| QA | Test scenarios and environment assumptions | What must be proven by automated tests? |

---

## Decision

- One-sentence decision statement.

Key points:

- Key point 1
- Key point 2

---

## Diagram

This section is mandatory.

> TEMPLATE ONLY — keep Mermaid simple and make sure it renders in the repo.

```mermaid
flowchart LR
```

---

## Alternatives Considered

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

### Negative / Risks

- Risk
- Mitigation:

---

## Impact

### Code

- Affected modules or services:
- New boundaries or responsibilities:
- Feature flags or toggles (names, defaults, removal plan):

### Data / Configuration

- Data model or schema changes:
- Config changes (keys, defaults, secrets handling):
- Backwards compatibility strategy:

### Documentation

- Feature docs to update:
- Testing docs to update:
- Architecture docs to update:
- `docs/Architecture.md` updates (what must change):
- Notes for `AGENTS.md` (new rules or patterns):

---

## Verification

This section is mandatory: describe how to prove the decision.

### Objectives

- What behavior or qualities must be proven.
- Which invariants from this ADR must be encoded as tests.
- Link each objective or scenario to the specific automated test(s) that prove it.

### Test Environment

- Environment (local compose, staging, or prod-like):
- Data and reset strategy (seed data, migrations, rollback plan):
- External dependencies (real, sandbox, or test environment required):

### Testing Methodology

- Core flows and invariants that must be proven:
- Positive flows that must pass:
- Negative or forbidden flows that must be rejected or fail safely:
- Edge, boundary, or unexpected flows that must be covered:
- Required realism level (real dependencies, contracts, environments):
- Coverage baseline requirement (must stay at least at the pre-change level or improve):
- Pass criteria for considering the ADR implementation complete:

### Test Commands

- build: (paste from `AGENTS.md`)
- test: (paste from `AGENTS.md`)
- format: (paste from `AGENTS.md`)
- coverage: (paste from `AGENTS.md`; delete if none)

### New Or Changed Tests

| ID | Scenario | Level (Unit / Int / API / UI) | Expected result | Notes / Data |
| --- | --- | --- | --- | --- |
| TST-001 | Happy path / negative / edge | Integration | Observable outcome | Fixtures / seed data |

### Regression And Analysis

- Regression suites to run (must stay green):
- Static analysis (tools or configs that must pass):
- Monitoring during rollout (logs, metrics, alerts to watch):
- Coverage comparison against baseline:

---

## Rollout And Migration

- Migration steps:
- Backwards compatibility:
- Rollback:

---

## References

- Issues or tickets:
- External docs or specs:
- Related ADRs:

---

## Filing Checklist

- [ ] File saved under `docs/ADR/ADR-XXXX-title-in-kebab-case.md` (not in `docs/templates/`).
- [ ] Status reflects the real state (`Proposed`, `Accepted`, `Rejected`, `Superseded`).
- [ ] Links to related features, tests, and ADRs are filled in.
- [ ] Diagram section contains at least one Mermaid diagram.
- [ ] Testing methodology is filled in with positive, negative, and edge flows plus pass criteria.
- [ ] New or updated automated tests exist for the changed behavior.
- [ ] All relevant tests are green and coverage did not fall below baseline.
- [ ] `docs/Architecture.md` is updated if module boundaries or interactions changed.
