/**
 * ImageViewer — fullscreen carousel for the images in the current folder.
 *
 * Opens at the clicked image's index. Resolves the slide list itself from
 * the shared folder-content query cache (`useFolderContent(folderPath)`)
 * so the tile stays decoupled from carousel state — `onImageOpen` only
 * carries the image's `RelativePath`, its stable identity. Each slide shows
 * the full-resolution image (`imageUrl(path, false)`); prev/next are the
 * shadcn carousel controls. Keyboard nav and zoom land in later phases.
 *
 * Progressive loading: only the clicked image loads first. Once it has
 * finished, the window expands to ±1, then ±2, then ±3 — each step waits
 * for the previous ring to finish loading before the next ring starts.
 * This means only one image is ever fetching/decoding for the very first
 * paint, then 2, then 2 more, etc., keeping the main thread free for
 * smooth carousel transitions and never starving the clicked image.
 *
 * The viewer is controlled by its parent: pass an `imagePath` to open and
 * `onClose` to dismiss (the browse route toggles `imagePath` between a
 * string and `undefined`).
 */
import { EntryType, type FolderEntry } from "@lib/api/generated/imageShare.schemas";
import { Loader2, X } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";
import Button from "@components/ui/Button";
import Carousel from "@components/ui/Carousel";
import { type UseEmblaCarouselType } from "embla-carousel-react";
import { imageUrl } from "@lib/api/urls";
import { tw } from "@lib/utils";
import { useFolderContent } from "@lib/api/contentQueries";

type CarouselApi = UseEmblaCarouselType[1];

/** Maximum preload window: current slide ± this many slides. */
const MAX_RANGE = 3;

/** Delay (ms) before outer rings (±2, ±3) start loading after a slide switch. */
const OUTER_DELAY = 500;

const OVERLAY_CLASS = tw`fixed inset-0 z-50 flex flex-col bg-black/95`;
const CAROUSEL_CLASS = tw`h-full min-h-0 flex-1 [&_[data-slot=carousel-content]]:h-full`;
const CONTENT_CLASS = tw`h-full ml-0`;
const SLIDE_CLASS = tw`flex h-full items-center justify-center pl-0`;
const IMAGE_CLASS = tw`max-h-full max-w-full object-contain`;
const HIDDEN_CLASS = tw`hidden`;
const SPINNER_CLASS = tw`size-8 animate-spin text-white/70`;
const CLOSE_CLASS = tw`absolute top-2 right-2 z-10 rounded-full bg-black/50 text-white backdrop-blur-sm hover:bg-black/70`;
const NAV_BUTTON_CLASS = tw`left-2 right-auto z-10 rounded-full bg-black/50 text-white backdrop-blur-sm hover:bg-black/70 disabled:hidden`;
const NAV_NEXT_CLASS = tw`right-2 left-auto z-10 rounded-full bg-black/50 text-white backdrop-blur-sm hover:bg-black/70 disabled:hidden`;
const ICON_CLASS = tw`size-4`;

interface ImageViewerProps {
  /** Current folder RelativePath (undefined or empty = root listing). */
  folderPath?: string;
  /** RelativePath of the image to open at (its index in the slide list). */
  imagePath: string;
  /** Fired when the viewer requests to close. */
  onClose: () => void;
}

function isImage(entry: FolderEntry): boolean {
  return entry.type === EntryType.File;
}

/** Largest loaded ring (0 = only current, 1 = ±1, …) where every slide in the ring is loaded. */
function loadedRing(current: number, count: number, loaded: ReadonlySet<number>): number {
  for (let ring = 0; ring <= MAX_RANGE; ring++) {
    for (let offset = -ring; offset <= ring; offset++) {
      const index = current + offset;
      if (index >= 0 && index < count && !loaded.has(index)) {
        return ring - 1;
      }
    }
  }
  return MAX_RANGE;
}

function ImageSlide({
  entry,
  index,
  shouldLoad,
  onLoaded,
}: {
  entry: FolderEntry;
  index: number;
  shouldLoad: boolean;
  onLoaded: (index: number) => void;
}) {
  const [loaded, setLoaded] = useState(false);
  const handleLoad = useCallback(() => {
    setLoaded(true);
    onLoaded(index);
  }, [index, onLoaded]);
  const src = loaded || shouldLoad ? imageUrl(entry.path, false) : undefined;
  return (
    <Carousel.CarouselItem className={SLIDE_CLASS}>
      {!loaded && <Loader2 className={SPINNER_CLASS} />}
      <img
        src={src}
        alt={entry.name}
        decoding="async"
        fetchPriority={shouldLoad ? "high" : "low"}
        className={loaded ? IMAGE_CLASS : HIDDEN_CLASS}
        onLoad={handleLoad}
      />
    </Carousel.CarouselItem>
  );
}

function ImageSlides({
  images,
  currentIndex,
  effectiveRange,
  onSlideLoaded,
}: {
  images: FolderEntry[];
  currentIndex: number;
  effectiveRange: number;
  onSlideLoaded: (index: number) => void;
}) {
  return (
    <Carousel.CarouselContent className={CONTENT_CLASS}>
      {images.map((entry, index) => (
        <ImageSlide
          key={entry.path}
          entry={entry}
          index={index}
          shouldLoad={Math.abs(index - currentIndex) <= effectiveRange}
          onLoaded={onSlideLoaded}
        />
      ))}
    </Carousel.CarouselContent>
  );
}

function useProgressiveLoading(
  currentIndex: number,
  count: number,
): { handleSlideLoaded: (index: number) => void; effectiveRange: number } {
  const [loadedIndices, setLoadedIndices] = useState<ReadonlySet<number>>(new Set());
  const [outerReady, setOuterReady] = useState(false);

  useEffect(() => {
    setOuterReady(false);
    const timer = setTimeout(() => setOuterReady(true), OUTER_DELAY);
    return () => clearTimeout(timer);
  }, [currentIndex]);

  const handleSlideLoaded = useCallback((index: number) => {
    setLoadedIndices((prev) => (prev.has(index) ? prev : new Set(prev).add(index)));
  }, []);

  const effectiveRange = Math.min(
    loadedRing(currentIndex, count, loadedIndices) + 1,
    outerReady ? MAX_RANGE : 1,
  );

  return { handleSlideLoaded, effectiveRange };
}

function ImageCarousel({
  images,
  startIndex,
  onCarouselReady,
}: {
  images: FolderEntry[];
  startIndex: number;
  onCarouselReady?: (api: CarouselApi | undefined) => void;
  }) {
  const clampedStart = Math.max(0, startIndex);
  const opts = useMemo(() => ({ startIndex: clampedStart }), [clampedStart]);
  const [currentIndex, setCurrentIndex] = useState(clampedStart);
  const { handleSlideLoaded, effectiveRange } = useProgressiveLoading(currentIndex, images.length);

  const handleSetApi = useCallback(
    (api: CarouselApi) => {
      onCarouselReady?.(api);
      if (api === undefined) {
        return;
      }
      const embla = api;
      function onSelect() {
        setCurrentIndex(embla.selectedScrollSnap());
      }
      setCurrentIndex(embla.selectedScrollSnap());
      embla.on("select", onSelect);
    },
    [onCarouselReady],
  );

  return (
    <Carousel.Carousel opts={opts} setApi={handleSetApi} className={CAROUSEL_CLASS}>
      <ImageSlides
        images={images}
        currentIndex={currentIndex}
        effectiveRange={effectiveRange}
        onSlideLoaded={handleSlideLoaded}
      />
      <Carousel.CarouselPrevious className={NAV_BUTTON_CLASS} />
      <Carousel.CarouselNext className={NAV_NEXT_CLASS} />
    </Carousel.Carousel>
  );
}

function CloseButton({ onClose }: { onClose: () => void }) {
  return (
    <Button
      variant="ghost"
      size="icon-sm"
      className={CLOSE_CLASS}
      onClick={onClose}
      aria-label="Close"
    >
      <X className={ICON_CLASS} />
    </Button>
  );
}

export default function ImageViewer({
  folderPath,
  imagePath,
  onClose,
}: ImageViewerProps): React.JSX.Element {
  const { data } = useFolderContent(folderPath);
  const images = useMemo(
    () => (data?.pages.flatMap((page) => page.items) ?? []).filter((entry) => isImage(entry)),
    [data],
  );
  const startIndex = images.findIndex((entry) => entry.path === imagePath);
  const [carouselApi, setCarouselApi] = useState<CarouselApi | undefined>();
  const handleCarouselReady = useCallback((api: CarouselApi | undefined) => {
    setCarouselApi(api);
  }, []);

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        onClose();
        return;
      }
      const api = carouselApi;
      if (api === undefined) {
        return;
      }
      if (event.key === "ArrowLeft") {
        api.scrollPrev();
      } else if (event.key === "ArrowRight") {
        api.scrollNext();
      }
    }

    globalThis.addEventListener("keydown", onKeyDown);
    return () => globalThis.removeEventListener("keydown", onKeyDown);
  }, [onClose, carouselApi]);

  return (
    <div className={OVERLAY_CLASS}>
      <CloseButton onClose={onClose} />
      {images.length > 0 && (
        <ImageCarousel
          images={images}
          startIndex={startIndex}
          onCarouselReady={handleCarouselReady}
        />
      )}
    </div>
  );
}
