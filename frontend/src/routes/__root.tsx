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
import { Outlet, createRootRouteWithContext, useMatches } from "@tanstack/react-router";
import { type QueryClient, queryOptions } from "@tanstack/react-query";
import { type User, getCurrentUser } from "@lib/api/generated";
import Breadcrumb from "@components/Breadcrumb";
import DownloadButton from "@components/DownloadButton";
import MetroAppBar from "@components/MetroAppBar";
import Sonner from "@components/ui/Sonner";
import { ThemeProvider } from "@lib/themeContext";
import { useMemo } from "react";

/**
 * Initial router context — supplied at `createRouter` time (see `router.tsx`).
 * Only contains the `queryClient`; the `user` is resolved by the root route's
 * `beforeLoad` and merged into context for all descendant routes.
 */
export interface RouterContext {
  queryClient: QueryClient;
}

/**
 * Read the browse route's splat from the match chain so the root layout can
 * render the app-bar breadcrumb. Returns `onBrowse: false` on non-browse
 * routes (e.g. admin) so the breadcrumb slot stays empty there.
 */
function useBrowseSplat(): { onBrowse: boolean; splat: string | undefined } {
  const matches = useMatches();
  for (const match of matches) {
    if (match.routeId === "/browse/$") {
      return { onBrowse: true, splat: (match.params as { _splat?: string })._splat };
    }
  }
  return { onBrowse: false, splat: undefined };
}

function RootComponent(): React.JSX.Element {
  const { user } = Route.useMatch().context;
  const { onBrowse, splat } = useBrowseSplat();
  const breadcrumb = useMemo(
    () =>
      onBrowse ? (
        <div className="flex min-w-0 flex-1 items-center gap-1">
          <Breadcrumb path={splat} />
          <DownloadButton path={splat} />
        </div>
      ) : undefined,
    [onBrowse, splat],
  );
  return (
    <ThemeProvider>
      <MetroAppBar user={user} breadcrumb={breadcrumb}>
        <Outlet />
      </MetroAppBar>
      <Sonner />
    </ThemeProvider>
  );
}

/** Query options for the current user (prefetched in the root route loader). */
function currentUserQueryOptions() {
  return queryOptions({
    queryKey: ["current-user"] as const,
    queryFn: async ({ signal }): Promise<User> => {
      const { data } = await getCurrentUser({ signal });
      return data as unknown as User;
    },
  });
}

export const Route = createRootRouteWithContext<RouterContext>()({
  beforeLoad: async ({ context }) => {
    const user = await context.queryClient.ensureQueryData(currentUserQueryOptions());
    return { user: user satisfies User };
  },
  component: RootComponent,
});
