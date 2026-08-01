/**
 * Root route — the application layout shell.
 *
 * Uses `createRootRouteWithContext` so every descendant route has type-safe
 * access to the shared `queryClient` (provided at router creation). The
 * authenticated `user` is resolved by `beforeLoad` and merged into context
 * for all descendants — it is not part of the initial `RouterContext`.
 *
 * The backend gates all access — unauthenticated users never reach the SPA.
 * `beforeLoad` fetches the current user via `GET /api/authentication/user`
 * purely to read `name`/`isAdmin` for the app bar and route guards; no
 * client-side auth redirect is needed. Session-expiry mid-session is handled
 * by the global `QueryClient` `onError` handler (Phase 8).
 */
import { Outlet, createRootRouteWithContext } from '@tanstack/react-router'
import { type IUser } from '@/lib/api/generated/imageShare.schemas'
import MetroAppBar from '@/components/MetroAppBar'
import { type QueryClient } from '@tanstack/react-query'
import { ThemeProvider } from '@/lib/themeContext'
import { getApiAuthenticationUser } from '@/lib/api/generated/authentication/authentication'

/**
 * Initial router context — supplied at `createRouter` time (see `router.tsx`).
 * Only contains the `queryClient`; the `user` is resolved by the root route's
 * `beforeLoad` and merged into context for all descendant routes.
 */
export interface RouterContext {
  queryClient: QueryClient
}

function RootComponent(): React.JSX.Element {
  const { user } = Route.useMatch().context
  return (
    <ThemeProvider>
      <MetroAppBar user={user}>
        <Outlet />
      </MetroAppBar>
    </ThemeProvider>
  )
}

export const Route = createRootRouteWithContext<RouterContext>()({
  beforeLoad: async () => {
    const response = await getApiAuthenticationUser()
    if (response.status !== 200) {
      throw new Error(`Unexpected response status: ${response.status}`)
    }
    return { user: response.data satisfies IUser }
  },
  component: RootComponent,
})
