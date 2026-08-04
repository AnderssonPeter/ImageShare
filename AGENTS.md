## General
When there are multiple solutions to a problem ask the user what solution to pick.
When you detect that I have made changes that you don't recognition don't just undo them, instead create a new plan how to resolve your task while keeping them in place.
Before trying to solve a issue where there are common packages to solve it, ask the user if they want to use a package, give them a list with some pros and cons.
Only add comments in code if they are needed, so if there is something thats not logical or its a bug in a framework, the goal should be that the code should be self documenting.

## Domain modeling (no helper / service / util / manager modules)

Do not create `*Helper`, `*Service`, `*Util`, or `*Manager` classes/modules as dumping grounds for free functions. They break encapsulation and hide behavior that belongs on a domain concept. Prefer one of these alternatives, in order:

1. **Find a better name** — a class/module with one responsibility and no more.
2. **Extend the type you don't own.** In C#, extension methods on a framework type (`StringValues`, `IFileInfo`, `string[]`) grouped in a single static `*Extensions` class. In TS, a free function placed next to the type or an augmenting module — not a "utils" grab-bag.
3. **Adapters implementing an external interface** (e.g. `IApiKeyProvider`, `IOpenIdConnectOptionsConfigure`, a React provider/hook contract) — legitimate because they exist to satisfy a framework's contract.
4. **Split** the class/module into multiple ones each with a narrower use case.

When you encounter an existing `*Helper`, `*Service`, `*Util`, or `*Manager` that violates this rule, do not add new code to it. Propose a refactor to the user per the rules above.

## File placement

Files should be placed by feature, not by type (e.g. backend `Authentication/`, `Browsing/`, `ImageConversion/`, `Health/`, `Errors/`, `Validation/`; frontend `routes/`, `components/ui/`, `lib/api/`).

## Skills
Project-specific conventions live as on-demand skills under `.opencode/skills/` (loaded only when relevant, to keep context lean):

- **frontend** — frontend dev conventions (React 19, TanStack Router/Query, shadcn on base-ui, orval API gen, oxlint; plus `pnpm lint/test/build` verification). Load when editing `frontend/`.
- **backend** — backend C# conventions (naming, options validation, file-provider IO, mediator binding sources; plus `dotnet r format/build/test/startup` verification, build-server shutdown, NuGet vulnerability fixes). Load when editing `ImageShare/`.
- **frontend-tests** — vitest test conventions (AAA, `expect.assertions`, jsdom + testing-library, mocking, QueryClient wrapper). Load when editing `frontend/**/*.test.*`.
- **backend-tests** — C# test conventions (TUnit with `[MicrosoftDI]`, InternalsVisibleTo over reflection, time-based tests, InMemoryFileProvider, AAA). Load when editing `ImageShare.Tests/`.

The `csharp-tunit` global skill covers TUnit syntax, assertions, and data-driven attributes; load it alongside `backend-tests` when writing TUnit tests.

## Library & Microsoft docs
Library/framework/SDK documentation is fetched via the Context7 and Microsoft Learn MCP servers (see their instructions). No project-specific rules beyond their defaults.

## Workflow
When working on a frontend task read `frontend\TODO.md`
1. Find the next pending item.
2. Work on **exactly one** todo item at a time:
   - Find the first unchecked item (`[ ]`).
   - Mark it in progress by changing `[ ]` → `[/]` before starting.
   - Implement only that single item — do not start or batch other items in the same phase.
3. When the item is complete:
   - Verify the change
   - **Always run `pnpm lint:fix`, `pnpm test`, `pnpm format` and `pnpm build` and fix every resulting error** before marking the item done. Warnings are not acceptable, all unit tests and linting errors must be fixed even if they have nothing to do with what was changed. (If you know of a permanent fix to the issue, suggest it to me.)
   - **Never disable lint rules.** Always fix warnings/errors by changing the code you own. Only if you have exhausted every possible code-level fix may you ask the user for permission to disable a rule — never do it without asking first.
   - Mark it done by changing `[/]` → `[x]`.
4. Ask the user to review the change, and allow them to tell you what can be done better.
5. Wait for the user's feedback before moving to the next item. Incorporate any requested fixes before proceeding.
6. Repeat from step 1.
