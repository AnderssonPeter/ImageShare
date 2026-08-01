<!-- context7 -->
Use Context7 MCP to fetch current documentation whenever the user asks about a library, framework, SDK, API, CLI tool, or cloud service -- even well-known ones like React, Next.js, Prisma, Express, Tailwind, Django, or Spring Boot. This includes API syntax, configuration, version migration, library-specific debugging, setup instructions, and CLI tool usage. Use even when you think you know the answer -- your training data may not reflect recent changes. Prefer this over web search for library docs.

Do not use for: refactoring, writing scripts from scratch, debugging business logic, code review, or general programming concepts.

## Steps

1. Always start with `resolve-library-id` using the library name and the user's question, unless the user provides an exact library ID in `/org/project` format
2. Pick the best match (ID format: `/org/project`) by: exact name match, description relevance, code snippet count, source reputation (High/Medium preferred), and benchmark score (higher is better). If results don't look right, try alternate names or queries (e.g., "next.js" not "nextjs", or rephrase the question). Use version-specific IDs when the user mentions a version
3. `query-docs` with the selected library ID and the user's full question (not single words)
4. Answer using the fetched docs
<!-- context7 -->

<!-- microsoft-learn -->
Use Microsoft learn to fetch current documentation about microsoft packages, sdks, products, services, and tools. This includes API syntax, configuration, version migration, library-specific debugging, setup instructions, and CLI tool usage.
Use even when you think you know the answer -- your training data may not reflect recent changes. Prefer this over web search for Microsoft docs.
<!-- microsoft-learn -->

## General
When there are multiple solutions to a problem ask the user what solution to pick.
When you detect that I have made changes that you don't recognition don't just undo them, instead create a new plan how to resolve your task while keeping them in place. 

## Verification

Before considering a task complete, run the CI steps individually (not `dotnet r ci`) so that slow or failing steps are easy to identify. Run them in order, stopping on the first failure:

1. `dotnet r format`
2. `dotnet r build`
3. `dotnet r test`
4. `dotnet r startup`

If any of the steps above fail, run `dotnet build-server shutdown` and then re-run the failed step. (If you have any idea how to fix it permanently, please do so and then re-run the failed step.)

Run `dotnet tool restore` once at the start of a new session before using `dotnet r`.

If `dotnet restore` fails with a vulnerability error (NU1901/NU1902/NU1903) treated as an error, upgrade the offending package to a non-vulnerable version rather than suppressing the warning. Use `nuget_fix_vulnerable_packages` to compute the smallest safe version change, then apply it.

## Naming

Do not use abbreviations when naming variables, parameters, fields, or methods. Use full, descriptive names (e.g. `cancellationToken` not `ct`, `directory` not `dir`, `extension` not `ext`).

Private fields must be `camelCase` without an underscore prefix (e.g. `cache`, not `_cache` or `Cache`). The `.editorconfig` IDE1006 naming rule enforces this in the IDE; prefer auto-properties (PascalCase) or primary-constructor parameters over introducing a private field where the value is just a stored dependency or computed option.

## Domain modeling (no helper / service classes)

Do not create `*Helper`, `*Service`, `*Util`, or `*Manager` classes as dumping grounds for free functions. They break encapsulation and hide behavior that belongs on a domain concept. Prefer one of these alternatives, in order:

1. **Find a better class name** Find a class name that has one responsibility no more
2. **Extension methods.** If the behavior is a pure transformation that operates on an existing type you do not own (framework types like `StringValues`, `IFileInfo`, `string[]`), expose it as an extension method on that type. Group related extensions in a single static `*Extensions` class — this is not a "helper" class, it is the C# mechanism for adding methods to foreign types.
3. **Adapters implementing an external interface.** A class that implements a third-party framework interface (e.g. `IApiKeyProvider`, `IOpenIdConnectOptionsConfigure`) is legitimate and not covered by this rule — it only exists to satisfy the framework's contract.
4. **Split the class into multiple classes** If none of the above is possible, split the class into multiple classes each with a more narrow usecase.
 
When you encounter an existing `*Helper`, `*Service`, `*Util`, or `*Manager` class that violates this rule, do not add new code to it. Propose a refactor to the user: according to the rules above.

## Options objects

All options must be validated on startup
All fields should have some form of validation applied to them, either through data annotations or custom validation logic.
This ensures that the application fails fast and provides clear feedback if configuration is incorrect.

## Unit tests
When writing unit tests:
* Use reflection to access private or internal methods, instead make them internal and use InternalsVisibleTo attribute to access them in tests
* Use time based tests, `Task.Delay` is not a feasible solution
* Use a physical directory in tests, instead use `Mirality.FileProviders.InMemoryFileProvider`
* the unit tests are written with TUnit, for more information use the `csharp-tunit` skill

## IO
When writing code that interacts with the file system:
* Don't interact with the file system directly , instead use the file provider:
  * When writing tests use `Mirality.FileProviders.InMemoryFileProvider`
  * For production code use `Mirality.FileProviders.WritablePhysicalFileProvider`
  * The code interacting with it should use
	* When only reading `IFileProvider`
	* When reading and writing `IWritableFileProvider`

`ISyncWritableFileProvider and `IWritableFileProvider` both inherit `IFileProvider` so there is no need to use both in the same class, if you need to write and read then use `IWritableFileProvider` but if you only need to read then use `IFileProvider`.

## File placement
Files should be placed by feature instead of by type.

## Unit tests
C# unit tests use TUnit, skill exists for this.
All unit tests should have Arrange, Act, Assert comments
When possible and where it makes sense use parameterized unit tests
Use DI for unit tests: add the `[MicrosoftDI]` class attribute and inject the system-under-test via the primary constructor (see `MicrosoftDIAttribute`). Register any missing dependencies in `MicrosoftDIAttribute.BuildProvider`. Only construct types manually when the test deliberately needs a different configuration than the shared container (e.g. an attacker forging a token with a different key).

## Mediator queries and commands
All query and command objects (types implementing `IBaseQuery` or `IBaseCommand` from `Mediator.Abstractions`) must have an explicit binding source attribute on every constructor parameter: `[FromQuery]`, `[FromRoute]`, `[FromBody]`, `[FromHeader]`, or `[FromServices]` from `Microsoft.AspNetCore.Mvc`. This is enforced by `StaticAnalysis`.
