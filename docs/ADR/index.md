# Architecture Decisions (ADR)

Architecture Decision Records capture the **why** behind key technical choices. They are intentionally short, but must be specific enough that a future contributor can understand:

- what problem we had,
- what options we considered,
- what we decided and why,
- what the consequences are.

```mermaid
flowchart LR
  Problem[Problem] --> Options[Options]
  Options --> Decision[Decision]
  Decision --> Consequences[Consequences]
  Consequences --> Code[Code + Tests]
```

## ADR List

- [ADR 0001: iCloud Drive Support vs CloudKit (Server-side)](0001-icloud-drive-support.md) — iCloud Drive is not implemented; CloudKit is supported as the official server-side Apple option.
