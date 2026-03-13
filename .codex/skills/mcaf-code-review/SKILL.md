---
name: mcaf-code-review
description: "Prepare for, perform, or tighten code review workflow: PR scope, review checklist, reviewer expectations, and merge hygiene. Use when shaping pull requests, defining review policy, or auditing whether a change is review-ready."
compatibility: "Requires repository access; may update PR templates, review docs, or contribution guidance."
---

# MCAF: Code Review

## Trigger On

- shaping PR review policy
- preparing a change for review
- auditing whether a change is actually review-ready
- tightening reviewer expectations or templates

## Value

- produce a concrete project delta: code, docs, config, tests, CI, or review artifact
- reduce ambiguity through explicit planning, verification, and final validation skills
- leave reusable project context so future tasks are faster and safer

## Do Not Use For

- implementing the code change itself
- generic team-process work with no PR or review component

## Inputs

- the diff or planned PR scope
- tests, docs, and architecture notes affected by the change
- current review template or review policy, if any

## Quick Start

1. Read the nearest `AGENTS.md` and confirm scope and constraints.
2. Run this skill's `Workflow` through the `Ralph Loop` until outcomes are acceptable.
3. Return the `Required Result Format` with concrete artifacts and verification evidence.

## Workflow

1. Confirm the change is small enough to review coherently. Split if needed.
2. Check that tests, docs, and architecture notes moved with the code.
3. Review in this order:
   - behavioural risk
   - design and maintainability
   - test quality
   - operational or security impact
4. If the repo needs review policy or a template, define it in-repo.
5. Keep reviewer guidance concrete. Avoid vague "review carefully" language.

## Deliver

- review-ready pull requests
- review guidance that is specific and enforceable
- findings tied to behaviour, design, testing, and risk

## Validate

- the review guidance tells reviewers what to check, not just that they should check
- PR scope is understandable without opening the whole repo
- tests and docs are part of review readiness, not afterthoughts

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

- read `references/code-reviews.md` and `references/pull-requests.md` first
- open `references/pull-request-template.md`, `references/inclusion-in-code-review.md`, or `references/faq.md` only when needed

## Example Requests

- "Make our PR template less useless."
- "Is this change actually ready for code review?"
- "Define stricter review expectations for this repo."
