# Feature: Test Fakes (`ManagedCode.Storage.TestFakes`)

## Purpose

Provide lightweight storage doubles for tests and demos, allowing consumers to replace real provider registrations without provisioning cloud resources.

These fakes are intended for fast tests where provider-specific behaviour is not the subject under test.

## Main Flows

- Register a real provider (for production wiring).
- Replace it with a fake in tests using DI replacement helpers.

```mermaid
flowchart LR
  App[App/Test] --> DI[DI container]
  DI --> Real[Real provider registration]
  DI --> Fake[Replace*() -> Fake provider]
  Fake --> Tests[Fast tests without cloud accounts]
```

## Components

- `ManagedCode.Storage.TestFakes/FakeAzureStorage.cs`
- `ManagedCode.Storage.TestFakes/FakeAzureDataLakeStorage.cs`
- `ManagedCode.Storage.TestFakes/FakeAWSStorage.cs`
- `ManagedCode.Storage.TestFakes/FakeGoogleStorage.cs`
- `ManagedCode.Storage.TestFakes/MockCollectionExtensions.cs` (replacement helpers)

## Current Behavior

- Fakes are resolved through `Microsoft.Extensions.DependencyInjection` and implement the same provider interfaces as the real storages.
- Prefer full integration tests (Testcontainers / HTTP fakes) for verifying provider-specific behaviour; use fakes for “consumer wiring” tests.

## Tests

- `Tests/ManagedCode.Storage.Tests/ExtensionsTests/ReplaceExtensionsTests.cs`
