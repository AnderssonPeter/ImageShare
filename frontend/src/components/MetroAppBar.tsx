/**
 * MetroAppBar — the top application bar (Metro flat aesthetic).
 *
 * Renders a flat, borderless bar (no shadows, 2px radius per the Metro
 * design system) with three regions:
 *  - Left:   the ImageShare logo + app title.
 *  - Centre: a `breadcrumb` slot (supplied by the active route).
 *  - Right:  theme toggle, admin button (gated on `user.isAdmin`), and a
 *            user chip showing the signed-in user's name.
 *
 * The bar is a presentational shell — it takes `user` and `breadcrumb` as
 * props so it can be tested in isolation before the router context (Phase 4)
 * is wired up. Page content is rendered below the bar via `children`.
 */
import { Shield, User } from 'lucide-react'
import Button from '@components/ui/Button'
import { type IUser } from '@lib/api/generated/imageShare.schemas'
import Logo from '@components/Logo'
import { type ReactNode } from 'react'
import ThemeToggle from '@components/ThemeToggle'
import Tooltip from '@components/ui/Tooltip'

interface AdminButtonProps {
  visible: boolean
}

function AdminButton({ visible }: AdminButtonProps) {
  if (!visible) {
    return
  }
  return (
    <Tooltip.Tooltip>
      <Tooltip.TooltipTrigger
        className={Button.buttonVariants({ variant: 'ghost', size: 'icon' })}
        aria-label="Admin"
      >
        <Shield className="size-4" />
      </Tooltip.TooltipTrigger>
      <Tooltip.TooltipContent>Admin</Tooltip.TooltipContent>
    </Tooltip.Tooltip>
  )
}

function UserChip({ name }: { name: string | undefined }) {
  return (
    <div className="flex items-center gap-2 px-2 text-sm text-foreground">
      <User className="size-4 text-muted-foreground" />
      <span className="max-w-32 truncate">{name ?? 'User'}</span>
    </div>
  )
}

function AppBarLeft() {
  return (
    <div className="flex shrink-0 items-center gap-2">
      <Logo className="size-6 text-primary" />
      <span className="text-sm font-medium text-foreground">ImageShare</span>
    </div>
  )
}

function AppBarBreadcrumb({ breadcrumb }: { breadcrumb: ReactNode }) {
  if (breadcrumb === undefined) {
    return
  }
  return <div className="min-w-0 flex-1">{breadcrumb}</div>
}

interface AppBarRightProps {
  user: IUser
}

function AppBarRight({ user }: AppBarRightProps) {
  return (
    <div className="flex shrink-0 items-center gap-1">
      <ThemeToggle />
      <AdminButton visible={user.isAdmin === true} />
      <UserChip name={user.name ?? undefined} />
    </div>
  )
}

export default function MetroAppBar({
  user,
  breadcrumb,
  children,
}: {
  user: IUser
  breadcrumb?: ReactNode
  children: ReactNode
}): React.JSX.Element {
  return (
    <div className="flex min-h-screen flex-col">
      <header className="flex h-12 items-center gap-3 border-b border-border bg-background px-4">
        <AppBarLeft />
        <AppBarBreadcrumb breadcrumb={breadcrumb} />
        <AppBarRight user={user} />
      </header>
      <main className="flex-1">{children}</main>
    </div>
  )
}
