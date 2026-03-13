---
name: mcaf-architecture-overview
description: "Create or update `docs/Architecture.md` as the global architecture map for a solution. Use when bootstrapping a repo, onboarding, or changing modules, boundaries, or contracts. Keep it navigational and use `references/overview-template.md` for scaffolding."
compatibility: "Requires repository write access; produces Markdown docs with Mermaid diagrams."
---

# MCAF: Architecture Overview

## Trigger On

- create the first repo-wide architecture map
- modules, boundaries, interfaces, or ownership changed
- onboarding is slow because there is no short "start here" system map

## Value

- produce a concrete project delta: code, docs, config, tests, CI, or review artifact
- reduce ambiguity through explicit planning, verification, and final validation skills
- leave reusable project context so future tasks are faster and safer

## Do Not Use For

- recording a single architecture decision with alternatives
- writing feature-level behaviour details

## Inputs

- current solution layout and entry points
- existing ADRs, feature docs, and boundary docs
- the nearest `AGENTS.md` files

## Quick Start

1. Read the nearest `AGENTS.md` and confirm scope and constraints.
2. Run this skill's `Workflow` through the `Ralph Loop` until outcomes are acceptable.
3. Return the `Required Result Format` with concrete artifacts and verification evidence.

## Workflow

1. Start from the current `docs/Architecture.md`; if it is missing, scaffold it from `references/overview-template.md`.
2. Build a short navigational overview:
   - system or module map
   - key boundaries and contracts
   - scoping hints
   - links to ADRs, feature docs, and high-signal code paths
3. Use only real names from the repo. No placeholders like "Module A".
4. Prefer Mermaid diagrams plus a tiny link index over long prose.
5. Split diagrams by boundary if the map becomes noisy.

## Deliver

- `docs/Architecture.md`
- a short architecture map that routes the reader to deeper docs

## Validate

- diagram nodes use real repo names
- every important box or boundary links to deeper material
- the file stays navigational instead of becoming an inventory dump
- the overview lets a new agent scope work without reading the whole repo

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

- use `references/overview-template.md` only when scaffolding the file

## Example Requests

- "Create an architecture overview for this repo."
- "Update the overview after splitting the API and worker."
- "Make onboarding easier by adding a real module map."
