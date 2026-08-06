/**
 * MetroAppBar — the top application bar (Metro flat aesthetic).
 *
 * Renders a flat, borderless bar (no shadows, 2px radius per the Metro
 * design system) with three regions:
 *  - Left:   the ImageShare logo + app title.
 *  - Centre: a `breadcrumb` slot (supplied by the active route).
 *  - Right:  theme toggle, share button (gated on `user.isAdmin`), and a
 *            user chip showing the signed-in user's name.
 *
 * The bar is a presentational shell — it takes `user` and `breadcrumb` as
 * props so it can be tested in isolation. Page content is rendered below the
 * bar via `children`.
 */
import { CircleUserRound } from "lucide-react";
import { type ReactNode } from "react";
import { type User } from "@lib/api/generated";
import Logo from "@components/Logo";
import ShareButton from "@components/ShareButton";
import ThemeToggle from "@components/ThemeToggle";

interface ShareButtonSlotProps {
  visible: boolean;
  onShare: (() => void) | undefined;
}

function ShareButtonSlot({ visible, onShare }: ShareButtonSlotProps) {
  if (!visible) {
    return;
  }
  return <ShareButton onClick={onShare} variant="app-bar" />;
}

function UserChip({ name }: { name: string | undefined }) {
  return (
    <div className="flex items-center gap-2 px-2 text-sm text-foreground">
      <CircleUserRound className="size-4 text-muted-foreground" />
      <span className="max-w-32 truncate">{name ?? "User"}</span>
    </div>
  );
}

function AppBarLeft() {
  return (
    <div className="flex shrink-0 items-center gap-2">
      <Logo className="size-6 text-primary" />
      <span className="text-sm font-medium text-foreground">ImageShare</span>
    </div>
  );
}

function AppBarBreadcrumb({ breadcrumb }: { breadcrumb: ReactNode }) {
  if (breadcrumb === undefined) {
    return;
  }
  return <div className="min-w-0 flex-1">{breadcrumb}</div>;
}

interface AppBarRightProps {
  user: User;
  onShare: (() => void) | undefined;
}

function AppBarRight({ user, onShare }: AppBarRightProps): React.JSX.Element {
  return (
    <div className="flex shrink-0 items-center gap-1">
      <ThemeToggle />
      <ShareButtonSlot visible={user.isAdmin === true} onShare={onShare} />
      <UserChip name={user.name ?? undefined} />
    </div>
  );
}

export default function MetroAppBar({
  user,
  breadcrumb,
  onShare,
  children,
}: {
  user: User;
  breadcrumb?: ReactNode;
  onShare?: () => void;
  children: ReactNode;
}): React.JSX.Element {
  return (
    <div className="flex min-h-screen flex-col">
      <header className="flex h-12 items-center gap-3 border-b border-border bg-background px-4">
        <AppBarLeft />
        <AppBarBreadcrumb breadcrumb={breadcrumb} />
        <AppBarRight user={user} onShare={onShare} />
      </header>
      <main className="flex-1">{children}</main>
    </div>
  );
}
