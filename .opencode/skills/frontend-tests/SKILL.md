---
name: frontend-tests
description: 'Vitest testing conventions for the frontend (AAA pattern, expect.assertions, jsdom + testing-library, vi.hoisted/vi.mock, QueryClientProvider wrapper). Use when writing or editing *.test.ts(x) files under frontend/'
---

# Frontend unit test conventions

Frontend tests use **vitest** with **jsdom** and **@testing-library/react**. Config is in `vite.config.ts` (`test.environment: 'jsdom'`, `globals: true`, setup file `src/test/setup.ts` which registers `@testing-library/jest-dom` matchers). Test files match `src/**/*.{test,spec}.{ts,tsx}`.

## Commands

- `pnpm test` — run once (`vitest run`).
- `pnpm test:watch` — watch mode.
- `pnpm test:ui` — vitest UI.

## Structure & style

- Follow the **Arrange / Act / Assert** pattern with explicit `// Arrange`, `// Act`, `// Assert` comments. For trivial tests a combined `// Arrange + Act + Assert` comment is acceptable (see `urls.test.ts`).
- Call `expect.assertions(n)` at the top of each test to assert the exact number of expectations that run.
- Pass an explicit timeout as the third argument to `it(name, fn, timeout)` (e.g. `1000`).
- Group related tests with `describe`. Name tests by behaviour, not implementation.
- Use parameterized tests where it makes sense.

## Mocking

- Hoist mocks with `vi.hoisted` so they are available before imports, then apply them with `vi.mock(import("./module"), async (importOriginal) => { ... })`, spreading `...actual` and overriding only the members under test. This preserves real types for everything else.
- Type mock functions with `vi.fn<typeof originalFunction>()` to keep them type-safe.
- Reset mocks per test with `mockFn.mockReset()` (or `mockResolvedValueOnce` for one-shot async stubs).

## Hooks that need providers

When testing a hook that depends on React Query, wrap it with a `QueryClientProvider` using a `QueryClient` configured with `queries: { retry: false }`, and render via `renderHook(() => useHook(...), { wrapper })`. Use `waitFor` to await async results. See `src/lib/api/content-queries.test.tsx` for the canonical pattern.

## Assertions

- Use vitest globals (`expect`, `describe`, `it`, `vi` are available without import, but importing them explicitly is also fine and matches the codebase style).
- Prefer `@testing-library/jest-dom` matchers (`.toBeInTheDocument()`, etc.) for DOM assertions.
- For async, prefer `waitFor` over arbitrary sleeps.
