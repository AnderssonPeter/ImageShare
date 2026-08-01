/**
 * Logo — the ImageShare six-tile Metro mark.
 *
 * Imported from `src/assets/logo.svg` (the single source, also referenced by
 * `index.html` as the favicon) via vite-plugin-svgr's `?react` query. Fills
 * use `currentColor`, so the logo inherits the surrounding text colour (e.g.
 * `text-primary` for the accent blue) and stays theme-aware.
 */
import LogoSvg from '@/assets/logo.svg?react'

export default function Logo({
  className,
}: {
  className?: string
}): React.JSX.Element {
  return <LogoSvg className={className} aria-hidden="true" />
}
