---
name: mcaf-solid-maintainability
description: "Apply SOLID, SRP, cohesion, composition-over-inheritance, and small-file discipline to code changes. Use when refactoring large files or classes, setting maintainability limits in `AGENTS.md`, documenting justified exceptions, or reviewing design quality."
compatibility: "Requires repository write access; uses maintainability limits from root or local `AGENTS.md`."
---

# MCAF: SOLID Maintainability

## Trigger On

- files, classes, or functions are too large or too coupled
- maintainability limits in `AGENTS.md` need to be added or tightened
- a change needs a justified temporary exception

## Value

- produce a concrete project delta: code, docs, config, tests, CI, or review artifact
- reduce ambiguity through explicit planning, verification, and final validation skills
- leave reusable project context so future tasks are faster and safer

## Do Not Use For

- writing architecture docs without touching code structure or policy
- cosmetic formatting-only edits

## Inputs

- the nearest `AGENTS.md`
- the code under change
- current testing seams and dependency boundaries

## Quick Start

1. Read the nearest `AGENTS.md` and confirm scope and constraints.
2. Run this skill's `Workflow` through the `Ralph Loop` until outcomes are acceptable.
3. Return the `Required Result Format` with concrete artifacts and verification evidence.

## Workflow

1. Read the active values for:
   - `file_max_loc`
   - `type_max_loc`
   - `function_max_loc`
   - `max_nesting_depth`
   - `exception_policy`
2. Evaluate the change through SOLID:
   - single responsibility
   - explicit dependencies
   - composition before inheritance
   - boundaries that are easy to test
3. Remove hardcoded values and inline string literals from implementation code by moving them into named constants, enums, configuration, or dedicated types.
4. Split by responsibility, not by arbitrary line count alone.
5. If a limit must be exceeded temporarily, document the exception exactly where `exception_policy` requires it.

## Deliver

- smaller, more cohesive code
- updated maintainability policy when repo rules changed
- explicit exception records when a temporary breach is justified

## Validate

- size limits are respected or explicitly waived
- responsibilities are clearer after the change
- the refactor improves testability instead of only moving lines around
- literals that matter are named once and reused instead of repeated inline
- no numeric limit was moved into framework prose or skill metadata

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

- read `references/limits-and-exceptions.md` first
- open `references/maintainability.md` for broader design guidance
- open `references/exception-handling.md` when documenting a temporary breach

## Example Requests

- "Split this 700-line service into cohesive parts."
- "Add maintainability limits to AGENTS."
- "Refactor this class to follow SOLID and document the one exception."

## Guardrails

- numeric limits belong in `AGENTS.md`, not in the framework guide or skill metadata
- a justified exception is a debt record, not a permanent escape hatch
