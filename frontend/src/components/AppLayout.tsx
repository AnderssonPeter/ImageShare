import { Outlet, useMatches } from "@tanstack/react-router";
import { type ReactNode, useCallback, useMemo } from "react";
import Breadcrumb from "@components/Breadcrumb";
import DownloadButton from "@components/DownloadButton";
import MetroAppBar from "@components/MetroAppBar";
import ShareDialogProvider from "@components/ShareDialogProvider";
import { type User } from "@lib/api/generated";
import { useShareDialog } from "@lib/shareDialogContext";
import { useUser } from "@lib/userContext";

/**
 * Read the browse route's splat from the match chain so the app bar can render
 * the breadcrumb. Returns `onBrowse: false` on non-browse routes (e.g. admin)
 * so the breadcrumb slot stays empty there.
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

/**
 * Build the share Return URL for the app-bar trigger from the browse splat.
 * Returns `undefined` at the root folder (empty splat) so the field stays
 * empty there; otherwise the `/browse/<folder>` permalink.
 */
function folderReturnUrl(splat: string | undefined): string | undefined {
  if (splat === undefined || splat === "") {
    return undefined;
  }
  return `/browse/${splat}`;
}

function AppBarShell({
  user,
  breadcrumb,
  splat,
}: {
  user: User;
  breadcrumb?: ReactNode;
  splat: string | undefined;
}): React.JSX.Element {
  const { openShare } = useShareDialog();
  const handleShare = useCallback(() => {
    openShare(folderReturnUrl(splat));
  }, [openShare, splat]);
  return (
    <MetroAppBar user={user} breadcrumb={breadcrumb} onShare={handleShare}>
      <Outlet />
    </MetroAppBar>
  );
}

export default function AppLayout(): React.JSX.Element {
  const { data: user } = useUser();
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
  if (user === undefined) {
    return <Outlet />;
  }
  return (
    <ShareDialogProvider enabled={user.isAdmin === true}>
      <AppBarShell user={user} breadcrumb={breadcrumb} splat={splat} />
    </ShareDialogProvider>
  );
}
