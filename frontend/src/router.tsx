/**
 * Router instance — wires the generated route tree with the QueryClient
 * context and browser scroll restoration.
 *
 * Only `queryClient` is provided here; the authenticated `user` is resolved
 * by the root route's `beforeLoad` and merged into context for descendants.
 * `defaultPreloadStaleTime: 0` ensures loaders always refetch fresh data
 * through React Query rather than serving stale route-level cache.
 *
 * `defaultErrorComponent` / `defaultNotFoundComponent` render the app's own
 * error and not-found pages for any route that doesn't override them. The
 * error component reuses `resolveErrorAction` so a 401 thrown from a route
 * loader/beforeLoad (which bypasses `QueryCache.onError`) still redirects to
 * the backend login endpoint instead of rendering the built-in error page.
 */
import { MutationCache, QueryCache, QueryClient } from "@tanstack/react-query";
import NotFound from "@components/Errors/NotFound";
import RouteError from "@components/Errors/RouteError";
import { type RouterContext } from "./routes/__root";
import { createRouter } from "@tanstack/react-router";
import { handleGlobalError } from "@lib/api/queryErrorHandler";
import { routeTree } from "./routeTree.gen";

/**
 * Singleton `QueryClient`. The query and mutation caches share a single
 * `onError` (`handleGlobalError`) so any failed request is routed centrally:
 * 401 → backend login, 404 → component empty state, 403/406 & unexpected → toast.
 */
const queryClient: QueryClient = new QueryClient({
  queryCache: new QueryCache({ onError: handleGlobalError }),
  mutationCache: new MutationCache({ onError: handleGlobalError }),
});

const router = createRouter({
  routeTree,
  context: { queryClient } satisfies RouterContext,
  defaultPreload: "intent",
  defaultPreloadStaleTime: 0,
  scrollRestoration: true,
  defaultErrorComponent: RouteError,
  defaultNotFoundComponent: NotFound,
});

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}

export { queryClient, router };
