# ADR Formats (templates)

This file is for **copy/paste**. Pick one of the templates below and fill it with real repo facts.

Rules (MCAF):

- ADRs are self-contained (no “as discussed”).
- At least one Mermaid diagram is mandatory.
- ADRs define testable invariants + verification commands (not vibes).

---

## Template 1: MCAF ADR (full)

Use this as the default template. Save as `docs/ADR/ADR-XXXX-title-in-kebab-case.md`.

````md
# ADR-XXXX: Title

Status: Proposed | Accepted | Implemented | Rejected | Superseded
Date: YYYY-MM-DD
Related Features: `docs/Features/...` (recommended)
Supersedes: `docs/ADR/ADR-....md` (delete if none)
Superseded by: `docs/ADR/ADR-....md` (delete if none)

Rules:

- This ADR is **self-contained** — avoid “as discussed”; include all critical context and links.
- At least **one Mermaid diagram is mandatory** (boundaries/modules/interactions for this decision).

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

## Diagram (Mandatory)

```mermaid
%% Show the boundaries/modules that change, and how they interact.
%% Prefer 1 clear diagram over many noisy ones.
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

## Verification (Mandatory: describe how to test this decision)

### Objectives

- What behaviour / qualities must be proven.
- Which invariants from this ADR must be encoded as tests (happy path + negative/forbidden + edge cases).
- Link each objective/scenario to the specific automated test(s) that prove it.

### Test environment

- Environment (local compose / staging / prod-like):
- Data and reset strategy (seed data, migrations, rollback plan):
- External dependencies (real / sandbox / test environment required):

### Test commands

- build: (paste from `AGENTS.md`)
- test: (paste from `AGENTS.md`)
- format: (paste from `AGENTS.md`)
- coverage: (paste from `AGENTS.md` if separate; otherwise delete)

### New or changed tests

| ID | Scenario | Level (Unit / Int / API / UI) | Expected result | Notes / Data |
| --- | --- | --- | --- | --- |
| TST-001 | Happy path / negative / edge | Integration | Observable outcome | Fixtures / seed data |

### Regression and analysis

- Regression suites to run (must stay green):
- Static analysis (tools/configs that must pass):
- Monitoring during rollout (logs/metrics/alerts to watch):

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
- [ ] `docs/Architecture.md` updated if module boundaries or interactions changed.
````

---

## Template 2: Mini ADR (small, safe decision)

Use this when the decision is real but the scope is contained (still needs a diagram + verification).

````md
# ADR-XXXX: Title

Status: Proposed | Accepted | Implemented | Rejected | Superseded
Date: YYYY-MM-DD
Related Features: `docs/Features/...` (delete if none)

## Decision

- One sentence.

## Why (context + constraints)

- Why now:
- Constraints:

## Diagram (Mandatory)

```mermaid
%% Boundaries/modules that change + their interaction.
```

## Alternatives (at least one)

- Alternative A (why not):

## Consequences

- Positive:
- Negative / risks + mitigations:

## Verification (Mandatory)

- Invariants to test:
- Tests to add/update:
- Commands to run (from `AGENTS.md`):
````

---

## Template 3: Options Matrix ADR (when choosing between 2–4 options)

Use this when you need to evaluate trade-offs explicitly and keep the decision auditable.

````md
# ADR-XXXX: Title

Status: Proposed | Accepted | Implemented | Rejected | Superseded
Date: YYYY-MM-DD

## Decision

- One sentence.

## Decision drivers (what matters)

- Driver 1:
- Driver 2:

## Options

| Option | Summary | Pros | Cons | Risk | Why/why not |
| --- | --- | --- | --- | --- | --- |
| A | | | | | |
| B | | | | | |

## Diagram (Mandatory)

```mermaid
%% Diagram the chosen option (and the most important alternative if helpful).
```

## Verification (Mandatory)

- Invariants to protect:
- Test plan:
````
