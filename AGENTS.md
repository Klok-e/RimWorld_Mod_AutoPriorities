# Repository Guidelines

## Project Structure & Module Organization

- `AutoPriorities/` is the C# mod project. Core logic: `AutoPriorities/Core/`. UI: `AutoPriorities/Ui/`. Harmony patches:
  `AutoPriorities/HarmonyPatches/`. Serialization: `AutoPriorities/PawnDataSerializer/`.
- `AutoPriorities/EmbeddedAssets/` holds default XML assets (presets, etc.).
- `Tests/` contains NUnit-based tests and fixtures.
- `AutoPriorities.sln` is the solution entry point.

## Build, Test, and Development Commands

- Prefer `make` targets (`make build`). Use `make cleanup` instead of manual
  `rm -rf`.
- Run tests via the IDE NUnit runner (Rider/VS).

Note: projects reference RimWorld assemblies via `../..//RimManaged/`. Ensure `RimManaged` exists and matches the target RimWorld version.

## Testing Guidelines

- Frameworks: NUnit, FluentAssertions, NSubstitute, AutoFixture.
- Test files live in `Tests/` and follow `*Tests.cs` naming (see `PawnsDataTests.cs`).
- Prefer focused unit tests; keep test data under `Tests/TestData/`.

## Configuration Tips

- When adding references or assets, update the `.csproj` and keep paths relative to the solution.
- Conditional compatibility patches are loaded from `AutoPriorities/ConditionalAssemblies/`.

## Agent-Specific Instructions

- Use JetBrains MCP commands (e.g., Rider MCP) when available for search, and inspections.
- Use do not use mcp for file changes.
- Inspections and files from rider MCP may be stale after editing without MCP commands.
- Rider MCP `projectPath` is the C# project root (`.../Source/AutoPriorities`), not the repo root (
  `.../RimWorld_Mod_AutoPriorities`). Use MCP only under the project root; use shell for repo-root files like `About/`.
- For `mcp__rider__get_file_text_by_path` `truncateMode` valid values are: `START`, `MIDDLE`, `END`, `NONE`.
- For code changes: only collect IDE-reported problems via MCP inspections after you edit a file.
- After changes: always build the project, then resolve warnings and errors in that order before finishing.
