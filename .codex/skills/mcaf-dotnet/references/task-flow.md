# .NET Task Flow

Use this flow for implementation, refactoring, debugging, or review in a .NET repository.

## 1. Detect The Actual Stack

Start from checked-in repo state:

```bash
rg -n "TargetFramework|TargetFrameworks|LangVersion|UseMicrosoftTestingPlatformRunner|TestingPlatformDotnetTestSupport|EnableNETAnalyzers|AnalysisLevel|TreatWarningsAsErrors" -g '*.csproj' -g 'Directory.Build.*' .
rg -n "xunit|xunit\\.v3|TUnit|MSTest|StyleCopAnalyzers|Roslynator|Meziantou|coverlet|ReportGenerator|JetBrains\\.ReSharper\\.GlobalTools|NetArchTest|ArchUnitNET|CodeQL|CSharpier" -g '*.csproj' -g '.config/dotnet-tools.json' .
rg --files -g '.editorconfig' -g '*.sln.DotSettings'
```

Read `AGENTS.md` before deciding commands.

## 2. Choose Modern But Supported Features

- Prefer stable features supported by the repo's real `TFM` and `LangVersion`.
- Do not use `LangVersion=latest`.
- Use preview features only when the repo explicitly opted into preview and the SDK supports them.

## 3. Post-Change Quality Pass

After changing `.NET` production code, do not stop after one green test run.

Use the repo's exact commands from `AGENTS.md`. If the repo has no wrappers, the normal shape is:

1. `format`
2. `build`
3. `analyze`
4. focused `test`
5. broader `test`
6. `coverage` and report generation when configured
7. extra configured gates such as Roslynator, StyleCop, Meziantou, ReSharper CLT, architecture tests, CodeQL, CSharpier, or Stryker

## 4. Completion Rule

A `.NET` task is not complete when:

- formatting is still dirty
- analyzers still fail
- only unit tests ran even though broader repo commands are configured
- coverage, architecture, or security gates were configured but skipped without reason
