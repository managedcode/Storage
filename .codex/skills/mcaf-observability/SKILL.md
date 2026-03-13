---
name: mcaf-observability
description: "Design or improve observability for application and delivery flows: logs, metrics, traces, correlation, alerts, and operational diagnostics. Use when a change affects runtime visibility, failure diagnosis, SLOs, or alerting."
compatibility: "Requires repository access; may update code, dashboards-as-code, alerting docs, or operational guidance."
---

# MCAF: Observability

## Trigger On

- a change affects runtime visibility or failure diagnosis
- logs, metrics, traces, or alerts are missing or vague
- the team cannot answer "how will we know this broke?"

## Value

- produce a concrete project delta: code, docs, config, tests, CI, or review artifact
- reduce ambiguity through explicit planning, verification, and final validation skills
- leave reusable project context so future tasks are faster and safer

## Do Not Use For

- feature behaviour work with no runtime visibility impact
- generic monitoring talk with no concrete flow to instrument

## Inputs

- the critical user or system flow under change
- current logs, metrics, traces, dashboards, and alerts
- operator expectations for diagnosis and response

## Quick Start

1. Read the nearest `AGENTS.md` and confirm scope and constraints.
2. Run this skill's `Workflow` through the `Ralph Loop` until outcomes are acceptable.
3. Return the `Required Result Format` with concrete artifacts and verification evidence.

## Workflow

1. Identify the critical user or system flow that needs visibility.
2. Define what must be observable:
   - success and failure
   - latency and throughput
   - correlation across boundaries
   - actionable alerting
3. Treat observability as part of done, not an afterthought.
4. Load only the references that match the affected runtime concern.

## Deliver

- observability requirements for the changed flow
- updated logging, metrics, traces, or alerting guidance
- clear operator and engineer visibility expectations

## Validate

- a failure can be detected and diagnosed from the chosen signals
- alerts are actionable, not noise
- cross-boundary correlation is possible where the flow needs it
- the observability plan matches user impact and operator needs

## Ralph Loop

Use the Ralph Loop for every task, including docs, architecture, testing, and tooling work.

1. Plan first (mandatory):
   - analyze current state
   - define target outcome, constraints, and risks
   - write a detailed execution plan
   - list final validation skills to run at the end, with order and reason
2. Execute one planned step and produce a concrete delta.
3. Review the result and capture findings with actionable next fixes.
4. Apply fixes in small batches and rerun the relevant checks or review steps.
5. Update the plan after each iteration.
6. Repeat until outcomes are acceptable or only explicit exceptions remain.
7. If a dependency is missing, bootstrap it or return `status: not_applicable` with explicit reason and fallback path.

### Required Result Format

- `status`: `complete` | `clean` | `improved` | `configured` | `not_applicable` | `blocked`
- `plan`: concise plan and current iteration step
- `actions_taken`: concrete changes made
- `validation_skills`: final skills run, or skipped with reasons
- `verification`: commands, checks, or review evidence summary
- `remaining`: top unresolved items or `none`

For setup-only requests with no execution, return `status: configured` and exact next commands.

## Load References

- read `references/observability.md` first
- open `references/alerting.md`, `references/best-practices.md`, `references/correlation-id.md`, `references/log-vs-metric-vs-trace.md`, or `references/pitfalls.md` only when needed

## Example Requests

- "Add observability requirements for this background worker."
- "We have logs but still cannot debug failures. Fix the plan."
- "Define alerts and traces for this API flow."
