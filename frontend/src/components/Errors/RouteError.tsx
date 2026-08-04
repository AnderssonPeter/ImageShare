/**
 * RouteError — the router's default error boundary UI.
 *
 * Wired as `defaultErrorComponent` on `createRouter` (see `router.tsx`), this
 * renders whenever a route's `beforeLoad`/loader/component throws and no
 * nearer route supplies its own `errorComponent`. It reuses
 * `resolveErrorAction` — the same pure function the global `QueryClient`
 * `onError` uses — so the status-based behaviour is a single source of truth:
 *
 *  - 401 → full-page redirect to the backend login endpoint. This closes the
 *    gap where an `ensureQueryData` rejection inside a route `beforeLoad`/
 *    loader bypasses `QueryCache.onError` (which only fires for observed
 *    queries/mutations) and would otherwise render the built-in error page.
 *  - 404 → a "couldn't find that" message (a 404 thrown from a loader, as
 *    opposed to an unmatched URL which renders `NotFound`).
 *  - 403/406/5xx/unknown → the RFC 7807 detail, title, or error message.
 *
 * The 401 redirect runs in an effect (not during render) so the component
 * stays pure. The "Retry" button resets the boundary and invalidates the
 * router so a failed loader re-runs.
 */
import { type ErrorComponentProps, Link, useRouter } from "@tanstack/react-router";
import { RotateCw, TriangleAlert } from "lucide-react";
import { useCallback, useEffect } from "react";
import Button from "@components/ui/Button";
import { resolveErrorAction } from "@lib/api/queryErrorHandler";

const HOME_LINK_CLASS = Button.buttonVariants({ variant: "ghost", size: "sm" });
const ROOT_SPLAT_PARAMS = { _splat: undefined };

/** Full in-app path (pathname + search + hash) to return to after re-login. */
function currentPath(): string {
  const { location } = globalThis;
  return location.pathname + location.search + location.hash;
}

function ErrorActions({ onRetry }: { onRetry: () => void }): React.JSX.Element {
  return (
    <div className="flex gap-2">
      <Button variant="default" size="sm" onClick={onRetry}>
        <RotateCw />
        Retry
      </Button>
      <Link to="/browse/$" params={ROOT_SPLAT_PARAMS} className={HOME_LINK_CLASS}>
        Go to library
      </Link>
    </div>
  );
}

export default function RouteError({ error, reset }: ErrorComponentProps): React.JSX.Element {
  const action = resolveErrorAction(error, currentPath());
  const router = useRouter();
  const redirectUrl = action.kind === "redirect" ? action.url : undefined;
  const handleRetry = useCallback(() => {
    reset();
    router.invalidate();
  }, [reset, router]);

  useEffect(() => {
    if (redirectUrl !== undefined) {
      globalThis.location.replace(redirectUrl);
    }
  }, [redirectUrl]);

  if (action.kind === "redirect") {
    return (
      <div className="flex h-full items-center justify-center p-8">
        <p className="text-sm text-muted-foreground">Redirecting to sign in…</p>
      </div>
    );
  }

  return (
    <div className="flex h-full flex-col items-center justify-center gap-3 p-8 text-center">
      <TriangleAlert className="size-5 text-muted-foreground" />
      <p className="text-sm text-foreground">
        {action.kind === "ignore" ? "We couldn't find what you were looking for." : action.message}
      </p>
      <ErrorActions onRetry={handleRetry} />
    </div>
  );
}
