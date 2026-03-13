---
name: mcaf-security-baseline
description: "Apply baseline engineering security guidance: secrets handling, secure defaults, threat modelling references, and review checkpoints for auth, data flow, pipelines, and external integrations. Use when a change has security impact but does not require a full standalone AppSec engagement."
compatibility: "Requires repository access; may update security docs, ADRs, and verification steps."
---

# MCAF: Security Baseline

## Trigger On

- a change has security impact but does not need a full separate AppSec exercise
- the work touches auth, secrets, trust boundaries, data flow, or pipeline permissions
- the team needs secure-default guidance before implementing

## Value

- produce a concrete project delta: code, docs, config, tests, CI, or review artifact
- reduce ambiguity through explicit planning, verification, and final validation skills
- leave reusable project context so future tasks are faster and safer

## Do Not Use For

- a full standalone threat-modeling engagement
- generic code review with no security surface

## Inputs

- the changed boundary, data flow, or integration
- auth, secret, and permission model for the affected path
- current security docs, ADRs, or CI rules

## Quick Start

1. Read the nearest `AGENTS.md` and confirm scope and constraints.
2. Run this skill's `Workflow` through the `Ralph Loop` until outcomes are acceptable.
3. Return the `Required Result Format` with concrete artifacts and verification evidence.

## Workflow

1. Identify the security surface:
   - authn and authz
   - secrets
   - external inputs
   - storage and transport
   - pipeline permissions
2. Apply secure defaults and least privilege before adding behaviour.
3. If the change introduces a trust boundary, update or add an ADR and link the reasoning.
4. Pull the relevant security references, not the whole set.

## Deliver

- security-aware design or implementation guidance
- updated security checkpoints in docs, ADRs, or CI
- the right threat-model references for the impacted area

## Validate

- secrets are handled explicitly
- authn and authz assumptions are visible
- new trust boundaries are documented
- the change does not smuggle insecure defaults into the repo

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

- read `references/security.md` first
- open `references/rules-of-engagement.md` or `references/threat-modelling.md` only when they match the task

## Example Requests

- "Review the security baseline for this new OAuth flow."
- "We are adding a webhook. What baseline security work is required?"
- "Tighten secrets and pipeline permissions for this repo."
