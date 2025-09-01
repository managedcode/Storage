# Copilot Instructions for ManagedCode.Storage

## Overview

ManagedCode.Storage is a universal storage abstraction library that provides a consistent interface for working with multiple cloud blob storage providers including Azure Blob Storage, AWS S3, Google Cloud Storage, and local file system. The library aims to simplify development by providing a single API for all storage operations.

## Project Structure

- **ManagedCode.Storage.Core**: Core abstractions and interfaces (IStorage, BaseStorage, etc.)
- **Storages/**: Provider-specific implementations
  - `ManagedCode.Storage.Azure`: Azure Blob Storage implementation
  - `ManagedCode.Storage.Aws`: AWS S3 implementation  
  - `ManagedCode.Storage.Google`: Google Cloud Storage implementation
  - `ManagedCode.Storage.FileSystem`: Local file system implementation
  - `ManagedCode.Storage.Ftp`: FTP storage implementation
  - `ManagedCode.Storage.Azure.DataLake`: Azure Data Lake implementation
- **Tests/**: Unit and integration tests
- **Integrations/**: Additional integrations (SignalR, Client/Server components)

## Technical Context

- **Target Framework**: .NET 9.0
- **Language Version**: C# 13
- **Architecture**: Provider pattern with unified interfaces
- **Key Features**: Async/await support, streaming operations, metadata handling, progress reporting

## Development Guidelines

### Code Style & Standards
- Use nullable reference types (enabled in project)
- Follow async/await patterns consistently
- Use ValueTask for performance-critical operations where appropriate
- Implement proper cancellation token support in all async methods
- Use ConfigureAwait(false) for library code
- Follow dependency injection patterns

### Key Interfaces & Patterns
- `IStorage`: Main storage interface for blob operations
- `IStorageOptions`: Configuration options for storage providers
- `BaseStorage`: Base implementation with common functionality
- All operations should support progress reporting via `IProgress<T>`
- Use `BlobMetadata` for storing blob metadata
- Support for streaming operations with `IStreamer`

### Performance Considerations
- Implement efficient streaming for large files
- Use memory-efficient approaches for data transfer
- Cache metadata when appropriate
- Support parallel operations where beneficial
- Minimize allocations in hot paths

### Testing Approach
- Unit tests for core logic
- Integration tests for provider implementations
- Use test fakes/mocks for external dependencies
- Test error scenarios and edge cases
- Validate async operation behavior

### Provider Implementation Guidelines
When implementing new storage providers:
1. Inherit from `BaseStorage` class
2. Implement all required interface methods
3. Handle provider-specific errors appropriately
4. Support all metadata operations
5. Implement efficient streaming operations
6. Add comprehensive tests
7. Document provider-specific limitations or features

### Error Handling
- Use appropriate exception types for different error scenarios
- Provide meaningful error messages
- Handle provider-specific errors and translate to common exceptions
- Support retry mechanisms where appropriate

### Documentation
- Document public APIs with XML comments
- Include usage examples for complex operations
- Document provider-specific behavior differences
- Keep README.md updated with supported features

## Common Tasks

### Adding a New Storage Provider
1. Create new project in `Storages/` folder
2. Inherit from `BaseStorage`
3. Implement provider-specific operations
4. Add configuration options
5. Create comprehensive tests
6. Update solution file and documentation

### Implementing New Features
1. Define interface changes in Core project
2. Update BaseStorage if needed
3. Implement in all relevant providers
4. Add tests for new functionality
5. Update documentation

### Performance Optimization
- Profile critical paths
- Optimize memory allocations
- Improve streaming performance
- Cache frequently accessed data
- Use efficient data structures

## Dependencies & Libraries
- Provider-specific SDKs (Azure.Storage.Blobs, AWS SDK, Google Cloud Storage)
- Microsoft.Extensions.* for dependency injection and configuration
- System.Text.Json for serialization
- Benchmarking tools for performance testing

## Building & Testing
- Use `dotnet build` to build the solution
- Run `dotnet test` for unit tests
- Integration tests may require cloud provider credentials
- Use `dotnet pack` to create NuGet packages