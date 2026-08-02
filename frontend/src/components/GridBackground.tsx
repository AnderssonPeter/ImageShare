/**
 * GridBackground — a random full-resolution image rendered behind the grid
 * content and dimmed to near-invisibility via a `bg-background/90` overlay
 * (dark in dark mode, light in light mode).
 *
 * When `path` is provided, picks a random image recursively from that folder
 * (`randomFolderUrl`). When `path` is undefined/empty (root listing), picks a
 * random image from the root via `randomRootUrl`. The overlay sits on top of
 * the image so only a faint texture shows through.
 */
import { type CSSProperties, useMemo } from "react";
import { randomFolderUrl, randomRootUrl } from "@lib/api/urls";
import { tw } from "@lib/utils";

const BACKGROUND_IMAGE_CLASS = tw`absolute inset-0 bg-cover bg-center`;
const BACKGROUND_OVERLAY_CLASS = tw`pointer-events-none absolute inset-0 bg-background/90`;

export default function GridBackground({ path }: { path?: string }): React.JSX.Element {
  const url = path ? randomFolderUrl(path, false, true) : randomRootUrl(false);
  const style = useMemo<CSSProperties>(
    () => ({ backgroundImage: `url(${url})` }),
    [url],
  );
  return (
    <>
      <div aria-hidden className={BACKGROUND_IMAGE_CLASS} style={style} />
      <div aria-hidden className={BACKGROUND_OVERLAY_CLASS} />
    </>
  );
}
