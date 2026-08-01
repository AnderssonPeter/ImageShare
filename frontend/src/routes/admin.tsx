/**
 * Admin layout route — gates all `/admin/**` routes on `user.isAdmin`.
 *
 * The backend gates SPA access entirely (only authenticated users reach
 * the app), but within the SPA an admin-only page must still verify the
 * user has the admin role so a non-admin authenticated user navigating to
 * `/admin` is redirected rather than shown an empty/broken page. On
 * failure a `redirect` to `/browse` is thrown (307 by default).
 */
import { Outlet, createFileRoute, redirect } from "@tanstack/react-router";

export const Route = createFileRoute("/admin")({
  beforeLoad: ({ context }) => {
    if (!context.user.isAdmin) {
      throw redirect({ to: "/browse/$" });
    }
  },
  component: AdminLayout,
});

function AdminLayout(): React.JSX.Element {
  return <Outlet />;
}
