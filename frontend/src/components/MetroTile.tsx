/**
 * MetroTile — flat Metro/Windows 8 tile that is always clickable.
 *
 * Renders a `<button>` so the whole tile is the click target. Uses Tailwind
 * classes for layout, colors, and hover/active states. A child `<div>`
 * (positioned absolutely with overscan) renders the background image and
 * translates up on `group-hover`, matching the original `::before`
 * behaviour without custom CSS.
 *
 * The `--metro-tile-image` CSS custom property is replaced by an
 * `imageUrl` prop; tiles without a cover image render a flat fill.
 */
import { type CSSProperties, type ReactNode, useMemo } from "react";
import cn, { tw } from "@lib/utils";

const BASE_CLASS = tw`group relative flex items-center justify-center overflow-hidden rounded-[var(--radius)] bg-tile text-tile-foreground hover:bg-muted active:bg-muted`;
const OVERLAY_BASE_CLASS = tw`pointer-events-none absolute -inset-1 bg-cover bg-center transition-transform duration-100 ease-out group-hover:translate-y-[-2px]`;
const CONTENT_CLASS = tw`absolute inset-0 z-[1] flex items-center justify-center`;

interface MetroTileProps {
  /** Background image URL for the tile cover (optional). */
  imageUrl?: string;
  /** Additional classes merged after the base classes. */
  className?: string;
  /** Inline style for the tile root element. */
  style?: CSSProperties;
  /** Fired when the tile is clicked. */
  onClick: () => void;
  /** Accessible name for the tile button. */
  ariaLabel: string;
  /** Tile content (icon, label, etc.). */
  children?: ReactNode;
}

export default function MetroTile({
  imageUrl,
  className,
  style,
  onClick,
  ariaLabel,
  children,
}: MetroTileProps): React.JSX.Element {
  const overlayStyle = useMemo<CSSProperties | undefined>(() => {
    if (imageUrl === undefined) {
      return;
    }
    return { backgroundImage: `url(${imageUrl})` };
  }, [imageUrl]);

  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={ariaLabel}
      className={cn(BASE_CLASS, className)}
      style={style}
    >
      {overlayStyle !== undefined && <div className={OVERLAY_BASE_CLASS} style={overlayStyle} />}
      <div className={CONTENT_CLASS}>{children}</div>
    </button>
  );
}
