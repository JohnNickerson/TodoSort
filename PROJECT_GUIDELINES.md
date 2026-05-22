Project Guidelines and Architecture
=================================

Purpose
-------

This document captures ground rules, coding guidelines, and a concise architecture overview for the TodoSort solution. Place this file at the repository root so it becomes part of the project context and is visible to contributors and automation.

Ground Rules
------------

- Communication: open issues for bugs and feature requests; prefer small focused PRs.
- Reviews: require at least one reviewer for non-trivial changes; include screenshots or failing test details for UI/behavioral changes.
- Tests: add unit tests for bug fixes and new behavior; keep tests fast and deterministic.
- CI: commits to main/master must pass tests and build.
- Secrets: never commit secrets or credentials. Use environment variables or secure stores.

Coding Guidelines
-----------------

- Language: C# targeting the existing solution and frameworks used in the repo.
- Formatting: follow existing project style; keep `using` directives ordered and minimal.
- Naming: use descriptive PascalCase for types and methods, camelCase for local variables.
- Nullability and safety: prefer explicit null checks; where possible adopt nullable annotations consistent with the project.
- Small changes: aim for single-concern commits and include unit tests where applicable.
- Documentation: add XML comments for public API surface and update README when behavior changes.

Branching & Commits
-------------------

- Branches: use feature branches named `feature/<short-desc>` or `fix/<short-desc>`.
- Pull Requests: include a short description, testing notes, and reviewers.
- Commit messages: short imperative title + optional body. Example: `Fix: preserve tags when moving items`

Build & Run
-----------

- Build solution:

  dotnet build

- Run the WPF GUI (from solution root):

  dotnet run --project WpfGui/WpfGui.csproj

- Run tests:

  dotnet test UnitTests/UnitTests.csproj

Project Layout and Architecture Overview
---------------------------------------

High-level responsibilities for main folders:

- `CLI/` — Command-line entrypoints and option parsing; runs operations implemented in `Core`.
- `Core/` — Business logic: models, services, comparers, and core helpers. Primary location for most domain logic.
    - `Data/` — Persistence layer and repository implementations such as `TodoRepository` and mappers.
    - `Export/` and `Import/` — Format-specific import/export implementations.
- `WpfGui/` — Desktop UI, views, and view models that consume `Core` services.
- `CoreGui/` — Shared GUI helpers and viewmodels used by the desktop UI.
- `UnitTests/` — Automated tests covering core behavior and regressions.

Data & Flow
-----------

- Typical flow: CLI or WpfGui invokes `Core` services which in turn use `Data` repositories for persistence. Import/Export classes transform external formats into domain `Core` models.
- Keep UI code thin: move business rules into `Core` so both CLI and GUI share behavior.

Guidance for Contributors
-------------------------

- Prefer adding tests with bugfixes. If a change touches multiple areas, split into separate PRs when possible.
- When modifying storage or data formats, include migration steps or clear notes in the PR.
- If you change public APIs in `Core`, update XML docs and add/adjust unit tests.

Where to look first
-------------------

- For domain logic: `Core/` and its tests in `UnitTests/`.
- For CLI behavior: `CLI/Program.cs` and `Options/`.
- For UI changes: `WpfGui/Views/` and `CoreGui/ViewModels/`.

Next Steps
----------

- Keep this file updated as architecture decisions evolve.
