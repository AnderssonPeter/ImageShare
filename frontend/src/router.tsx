/**
 * Router instance — wires the generated route tree with the QueryClient
 * context and browser scroll restoration.
 *
 * Only `queryClient` is provided here; the authenticated `user` is resolved
 * by the root route's `beforeLoad` and merged into context for descendants.
 * `defaultPreloadStaleTime: 0` ensures loaders always refetch fresh data
 * through React Query rather than serving stale route-level cache.
 */
import { MutationCache, QueryCache, QueryClient } from "@tanstack/react-query";
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
});

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}

export { queryClient, router };
