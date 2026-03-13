# Maintainability

Maintainability is the cost of changing the system safely.

## Design Signals

- files and types stay cohesive
- dependencies are explicit
- responsibilities are easy to explain
- tests can exercise behaviour without contortions
- hardcoded values and inline string literals do not leak through business logic

## Refactor Triggers

- a file grows beyond the local limits in `AGENTS.md`
- a type carries multiple reasons to change
- a function mixes orchestration, branching, and domain logic
- every change forces edits in too many places

## Practical Rule

Split by responsibility first.
Line-count limits are a symptom detector, not the design principle.
If a value or label matters, give it a name through a constant, enum, config entry, or dedicated type instead of repeating a literal inline.
