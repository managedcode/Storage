---
title: ADR
description: "Architecture Decision Records (ADR) for ManagedCode.Storage."
keywords: "architecture decisions, ADR, ManagedCode.Storage, design decisions, providers, CloudKit, iCloud Drive"
permalink: /adr/
nav_order: 6
---

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

> Note: the GitHub Pages docs generator publishes every `docs/ADR/*.md` page automatically, but to make a page “visible” in the catalog/navigation you should also link it from this index.

## ADR List

- [ADR 0001: iCloud Drive Support vs CloudKit (Server-side)](0001-icloud-drive-support.md) — iCloud Drive is not implemented; CloudKit is supported as the official server-side Apple option.
- [ADR 0002: Standardize on `Result` / `Result<T>` for Public APIs](0002-result-model.md) — consistent success/failure model across providers and integrations.
- [ADR 0003: Implement Providers via `BaseStorage<TClient, TOptions>` (Template Method)](0003-base-storage-template.md) — shared provider behaviour lives in the base class; providers implement internals.
- [ADR 0004: Use Keyed DI + `Add*Storage...` Extensions for Multi-Storage Wiring](0004-keyed-di-and-extensions.md) — default + keyed registrations for multi-tenant/multi-region setups.
- [ADR 0005: Implement a VFS Overlay (`IVirtualFileSystem`) on Top of `IStorage`](0005-virtual-file-system-overlay.md) — file/directory API as an optional overlay, not part of `IStorage`.
- [ADR 0006: “Base-First” ASP.NET Controllers/Hubs with Minimal Routing Defaults](0006-aspnet-base-first.md) — consumers inherit and customize routing/auth without rigid defaults.
- [ADR 0007: Chunked Uploads Stage to Disk and Validate with CRC32](0007-chunked-uploads-disk-and-crc.md) — provider-agnostic large file uploads with resumability + integrity.
- [ADR 0008: Cloud Drive Providers Use SDK Client Wrappers + Options-Based Auth](0008-cloud-drive-clients-and-auth.md) — stable swap points and testability without heavy mocking.
- [ADR 0009: Integration-First Testing with Testcontainers + `HttpMessageHandler` Fakes](0009-testing-containers-and-httpfakes.md) — realistic flows without real cloud accounts.
- [ADR 0011: Normalize MIME Type Resolution via `MimeHelper`](0011-mimehelper-normalization.md) — consistent content-type logic across the repo.
- [ADR 0012: Modular Packaging (Core + Providers + Integrations)](0012-modular-packaging-structure.md) — keep dependencies small by shipping core/providers/integrations as separate packages.
