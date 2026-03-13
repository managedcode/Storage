---
name: mcaf-dotnet
description: "Primary entry skill for C# and .NET tasks. Detect the repo's language version, test runner, quality stack, and architecture rules; route to the right .NET subskills; and run the repo-defined post-change quality pass after any code change. Use when the user asks to implement, debug, review, or refactor .NET code, or asks which .NET skill or toolchain should apply."
compatibility: "Requires a .NET solution or project; respects root and local `AGENTS.md` first."
---

# MCAF: .NET

## Trigger On

- writing, reviewing, debugging, or refactoring C# or .NET code
- deciding which .NET skill should be used for a task
- modernizing a .NET codebase while keeping language and runtime compatibility
- verifying .NET changes with analyzers, formatting, tests, and coverage

## Value

- produce a concrete project delta: code, docs, config, tests, CI, or review artifact
- reduce ambiguity through explicit planning, verification, and final validation skills
- leave reusable project context so future tasks are faster and safer

## Do Not Use For

- non-.NET repositories
- a single-tool task where one exact .NET tool skill already covers the whole request

## Inputs

- the nearest `AGENTS.md`
- solution and project files
- `Directory.Build.*` files
- the repo-root `.editorconfig` and any nested `.editorconfig` files that apply
- target `TFM`, `LangVersion`, SDK version, test packages, and analyzer packages

## Quick Start

1. Read the nearest `AGENTS.md` and confirm scope and constraints.
2. Run this skill's `Workflow` through the `Ralph Loop` until outcomes are acceptable.
3. Return the `Required Result Format` with concrete artifacts and verification evidence.

## Workflow

1. Detect the real stack before changing code or commands:
   - `TFM` or `TFMs`
   - explicit `LangVersion` or the default implied by the SDK and target framework
   - test framework: xUnit, TUnit, or MSTest
   - runner model: `VSTest` or `Microsoft.Testing.Platform`
   - installed analyzers, formatters, coverage tools, and architecture-test libraries
2. Route framework mechanics through exactly one matching test skill:
   - `mcaf-dotnet-xunit`
   - `mcaf-dotnet-tunit`
   - `mcaf-dotnet-mstest`
3. Route quality and policy through the matching skill:
   - `mcaf-dotnet-quality-ci`
   - `mcaf-dotnet-analyzer-config`
   - `mcaf-dotnet-complexity`
   - tool-specific skills such as `mcaf-dotnet-format`, `mcaf-dotnet-roslynator`, `mcaf-dotnet-stylecop-analyzers`, `mcaf-dotnet-meziantou-analyzer`, `mcaf-dotnet-coverlet`, `mcaf-dotnet-reportgenerator`, `mcaf-dotnet-resharper-clt`, `mcaf-dotnet-netarchtest`, `mcaf-dotnet-archunitnet`, `mcaf-dotnet-codeql`, `mcaf-dotnet-csharpier`, and `mcaf-dotnet-stryker`
4. Route design and structure through:
   - `mcaf-solid-maintainability` for SOLID, SRP, cohesion, and maintainability limits
   - `mcaf-architecture-overview` when system or module boundaries, contracts, or architecture docs need work
   - `mcaf-dotnet-features` when modern C# feature selection or language-version compatibility matters
5. Write or review code using the newest stable C# features the repo actually supports.
6. After any .NET code change, run the repo-defined post-change quality pass from narrow to broad:
   - `format`
   - `build`
   - `analyze`
   - focused `test`
   - broader `test`
   - `coverage` and report generation when configured
   - extra configured gates such as Roslynator, StyleCop, Meziantou, ReSharper CLT, NetArchTest, ArchUnitNET, CodeQL, CSharpier, or Stryker
7. If the repo does not define these commands clearly, tighten `AGENTS.md` before continuing so later agents stop guessing.
8. Do not introduce preview language features unless the repo explicitly opts into preview in project or MSBuild settings.

## Bootstrap When Missing

When a requested .NET gate or tool is missing in this repo, do not stop at "not configured":

1. Detect whether the tool is already present in project files, tool manifests, or CI.
2. If the tool is optional and repo policy is unclear, ask whether to enable it as a default gate.
3. If approved or already required by policy, install and wire it through the matching tool skill.
4. Update `AGENTS.md` so future agents have exact commands and stop guessing.
5. Run one verify pass and return `status: configured` or `status: improved`.
6. If the tool is intentionally out of scope for this repo, return `status: not_applicable` with reason and route.


## Deliver

- .NET changes that use the right framework and tool skills
- version-compatible modern C# code
- a completed post-change verification pass, not only green tests

## Validate

- only the skills relevant to the current .NET task were opened
- `VSTest` and `Microsoft.Testing.Platform` assumptions were not mixed
- the language features used are supported by the repo's real `TFM` and `LangVersion`
- format, analyzers, tests, and any configured extra gates were all considered before completion

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

- read `references/skill-routing.md` first
- read `references/task-flow.md` when the task includes implementation, refactoring, or review

## Example Requests

- "Work on this .NET feature and use the right skills."
- "Which .NET skills should open for this repo?"
- "Refactor this C# code and run the full quality pass after."
