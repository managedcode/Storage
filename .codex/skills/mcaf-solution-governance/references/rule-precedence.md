# Rule Precedence

Use this order whenever both root and local governance files exist:

1. Read the solution-root `AGENTS.md`.
2. Read the nearest local `AGENTS.md`.
3. Apply the stricter rule when both files cover the same topic.
4. If a local rule appears weaker than root policy, stop and clarify it before editing code.
5. Document justified exceptions explicitly in the nearest durable doc:
   - local `AGENTS.md`
   - ADR
   - feature doc

Root `AGENTS.md` owns:

- cross-solution workflow
- global commands
- global skill catalog
- default maintainability limits
- exception policy shape

Local `AGENTS.md` owns:

- entry points
- project boundaries
- local commands
- stricter local limits
- applicable skills
- project-specific risks
