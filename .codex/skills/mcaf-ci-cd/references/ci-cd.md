# CI/CD

MCAF treats CI/CD as a safety system, not a release ritual.

## Core Principles

- every mergeable change passes automated verification
- the main branch stays shippable
- deployment flow is explicit and reproducible
- rollback or safe failure is designed before release

## Minimum Pipeline Shape

1. validate pull requests with build, tests, and analyzers
2. produce a versioned artifact from trusted code
3. deploy to non-production automatically
4. promote to production through an explicit policy

## Guardrails

- keep secrets out of pipeline definitions
- keep permissions least-privilege
- prefer checked-in scripts over long inline shell blocks
- treat AI-generated workflow YAML as draft content until reviewed

## Rollback Expectations

- define what constitutes a failed rollout
- define who or what can stop promotion
- define the fastest safe rollback path

## What Good Looks Like

- a new engineer can explain every stage
- failures are obvious and actionable
- release rules live in the repo, not in memory
