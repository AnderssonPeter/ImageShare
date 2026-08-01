/**
 * MetroTile — flat Metro/Windows 8 tile with press-nudge animation.
 *
 * Uses Tailwind classes for layout, colors, and hover/active states. A
 * child `<div>` (positioned absolutely with overscan) renders the
 * background image and translates up on `group-active`, matching the
 * original `::before` behaviour without custom CSS.
 *
 * The `--metro-tile-image` CSS custom property is replaced by an
 * `imageUrl` prop; tiles without a cover image render a flat fill.
 */
import { type CSSProperties, type ReactNode, useMemo } from "react";
import cn, { tw } from "@lib/utils";

const BASE_CLASS = tw`group relative flex items-center justify-center overflow-hidden rounded-[var(--radius)] bg-tile text-tile-foreground hover:bg-muted active:bg-muted`;
const OVERLAY_BASE_CLASS = tw`pointer-events-none absolute -inset-1 bg-cover bg-center transition-transform duration-100 ease-out group-active:translate-y-[-2px]`;
const CONTENT_CLASS = tw`relative z-[1]`;

interface MetroTileProps {
  /** Background image URL for the tile cover (optional). */
  imageUrl?: string;
  /** Additional classes merged after the base classes. */
  className?: string;
  /** Inline style for the tile root element. */
  style?: CSSProperties;
  /** Tile content (icon, label, etc.). */
  children: ReactNode;
}

export default function MetroTile({
  imageUrl,
  className,
  style,
  children,
}: MetroTileProps): React.JSX.Element {
  const overlayStyle = useMemo<CSSProperties | undefined>(() => {
    if (imageUrl === undefined) {
      return;
    }
    return { backgroundImage: `url(${imageUrl})` };
  }, [imageUrl]);

  return (
    <div className={cn(BASE_CLASS, className)} style={style}>
      {overlayStyle !== undefined && <div className={OVERLAY_BASE_CLASS} style={overlayStyle} />}
      <div className={CONTENT_CLASS}>{children}</div>
    </div>
  );
}
