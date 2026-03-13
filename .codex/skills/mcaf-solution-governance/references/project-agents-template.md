# Project-Local AGENTS.md

> Template for a project or module root inside a multi-project solution. Copy into the project root as `AGENTS.md`, then replace placeholders with real values.

Project: TODO
Owned by: TODO

Parent: `../AGENTS.md`

## Purpose

- What this project or module does:
- Why it exists in the solution:

## Entry Points

- `...`
- `...`

## Boundaries

- In scope:
- Out of scope:
- Protected or high-risk areas:

## Project Commands

- `build`: `...`
- `test`: `...`
- `format`: `...`
- `analyze`: `...` (delete if not used)

For .NET projects also document:

- the active test framework
- the runner model: `VSTest` or `Microsoft.Testing.Platform`
- whether analyzer severity lives in the repo-root `.editorconfig`

## Applicable Skills

- `...`
- `...`

For .NET projects this usually includes:

- `mcaf-testing`
- exactly one of `mcaf-dotnet-xunit`, `mcaf-dotnet-tunit`, or `mcaf-dotnet-mstest`
- `mcaf-dotnet-quality-ci`
- `mcaf-dotnet-complexity` when complexity gates are part of done

## Local Constraints

- Stricter maintainability limits, if any:
  - `file_max_loc`: `...`
  - `type_max_loc`: `...`
  - `function_max_loc`: `...`
  - `max_nesting_depth`: `...`
- Required local docs:
- Local exception policy:

## Local Rules

- Project-specific rules go here.
- Local rules may tighten root rules, but must not weaken them silently.
