# .NET Skill Routing

Use `mcaf-dotnet` as the entry skill when a task spans more than one .NET concern.

## Open These Skills When

| Need | Skill |
| --- | --- |
| overall .NET implementation, debugging, review, or verification flow | `mcaf-dotnet` |
| xUnit test mechanics | `mcaf-dotnet-xunit` |
| TUnit test mechanics | `mcaf-dotnet-tunit` |
| MSTest test mechanics | `mcaf-dotnet-mstest` |
| modern C# feature choice, version upgrades, or language compatibility | `mcaf-dotnet-features` |
| .NET quality gates, analyzer stack, coverage, mutation, or security gate selection | `mcaf-dotnet-quality-ci` |
| repo-root `.editorconfig` authoring and analyzer severity ownership | `mcaf-dotnet-analyzer-config` |
| complex methods, maintainability metrics, and coupling thresholds | `mcaf-dotnet-complexity` |
| one concrete tool such as Roslynator, StyleCop, Coverlet, ReportGenerator, ReSharper CLT, or CSharpier | the exact tool skill |
| SOLID-driven refactors and maintainability-limit enforcement | `mcaf-solid-maintainability` |
| architecture map or boundary documentation | `mcaf-architecture-overview` |
| architecture rules in executable tests | `mcaf-dotnet-netarchtest` or `mcaf-dotnet-archunitnet` |
| CI workflow or release gate design | `mcaf-ci-cd` |

## Routing Rules

- Open the smallest set of skills that covers the task.
- Do not open more than one test-framework skill for the same project.
- Do not run every .NET tool by default. Run the tools the repo actually configured.
- If the repo already standardized on one formatter or analyzer set, use the matching skill rather than creating a second path.
- After code changes, still run the repo-defined quality pass even when the task was mainly about tests or refactoring.
