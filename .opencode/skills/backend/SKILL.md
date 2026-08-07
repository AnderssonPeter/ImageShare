---
name: backend
description: 'Backend C# conventions for the ImageShare ASP.NET Core API (naming, domain modeling, options validation, file-provider IO, mediator binding sources, feature-based file placement). Use when writing or editing code under ImageShare/'
---

# Backend conventions

The backend lives in `/app/ImageShare`. Stack: ASP.NET Core minimal APIs, Mediator (Mediator.Abstractions), file-provider-based IO, options pattern. Tests in `/app/ImageShare.Tests`.

## Naming

- Do not use abbreviations when naming variables, parameters, fields, or methods. Use full, descriptive names (e.g. `cancellationToken` not `ct`, `directory` not `dir`, `extension` not `ext`).
- Private fields must be `camelCase` without an underscore prefix (e.g. `cache`, not `_cache` or `Cache`). The `.editorconfig` IDE1006 rule enforces this. Prefer auto-properties (PascalCase) or primary-constructor parameters over introducing a private field where the value is just a stored dependency or computed option.

## Options objects

- All options must be validated on startup.
- All fields should have some form of validation applied, either through data annotations or custom validation logic. This ensures the application fails fast and provides clear feedback if configuration is incorrect.

## IO (file providers)

When writing code that interacts with the file system, do not interact with the file system directly — use the file provider:

- When writing tests use `Mirality.FileProviders.InMemoryFileProvider`.
- For production code use `Mirality.FileProviders.WritablePhysicalFileProvider`.

The code interacting with it should use:
- `IFileProvider` when only reading.
- `IWritableFileProvider` when reading and writing.

`ISyncWritableFileProvider` and `IWritableFileProvider` both inherit `IFileProvider`, so there is no need to use both in the same class — use `IWritableFileProvider` if you need to read and write, `IFileProvider` if you only need to read.

## Mediator queries and commands

All query and command objects (types implementing `IBaseQuery` or `IBaseCommand` from `Mediator.Abstractions`) must have an explicit binding source attribute on every constructor parameter: `[FromQuery]`, `[FromRoute]`, `[FromBody]`, `[FromHeader]`, or `[FromServices]` from `Microsoft.AspNetCore.Mvc`. This is enforced by `StaticAnalysis`.

## Verification

Before considering a backend task complete, run the CI steps individually (not `dotnet r ci`) so that slow or failing steps are easy to identify. Run them in order, stopping on the first failure.

Run `dotnet tool restore` once at the start of a new session before using `dotnet r`.

1. `dotnet r format`
2. `dotnet r build`
3. `dotnet r test`
4. `dotnet r startup`

### `dotnet r format` is not clean unless it prints nothing

`dotnet format` returns exit code `0` even when it emits diagnostics it cannot auto-fix, so a clean exit code does **not** mean formatting is clean. A successful format step is one that prints **nothing** (no warnings, no "Unable to fix..." messages). Treat any output on stdout/stderr as a real violation and fix it before proceeding — do not dismiss messages like:

To locate the offending file/symbol, re-run format with detailed verbosity and narrow it down:

```
dotnet format ImageShare.slnx --verbosity detailed --no-restore
```

If any of the steps above fail, run `dotnet build-server shutdown` and then re-run the failed step. (If you have an idea how to fix it permanently, do so and then re-run the failed step.)

If `dotnet restore` fails with a vulnerability error (NU1901/NU1902/NU1903) treated as an error, upgrade the offending package to a non-vulnerable version rather than suppressing the warning. Use `nuget_fix_vulnerable_packages` to compute the smallest safe version change, then apply it.
