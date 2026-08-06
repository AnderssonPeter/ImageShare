/**
 * UserProvider — single source of truth for the authenticated user.
 *
 * Wraps the app once (mounted in the root route layout) and exposes the
 * current `User` via `useUser()`. The user is fetched via React Query
 * (`currentUserQueryOptions`) and prefetched by the root route's `beforeLoad`
 * so it is already in the cache when the provider mounts.
 *
 * Route guards that need the user before React renders (e.g. the admin gate)
 * read it directly from the query cache via `queryClient.getQueryData`, not
 * from this context — React context is only available inside the React tree.
 */
import { type ReactNode, createContext, useContext } from "react";
import { type UseQueryResult, useQuery } from "@tanstack/react-query";
import { type User, getCurrentUser } from "@lib/api/generated";
import { ensureUser } from "@lib/api/guards";

/** Query key for the current-user query (shared with route-level prefetch). */
export const CURRENT_USER_QUERY_KEY = ["current-user"] as const;

/** Query options for the current user (prefetched in the root route loader). */
export function currentUserQueryOptions() {
  return {
    queryKey: CURRENT_USER_QUERY_KEY,
    queryFn: async ({ signal }: { signal: AbortSignal }): Promise<User> => {
      const { data } = await getCurrentUser({ signal });
      return ensureUser(data);
    },
  };
}

export type UserContextValue = UseQueryResult<User>;

const UserContext = createContext<UserContextValue | undefined>(undefined);

export function UserProvider({ children }: { children: ReactNode }): React.JSX.Element {
  const userQuery = useQuery(currentUserQueryOptions());
  return <UserContext.Provider value={userQuery}>{children}</UserContext.Provider>;
}

/**
 * Read the current user. Must be used within a `UserProvider`.
 * Throws if the provider is missing so misuse fails loudly.
 */
export function useUser(): UserContextValue {
  const value = useContext(UserContext);
  if (value === undefined) {
    throw new Error("useUser must be used within a UserProvider");
  }
  return value;
}
