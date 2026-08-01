/**
 * Share-token route — `/admin/share/{token}`.
 *
 * An admin generates a JWT share token (Phase 7 `ShareLinkDialog`) and lands
 * here to preview the shareable sign-in link. The link points to the backend
 * `GET /api/authentication/login/jwt/{token}` endpoint, which validates the
 * JWT, sets the auth cookie, and redirects — so a share recipient never
 * needs to reach the SPA unauthenticated; the backend handles sign-in and
 * only then serves the app.
 *
 * This route is nested under the admin layout, so the `beforeLoad` isAdmin
 * gate already ensures only admins can reach it.
 */
import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/admin/share/$token")({
  component: ShareTokenComponent,
});

function ShareTokenComponent(): React.JSX.Element {
  const { token } = Route.useParams();
  const shareUrl = `/api/authentication/login/jwt/${token}`;

  return (
    <div className="flex flex-col gap-gutter p-4">
      <h2 className="text-lg font-semibold text-foreground">Share Link</h2>
      <code className="text-sm text-muted-foreground">{shareUrl}</code>
    </div>
  );
}
