import { MutationCache, QueryCache, QueryClient } from "@tanstack/react-query";
import NotFound from "@components/Errors/NotFound";
import RouteError from "@components/Errors/RouteError";
import { type RouterContext } from "./routes/__root";
import { createRouter } from "@tanstack/react-router";
import { handleGlobalError } from "@lib/api/queryErrorHandler";
import { routeTree } from "./routeTree.gen";

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
