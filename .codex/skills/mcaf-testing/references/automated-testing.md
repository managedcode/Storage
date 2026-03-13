# Automated Testing

Automated testing in MCAF is evidence, not ceremony.

## Core Rules

- every non-trivial behaviour change gets automated coverage
- default to TDD: write the failing test first, make it pass, then refactor
- pick the highest-value test level that proves the change
- tests prove user-visible or caller-visible flow, not just internal implementation
- start small, then widen verification only when necessary
- flaky tests are bugs

## TDD First

- use Red -> Green -> Refactor as the default workflow for new behaviour and bug fixes
- bug fixes start with a failing regression test that reproduces the reported issue
- when strict TDD is blocked by legacy code, third-party limits, or missing harnesses, document the reason and add the automated test in the same change before calling the work done

## Test-Level Selection

- unit tests for isolated logic
- integration tests for real boundary interaction
- API tests for public contracts
- UI or end-to-end tests for user-visible flows

## User-Flow Coverage

- tests must prove the main user flow or caller-visible system flow changed by the work
- for user-visible behaviour, cover the happy path and the most important failure or edge path at integration, API, or UI level
- for cross-boundary changes, prefer tests that exercise the real contract between components over isolated implementation checks
- if the change is not directly user-facing, prove the system flow at the boundary that another module, job, or API caller actually uses

## Coverage Norms

- changed production code should ship with at least 80% line coverage
- where branch coverage is available, changed production code should reach at least 70% branch coverage
- critical flows and public contracts should reach at least 90% line coverage with explicit success and failure assertions
- overall repository or module coverage must not go down without a written exception in the relevant task, feature doc, or ADR
- coverage numbers are guardrails for finding gaps; they do not replace scenario coverage or user-flow verification

## Good Test Characteristics

- assert outcomes, not internal trivia
- cover positive, negative, and edge behaviour where it matters
- are deterministic in local and CI runs
- make failures easy to diagnose

## Common Smells

- mocks, fakes, or stubs hiding broken integration paths
- huge suites run before any focused verification
- tests that only prove implementation detail
- tests nobody trusts enough to keep
