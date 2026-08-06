/**
 * Root route — the application layout shell.
 *
 * Uses `createRootRouteWithContext` so every descendant route has type-safe
 * access to the shared `queryClient` (provided at router creation).
 *
 * The authenticated `user` is prefetched by `beforeLoad` (so it is in the
 * cache before React renders) but is **not** stored on the router context.
 * Instead it is exposed via `UserProvider` / `useUser()` (a React context),
 * which reads the cached query. Route guards that need the user before React
 * renders read it directly from the query cache.
 */
import { UserProvider, currentUserQueryOptions } from "@lib/userContext";
import { ThemeProvider } from "@lib/themeContext";
import AppLayout from "@components/AppLayout";
import { type QueryClient } from "@tanstack/react-query";
import Sonner from "@components/ui/Sonner";
import { createRootRouteWithContext } from "@tanstack/react-router";

/**
 * Initial router context — supplied at `createRouter` time (see `router.tsx`).
 * Only contains the `queryClient`; the `user` is resolved via React Query +
 * `UserProvider`, not the router context.
 */
export interface RouterContext {
  queryClient: QueryClient;
}

function RootComponent(): React.JSX.Element {
  return (
    <ThemeProvider>
      <UserProvider>
        <AppLayout />
        <Sonner />
      </UserProvider>
    </ThemeProvider>
  );
}

export const Route = createRootRouteWithContext<RouterContext>()({
  beforeLoad: async ({ context }) => {
    await context.queryClient.ensureQueryData(currentUserQueryOptions());
  },
  component: RootComponent,
});
