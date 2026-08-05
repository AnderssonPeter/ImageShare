---
name: backend-tests
description: 'C# backend unit test conventions for ImageShare.Tests (TUnit with [MicrosoftDI] DI, InternalsVisibleTo not reflection, time-based tests, InMemoryFileProvider, AAA comments, parameterized tests). Use when writing or editing tests under ImageShare.Tests/'
---

# Backend unit test conventions

Backend tests live in `/app/ImageShare.Tests` and use **TUnit** (load the `csharp-tunit` skill for full TUnit syntax, assertions, and data-driven attributes).

## DI-first testing

- Use DI for unit tests: add the `[MicrosoftDI]` class attribute and inject the system-under-test via the primary constructor (see `MicrosoftDIAttribute` in the test project). Register any missing dependencies in `MicrosoftDIAttribute.BuildProvider`.
- Only construct types manually when the test deliberately needs a different configuration than the shared container (e.g. an attacker forging a token with a different key).

## Encapsulation

- Do not use reflection to access private or internal methods. Instead make the member `internal` and use the `InternalsVisibleTo` attribute to expose it to the test project.

## Test isolation & determinism

- Use time-based tests; `Task.Delay` is not a feasible solution.
- Use a virtual/in-memory file system in tests, not a physical directory: use `Mirality.FileProviders.InMemoryFileProvider` (see `FileProviderTestExtensions`).

## Structure & style

- Follow the **Arrange / Act / Assert** pattern with explicit `// Arrange`, `// Act`, `// Assert` or // Act & Assert comments.
- When possible and where it makes sense, use parameterized unit tests (TUnit `[Arguments]` / `[MethodData]` / `[ClassData]`).

## Supporting types in the test project

- `ImageShareWebApplicationFactory` — integration-test web app factory.
- `StaticAnalysis` — enforces rules such as the mediator binding-source attribute requirement.
- `HttpResultTestExtensions`, `TestImageFactory`, `TestUser`, `StatusCodeSelectorTests` — shared helpers/fixtures.
