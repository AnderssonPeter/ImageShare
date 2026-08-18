/**
 * MetroTile — flat Metro/Windows 8 tile that is always clickable.
 *
 * Renders a `<button>` so the whole tile is the click target. Uses Tailwind
 * classes for layout, colors, and hover/active states. A child `<div>`
 * (positioned absolutely with overscan) renders the background image and
 * translates up on `group-hover`, matching the original `::before`
 * behaviour without custom CSS.
 *
 * The cover image fades in (0 → 1) once the browser has finished loading it.
 * A hidden `Image()` preloads the URL; when its `onload` fires the overlay's
 * opacity transitions to 1. The `loadedUrl` state tracks *which* URL has
 * finished loading so that a prop change to a new URL immediately drops
 * opacity back to 0 (no flash of the old image) until the new one is ready.
 */
import { type CSSProperties, type ReactNode, useEffect, useMemo, useState } from "react";
import cn, { tw } from "@lib/utils";

const BASE_CLASS = tw`group relative flex shrink-0 items-center justify-center overflow-hidden rounded-[var(--radius)] bg-tile text-tile-foreground hover:bg-muted active:bg-muted`;
const OVERLAY_BASE_CLASS = tw`pointer-events-none absolute -inset-1 bg-cover bg-center transition-[transform,opacity] duration-300 ease-out group-hover:translate-y-[-2px]`;
const CONTENT_CLASS = tw`absolute inset-0 flex items-center justify-center`;

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
  const [loadedUrl, setLoadedUrl] = useState<string>();
  const loaded = imageUrl !== undefined && loadedUrl === imageUrl;
  const overlayStyle = useMemo<CSSProperties | undefined>(() => {
    if (imageUrl === undefined) {
      return;
    }
    return { backgroundImage: `url(${imageUrl})` };
  }, [imageUrl]);

  useEffect(() => {
    if (imageUrl === undefined) {
      return;
    }
    const image = new Image();
    image.addEventListener("load", () => setLoadedUrl(imageUrl), { once: true });
    image.src = imageUrl;
  }, [imageUrl]);

  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={ariaLabel}
      className={cn(BASE_CLASS, className)}
      style={style}
    >
      {overlayStyle !== undefined && (
        <div
          className={cn(OVERLAY_BASE_CLASS, loaded ? "opacity-100" : "opacity-0")}
          style={overlayStyle}
        />
      )}
      <div className={CONTENT_CLASS}>{children}</div>
    </button>
  );
}
