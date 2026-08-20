/**
 * RouteError — the router's default error boundary UI.
 *
 * Wired as `defaultErrorComponent` on `createRouter` (see `router.tsx`), this
 * renders whenever a route's `beforeLoad`/loader/component throws and no
 * nearer route supplies its own `errorComponent`. It reuses
 * `resolveErrorAction` — the same pure function the global `QueryClient`
 * `onError` uses — so the status-based behaviour is a single source of truth:
 *
 *  - 401 → branched by the last known `AuthenticationSource` (read from the
 *    `authSource` cookie): OpenIdConnect shows "session expired" + a "Sign in"
 *    button, Jwt shows "you no longer have access", ApiKey is ignored, and an
 *    unknown source falls back to a full-page redirect to the backend login
 *    endpoint (closes the gap where an `ensureQueryData` rejection inside a
 *    route `beforeLoad`/loader bypasses `QueryCache.onError`, which only fires
 *    for observed queries/mutations).
 *  - 404 → a "couldn't find that" message (a 404 thrown from a loader, as
 *    opposed to an unmatched URL which renders `NotFound`).
 *  - 403/406/5xx/unknown → the RFC 7807 detail, title, or error message.
 *
 * The 401 redirect (unknown-source fallback) runs in an effect (not during
 * render) so the component stays pure. The "Retry" button resets the boundary
 * and invalidates the router so a failed loader re-runs.
 */
import { type ErrorComponentProps, Link, useRouter } from "@tanstack/react-router";
import { RotateCw, TriangleAlert } from "lucide-react";
import { useCallback, useEffect } from "react";
import Button from "@components/ui/Button";
import { ApiError } from "@lib/api/errors";
import { resolveErrorAction } from "@lib/api/queryErrorHandler";
import { useTranslation } from "@lib/i18n";

const HOME_LINK_CLASS = Button.buttonVariants({ variant: "ghost", size: "sm" });
const ROOT_SPLAT_PARAMS = { _splat: undefined };

/** Full in-app path (pathname + search + hash) to return to after re-login. */
function currentPath(): string {
  const { location } = globalThis;
  return location.pathname + location.search + location.hash;
}

function ErrorActions({ onRetry }: { onRetry: () => void }): React.JSX.Element {
  const { t: translate } = useTranslation();
  return (
    <div className="flex gap-2">
      <Button variant="default" size="sm" onClick={onRetry}>
        <RotateCw />
        {translate("errors.retry")}
      </Button>
      <Link to="/browse/$" params={ROOT_SPLAT_PARAMS} className={HOME_LINK_CLASS}>
        {translate("errors.goLibrary")}
      </Link>
    </div>
  );
}

export default function RouteError({ error, reset }: ErrorComponentProps): React.JSX.Element {
  const { t: translate } = useTranslation();
  const action = resolveErrorAction(error, { currentPath: currentPath(), translate });
  const router = useRouter();
  const redirectUrl = action.kind === "redirect" ? action.url : undefined;
  const isAuthExpiry = action.kind === "toast" && error instanceof ApiError && error.status === 401;
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
        <p className="text-sm text-muted-foreground">{translate("errors.redirecting")}</p>
      </div>
    );
  }

  return (
    <div className="flex h-full flex-col items-center justify-center gap-3 p-8 text-center">
      <TriangleAlert className="size-5 text-muted-foreground" />
      <p className="text-sm text-foreground">
        {action.kind === "ignore" ? translate("errors.notFoundMessage") : action.message}
      </p>
      {isAuthExpiry && action.kind === "toast" && action.action !== undefined ? (
        <a href={action.action.url} className={HOME_LINK_CLASS}>
          {action.action.label}
        </a>
      ) : (
        <ErrorActions onRetry={handleRetry} />
      )}
    </div>
  );
}
