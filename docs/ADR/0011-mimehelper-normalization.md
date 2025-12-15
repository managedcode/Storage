---
keywords: "MimeHelper, MIME type, content-type, ManagedCode.MimeTypes, UploadOptions, ADR"
---

# ADR 0011: Normalize MIME Type Resolution via `MimeHelper`

## Status

Accepted — 2025-12-15

## Context

Content-type (`Content-Type`) influences behaviour across providers and integrations, so we need one consistent, predictable MIME rule across the repository.

- object metadata and download responses,
- browser behaviour (inline vs attachment),
- downstream processing (virus scanning, previews, indexing).

Different providers and platforms handle MIME detection differently, so we want one consistent rule.

## Problem

If each provider (or each integration surface) implements its own MIME resolution logic:

- behaviour diverges across providers,
- consumers see inconsistent metadata,
- tests and docs become harder to reason about.

## Decision

All MIME lookups flow through `ManagedCode.MimeTypes.MimeHelper`:

- When `UploadOptions.MimeType` is not supplied, `BaseStorage` derives a default via `MimeHelper` (`GetMimeType(...)` or `MimeHelper.TEXT` for string uploads).
- Providers should use the resolved `options.MimeType` instead of implementing ad-hoc MIME detection.

```mermaid
flowchart LR
  Input[FileName / Extension / Text] --> Base[BaseStorage]
  Base --> Mime[MimeHelper]
  Mime --> Opts[UploadOptions.MimeType]
  Opts --> Provider[Provider upload]
```

## Alternatives Considered

1. **Per-provider MIME detection**
   - Pros: providers can tailor behaviour to SDK capabilities.
   - Cons: inconsistent metadata; duplicated logic.
2. **Use framework-only helpers (`FileExtensionContentTypeProvider`, etc.)**
   - Pros: common in ASP.NET.
   - Cons: not always available/consistent in all target contexts; would duplicate logic already present in `ManagedCode.MimeTypes`.
3. **Centralize on `MimeHelper` (chosen)**
   - Pros: one rule for the entire repo; consistent content-type handling.
   - Cons: requires discipline to avoid adding ad-hoc helpers.

## Consequences

### Positive

- Consistent MIME behaviour across providers and integration surfaces.
- Simpler docs and fewer surprises for consumers.

### Negative

- Providers must avoid “helpful” local MIME heuristics.
- Changes in MIME mapping should be done centrally and verified.

## References (Internal)

- `ManagedCode.Storage.Core/BaseStorage.cs`
- `docs/Features/mime-and-crc.md`
- `docs/Features/storage-core.md`
