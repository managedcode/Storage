# Security Baseline

Baseline security guidance should be generic, practical, and tied to real change risk.

## Review Areas

- authentication and authorization
- secrets and credentials
- external inputs and trust boundaries
- storage, transport, and sensitive data handling
- CI/CD permissions and supply-chain exposure

## Default Expectations

- use least privilege
- keep secrets out of source and logs
- prefer secure defaults over opt-in hardening
- document new trust boundaries and security assumptions

## Escalate When

- the change introduces a new public attack surface
- the design changes identity or permission models
- customer or regulated data handling changes materially
- the team cannot explain the threat model in plain language
