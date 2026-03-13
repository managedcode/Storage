# Maintainability Limits and Exceptions

Put numeric limits in `AGENTS.md`, not in the framework guide or skill metadata.

Required keys:

- `file_max_loc`
- `type_max_loc`
- `function_max_loc`
- `max_nesting_depth`
- `exception_policy`

Starter example:

```text
file_max_loc: 400
type_max_loc: 200
function_max_loc: 50
max_nesting_depth: 3
exception_policy: Document the reason, scope, and removal plan in the nearest ADR, feature doc, or local AGENTS.md.
```

Document an exception only when all of these are true:

1. Splitting now would create a worse boundary or break a needed abstraction.
2. The exception is local and well understood.
3. The code still has clear responsibilities.
4. There is a follow-up path to remove or reduce the exception.

Acceptable exception note:

```text
Exception: `OrderImportCoordinator` exceeds `type_max_loc` temporarily because the import flow is being split in two follow-up tasks. Scope is limited to `src/import/`. Remove after `ImportStepPlanner` and `ImportFailureReporter` are extracted.
```
