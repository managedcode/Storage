---
name: mcaf-ci-cd
description: "Design or refine CI/CD workflows, quality gates, release flow, and safe AI-assisted pipeline authoring. Use when adding or changing build pipelines, release stages, IaC-driven environments, or deployment rollback policy."
compatibility: "Requires repository access; may update CI workflows, pipeline docs, and release guidance."
---

# MCAF: CI/CD

## Trigger On

- adding or changing CI workflows
- defining release flow or rollback policy
- tightening pipeline quality gates
- writing or reviewing AI-assisted pipeline YAML

## Value

- produce a concrete project delta: code, docs, config, tests, CI, or review artifact
- reduce ambiguity through explicit planning, verification, and final validation skills
- leave reusable project context so future tasks are faster and safer

## Do Not Use For

- feature-level testing with no pipeline or release impact
- general source-control policy without CI/CD changes

## Inputs

- the current pipeline and release flow
- real build, test, analyze, and deploy steps
- environment and rollback constraints

## Quick Start

1. Read the nearest `AGENTS.md` and confirm scope and constraints.
2. Run this skill's `Workflow` through the `Ralph Loop` until outcomes are acceptable.
3. Return the `Required Result Format` with concrete artifacts and verification evidence.

## Workflow

1. Define the target flow first:
   - PR validation
   - integration-branch gates
   - non-production deployment
   - production promotion or release
2. Keep pipelines reviewable:
   - explicit build, test, and analyze steps
   - least-privilege secrets and permissions
   - rollback or fail-safe strategy
3. Treat AI-generated YAML as draft content until it is reviewed and validated.
4. For .NET repositories, make the quality gate explicit:
   - formatting ownership
   - analyzer ownership
   - coverage and report generation
   - runner model (`VSTest` or `Microsoft.Testing.Platform`)
5. Pull only the references that match the current delivery problem.

## Deliver

- CI/CD changes that are explicit, reproducible, and reviewable
- release documentation with rollback thinking
- pipeline rules aligned with MCAF verification

## Validate

- every stage has a clear purpose and failure signal
- rollback or safe failure is explicit
- secrets and permissions are minimized
- the pipeline matches the repo’s actual verification model

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

- read `references/ci-cd.md` first
- for .NET quality gates, use `mcaf-dotnet-quality-ci`

## Example Requests

- "Design CI for this repo."
- "Tighten our deployment gates and rollback story."
- "Review this GitHub Actions YAML before we trust it."
