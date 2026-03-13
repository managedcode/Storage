# Correlation IDs

Use a correlation ID when one logical action crosses process or service boundaries.

## Rules

- create or accept a correlation ID at the boundary
- propagate it across downstream calls, messages, and background work
- include it in logs and diagnostic output
- do not overload it with business meaning

## When It Matters Most

- request chains across multiple services
- async workflows
- retries and dead-letter investigation
