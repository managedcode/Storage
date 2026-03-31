---
title: Setup
description: "How to clone, build, and run tests for ManagedCode.Storage."
keywords: "ManagedCode.Storage setup, .NET 10, dotnet restore, dotnet build, dotnet test, Docker, Testcontainers, Azurite, LocalStack, FakeGcsServer, SFTP"
permalink: /setup/
nav_order: 2
---

# Development Setup

## Prerequisites

- .NET SDK: **.NET 10** (`10.0.x`)
- Docker: required for Testcontainers-backed integration tests (Azurite / LocalStack / FakeGcsServer / SFTP)

## Workflow (Local)

```mermaid
flowchart LR
  A[Clone repo] --> B[dotnet restore]
  B --> C[dotnet build]
  C --> D[dotnet test]
  D --> E[dotnet format]
  D --> F[Docker daemon]
```

## Clone

```bash
git clone https://github.com/managedcode/Storage.git
cd Storage
```

## Restore / Build / Test

Canonical commands (see `AGENTS.md`):

```bash
dotnet restore ManagedCode.Storage.slnx
dotnet build ManagedCode.Storage.slnx --configuration Release
dotnet test Tests/ManagedCode.Storage.Tests/ManagedCode.Storage.Tests.csproj --configuration Release
```

## Testing Strategy

The full test strategy (suite layout, categories, containers, cloud-drive HTTP fakes) lives in `docs/Testing/strategy.md`:

- [Testing Strategy](../Testing/strategy.md)

## Formatting

```bash
dotnet format ManagedCode.Storage.slnx
```

## Notes

- Start Docker Desktop (or your Docker daemon) before running the full test suite.
- AWS and Orleans integration tests intentionally pin LocalStack to `localstack/localstack:4.14.0`; do not switch them back to `latest`, because the end-of-March 2026 `latest` image became auth-gated and breaks CI without a token.
- GitHub Actions `build-and-test` and `Release` intentionally exclude `Category=BrowserStress`; run that browser-hosted `1 GiB` stress lane locally with `dotnet test ... --filter "Category=BrowserStress"` when you specifically need it.
- Never commit secrets (cloud keys, OAuth tokens, connection strings). Use environment variables or user secrets.
- Credentials for cloud-drive providers are documented in `docs/Development/credentials.md`.
