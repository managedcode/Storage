---
name: mcaf-dotnet-code-analysis
description: "Use the free built-in .NET SDK analyzers and analysis levels. Use when a .NET repo needs first-party code analysis, `EnableNETAnalyzers`, `AnalysisLevel`, or warning policy wired into build and CI."
compatibility: "Requires a .NET SDK-based repository; respects the repo's `AGENTS.md` commands first."
---

# MCAF: .NET Code Analysis

## Trigger On

- the repo wants first-party .NET analyzers
- CI should fail on analyzer warnings
- the team needs `AnalysisLevel` or `AnalysisMode` guidance

## Value

- produce a concrete project delta: code, docs, config, tests, CI, or review artifact
- reduce ambiguity through explicit planning, verification, and final validation skills
- leave reusable project context so future tasks are faster and safer

## Do Not Use For

- third-party analyzer selection by itself
- formatting-only work

## Inputs

- the nearest `AGENTS.md`
- project files or `Directory.Build.props`
- current analyzer severity policy

## Quick Start

1. Read the nearest `AGENTS.md` and confirm scope and constraints.
2. Run this skill's `Workflow` through the `Ralph Loop` until outcomes are acceptable.
3. Return the `Required Result Format` with concrete artifacts and verification evidence.

## Workflow

1. Start with SDK analyzers before adding third-party packages.
2. Enable or document:
   - `EnableNETAnalyzers`
   - `AnalysisLevel`
   - `AnalysisMode`
   - warning policy such as `TreatWarningsAsErrors`
3. Keep per-rule severity in the repo-root `.editorconfig`.
4. Use `dotnet build` as the analyzer execution gate in CI.
5. Add third-party analyzers only for real gaps that first-party rules do not cover.

## Bootstrap When Missing

If first-party .NET code analysis is requested but not configured yet:

1. Detect current state:
   - `dotnet --info`
   - `rg -n "EnableNETAnalyzers|AnalysisLevel|AnalysisMode|TreatWarningsAsErrors" -g '*.csproj' -g 'Directory.Build.*' .`
2. Treat SDK analyzers as built-in functionality, not as a separate third-party install path.
3. Enable the needed properties in the solution's MSBuild config, typically in `Directory.Build.props` or the target project file:
   - `EnableNETAnalyzers`
   - `AnalysisLevel`
   - `AnalysisMode` when needed
   - warning policy such as `TreatWarningsAsErrors`
4. Keep rule-level severity in the repo-root `.editorconfig`.
5. Run `dotnet build SOLUTION_OR_PROJECT` and return `status: configured` or `status: improved`.
6. If the repo intentionally defers analyzer policy to another documented build layer, return `status: not_applicable`.

## Deliver

- first-party analyzer policy that is explicit and reviewable
- build-time analyzer execution for CI

## Validate

- analyzer behavior is driven by repo config, not IDE defaults
- CI can reproduce the same warnings and errors locally

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

- read `references/code-analysis.md` first

## Example Requests

- "Turn on built-in .NET analyzers."
- "Make analyzer warnings fail the build."
- "Set the right `AnalysisLevel` for this repo."
