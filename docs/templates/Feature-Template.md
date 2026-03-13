# Feature: FeatureName

> TEMPLATE ONLY — remove this note and replace all placeholder text before saving as a real feature doc under `docs/Features/`.

Links:  
Architecture: `docs/Architecture.md`  
Modules:  
ADRs: `docs/ADR/...`

---

## Implementation Plan (Step-by-Step)

> TEMPLATE ONLY — replace these checkboxes with real implementation steps for this feature and keep them updated while implementing.

- [ ] Analyze current behavior (facts)
- [ ] Finalize spec (rules, flows, diagram, verification)
- [ ] Implement the feature (smallest safe increments)
- [ ] Add or update automated tests for each scenario (happy, negative, edge)
- [ ] Run verification commands (build, test, format, coverage) and record results
- [ ] Update docs (feature, ADRs, architecture overview) and close the checklist

---

## Purpose

Short description of the business problem and value.

---

## Stakeholders (Who Needs This To Be Clear)

| Role | What they need from this spec |
| --- | --- |
| Product / Owner | Scope, acceptance criteria, user-visible behavior |
| Engineering | Modules, contracts, data, error handling, edge cases |
| DevOps / SRE | Config, rollout plan, monitoring or alerts, rollback |
| QA | Executable test flows (positive, negative, edge) plus environment |

---

## Scope

### In Scope

- Item

### Out Of Scope

- Item

---

## Business Rules

- Rule 1
- Rule 2
- Rule 3

---

## User Flows

### Primary Flows

1. Flow name
   - Actor:
   - Trigger:
   - Steps:
   - Result:

### Edge Cases

- Edge case -> Expected behavior

---

## System Behavior

- Entry points: API endpoints, UI, events, scheduled jobs
- Reads from:
- Writes to:
- Side effects or emitted events:
- Idempotency:
- Error handling:
- Security or permissions:
- Feature flags or toggles:
- Performance or SLAs:
- Observability:

---

## Diagrams

At least one Mermaid diagram is mandatory in every real feature doc.

> TEMPLATE ONLY — keep Mermaid simple and make sure it renders in the repo.

```mermaid
flowchart LR
```

---

## Verification

This section is mandatory: describe how to test the feature.

### Test Environment

- Environment or stack:
- Data and reset strategy:
- External dependencies:

### Testing Methodology

- Main flows that must be proven end-to-end:
- Positive flows that must pass:
- Negative flows that must fail safely and predictably:
- Edge, boundary, or unexpected flows that must be covered:
- Test realism requirements:
- Coverage baseline requirement:
- Pass criteria for considering the task done:

### Test Commands

- build: (paste from `AGENTS.md`)
- test: (paste from `AGENTS.md`)
- format: (paste from `AGENTS.md`)
- coverage: (paste from `AGENTS.md`; delete if none)

### Test Flows

**Positive scenarios**

| ID | Description | Level (Unit / Int / API / UI) | Expected result | Data / Notes |
| --- | --- | --- | --- | --- |
| POS-001 | Happy path | Integration | Outcome observed via public interface | Data / fixtures |

**Negative scenarios**

| ID | Description | Level (Unit / Int / API / UI) | Expected result | Data / Notes |
| --- | --- | --- | --- | --- |
| NEG-001 | Validation failure | API | Error response or code | Invalid input example |

**Edge cases**

| ID | Description | Level (Unit / Int / API / UI) | Expected result | Data / Notes |
| --- | --- | --- | --- | --- |
| EDGE-001 | Boundary condition | Integration | Expected behavior at boundary | Data / timing notes |

### Test Mapping

- Integration tests:
- API tests:
- UI or E2E tests:
- Unit tests:
- Static analysis:
- Coverage comparison against baseline:

### Non-Functional Checks

Include this section only if it applies to this feature; otherwise remove it.

- Performance or load (tool, threshold, command):
- Security or privacy (threats to verify):
- Observability (log or metric assertions):

---

## Definition Of Done

- Behavior matches the rules and flows in this document.
- Diagram section contains at least one Mermaid diagram that renders in the repo.
- All test flows above are covered by automated tests as applicable.
- Testing methodology is written down and matches the implemented tests.
- New or updated automated tests were added for the changed behavior.
- Positive, negative, and edge flows are all covered where applicable.
- Test and build commands listed above run clean in local and CI environments.
- Coverage is at least at the pre-change baseline or better.
- Documentation updated: this feature doc, related ADRs, testing or API docs, architecture docs, and `AGENTS.md` if rules or patterns changed.

---

## References

- ADRs: `docs/ADR/...`
- API: `docs/API/...`
- Architecture: `docs/Architecture.md`
- Testing: `docs/Testing/...`
- Code: modules / namespaces
