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

## Verification

Before considering a task complete, run `dotnet r ci` to verify all changes pass CI checks.

## Naming

Do not use abbreviations when naming variables, parameters, fields, or methods. Use full, descriptive names (e.g. `cancellationToken` not `ct`, `directory` not `dir`, `extension` not `ext`).

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
