/**
 * Router instance — wires the generated route tree with the QueryClient
 * context and browser scroll restoration.
 *
 * Only `queryClient` is provided here; the authenticated `user` is resolved
 * by the root route's `beforeLoad` and merged into context for descendants.
 * `defaultPreloadStaleTime: 0` ensures loaders always refetch fresh data
 * through React Query rather than serving stale route-level cache.
 */
import { QueryClient } from '@tanstack/react-query'
import { type RouterContext } from './routes/__root'
import { createRouter } from '@tanstack/react-router'
import { routeTree } from './routeTree.gen'

const queryClient: QueryClient = new QueryClient()

const router = createRouter({
  routeTree,
  context: { queryClient } satisfies RouterContext,
  defaultPreload: 'intent',
  defaultPreloadStaleTime: 0,
  scrollRestoration: true,
})

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}

export { queryClient, router }
