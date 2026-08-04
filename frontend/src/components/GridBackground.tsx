/**
 * GridBackground — a random full-resolution image rendered behind the grid
 * content and dimmed to near-invisibility via a `bg-background/90` overlay
 * (dark in dark mode, light in light mode).
 *
 * When `path` is provided, picks a random image recursively from that folder
 * (`randomFolderUrl`). When `path` is undefined/empty (root listing), picks a
 * random image from the root via `randomRootUrl`. The overlay sits on top of
 * the image so only a faint texture shows through.
 *
 * The random endpoint may return a different image per request, so the next
 * image is preloaded with `fetch` and decoded before the visible layer swaps.
 * Two stacked layers alternate: the incoming image fades in (0 → 1) while the
 * outgoing image fades out (1 → 0) simultaneously — a seamless cross-fade with
 * no blank gap. Object URLs are revoked after the fade completes.
 */
import { type CSSProperties, useEffect, useMemo, useRef, useState } from "react";
import { randomFolderUrl, randomRootUrl } from "@lib/api/urls";
import { tw } from "@lib/utils";

/** CSS fade duration in milliseconds — must match the `duration-700` class. */
const FADE_MS = 700;
const BACKGROUND_IMAGE_CLASS = tw`absolute inset-0 bg-cover bg-center transition-opacity duration-700 ease-in-out`;
const BACKGROUND_OVERLAY_CLASS = tw`pointer-events-none absolute inset-0 bg-background/90`;

interface BackgroundState {
  front: string | undefined;
  back: string | undefined;
  showFront: boolean;
}

const INITIAL_STATE: BackgroundState = { back: undefined, front: undefined, showFront: true };

/** Load `url` on the inactive layer, then flip which layer is visible. */
function advanceLayers(state: BackgroundState, url: string): BackgroundState {
  const showFront = !state.showFront;
  return showFront
    ? { back: state.back, front: url, showFront }
    : { back: url, front: state.front, showFront };
}

/** Fetch `url`, decode the image, and return a stable object URL. */
async function preloadImageObjectUrl(url: string, signal: AbortSignal): Promise<string> {
  const response = await fetch(url, { credentials: "same-origin", signal });
  if (!response.ok) {
    throw new Error(`background preload failed: ${response.status}`);
  }
  const blob = await response.blob();
  const objectUrl = URL.createObjectURL(blob);
  /* Force the browser to decode the image before we hand the URL back.
   * Without this, swapping it into `background-image` still triggers a decode
   * on the main thread, causing a brief flash where the old image is gone
   * but the new one isn't painted yet. `Image.decode()` resolves once the
   * decoded frames are ready, so the subsequent CSS swap paints in the same
   * frame. */
  const image = new Image();
  image.decoding = "async";
  image.src = objectUrl;
  await image.decode();
  return objectUrl;
}

function BackgroundLayer({
  url,
  visible,
}: {
  url: string | undefined;
  visible: boolean;
}): React.JSX.Element {
  const style = useMemo<CSSProperties>(
    () =>
      url === undefined
        ? { opacity: visible ? 1 : 0 }
        : { opacity: visible ? 1 : 0, backgroundImage: `url(${url})` },
    [url, visible],
  );
  return <div aria-hidden className={BACKGROUND_IMAGE_CLASS} style={style} />;
}

export default function GridBackground({ path }: { path?: string }): React.JSX.Element {
  const targetUrl = path ? randomFolderUrl(path, false, true) : randomRootUrl(false);
  const [state, setState] = useState<BackgroundState>(INITIAL_STATE);
  const currentObjectUrl = useRef<string | null>(null);
  const objectUrls = useRef<Set<string>>(new Set());

  useEffect(() => {
    const controller = new AbortController();
    void (async () => {
      try {
        const nextObjectUrl = await preloadImageObjectUrl(targetUrl, controller.signal);
        const previousObjectUrl = currentObjectUrl.current;
        currentObjectUrl.current = nextObjectUrl;
        objectUrls.current.add(nextObjectUrl);
        setState((prev) => advanceLayers(prev, nextObjectUrl));
        if (previousObjectUrl !== null) {
          setTimeout(() => {
            URL.revokeObjectURL(previousObjectUrl);
            objectUrls.current.delete(previousObjectUrl);
          }, FADE_MS);
        }
      } catch {
        // Leave the previous background in place on abort or network error.
      }
    })();
    return () => {
      controller.abort();
    };
  }, [targetUrl]);

  useEffect(
    () => () => {
      for (const url of objectUrls.current) {
        URL.revokeObjectURL(url);
      }
    },
    [],
  );

  return (
    <>
      <BackgroundLayer url={state.front} visible={state.showFront} />
      <BackgroundLayer url={state.back} visible={!state.showFront} />
      <div aria-hidden className={BACKGROUND_OVERLAY_CLASS} />
    </>
  );
}
