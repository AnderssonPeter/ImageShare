import { Loader2, X } from "lucide-react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import Button from "@components/ui/Button";
import Carousel from "@components/ui/Carousel";
import { type FolderEntry } from "@lib/api/generated";
import { type UseEmblaCarouselType } from "embla-carousel-react";
import { imageUrl } from "@lib/api/urls";
import { tw } from "@lib/utils";
import { useCanLoadImages } from "@lib/api/usageAgreement";
import { useFolderContent } from "@lib/api/contentQueries";

type CarouselApi = UseEmblaCarouselType[1];
type ReadyHandler = (api: CarouselApi | undefined) => void;

const MAX_RANGE = 3;
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
  folderPath?: string;
  imageName: string;
  onImageChange?: (name: string) => void;
  onClose: () => void;
}

interface CarouselApiHandlerOptions {
  images: FolderEntry[];
  imageName: string;
  onImageChange: ((name: string) => void) | undefined;
  onReady: ReadyHandler;
}

function isImage(entry: FolderEntry): boolean {
  return entry.type === "File";
}

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

interface ImageSlideProps {
  entry: FolderEntry;
  index: number;
  shouldLoad: boolean;
  onLoaded: (index: number) => void;
}

function ImageSlide({ entry, index, shouldLoad, onLoaded }: ImageSlideProps): React.JSX.Element {
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

interface ImageSlidesProps {
  images: FolderEntry[];
  currentIndex: number;
  effectiveRange: number;
  canLoadImages: boolean;
  onSlideLoaded: (index: number) => void;
}

function ImageSlides({
  images,
  currentIndex,
  effectiveRange,
  canLoadImages,
  onSlideLoaded,
}: ImageSlidesProps): React.JSX.Element {
  return (
    <Carousel.CarouselContent className={CONTENT_CLASS}>
      {images.map((entry, index) => (
        <ImageSlide
          key={entry.path}
          entry={entry}
          index={index}
          shouldLoad={canLoadImages && Math.abs(index - currentIndex) <= effectiveRange}
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

function useCarouselApiHandler(options: CarouselApiHandlerOptions): {
  opts: { startIndex: number };
  setApi: (api: CarouselApi) => void;
  currentIndex: number;
} {
  const { images, imageName, onImageChange, onReady } = options;
  const latestRef = useRef({ images, imageName, onImageChange });
  latestRef.current = { images, imageName, onImageChange };
  const startIndexRef = useRef<number | null>(null);
  if (startIndexRef.current === null) {
    startIndexRef.current = Math.max(
      0,
      images.findIndex((entry) => entry.name === imageName),
    );
  }
  const startIndex = startIndexRef.current;
  const [currentIndex, setCurrentIndex] = useState(startIndex);
  const setApi = useCallback(
    (api: CarouselApi) => {
      onReady(api);
      if (api === undefined) {
        return;
      }
      const embla = api;
      function onSelect() {
        const index = embla.selectedScrollSnap();
        setCurrentIndex(index);
        const latest = latestRef.current;
        const name = latest.images[index]?.name;
        if (name !== undefined && name !== latest.imageName) {
          latest.onImageChange?.(name);
        }
      }
      setCurrentIndex(embla.selectedScrollSnap());
      embla.on("select", onSelect);
    },
    [onReady],
  );
  return { opts: useMemo(() => ({ startIndex }), [startIndex]), setApi, currentIndex };
}

interface ImageCarouselProps {
  images: FolderEntry[];
  imageName: string;
  onImageChange?: (name: string) => void;
  onCarouselReady: ReadyHandler;
}

function ImageCarousel({
  images,
  imageName,
  onImageChange,
  onCarouselReady,
}: ImageCarouselProps): React.JSX.Element {
  const canLoadImages = useCanLoadImages();
  const { opts, setApi, currentIndex } = useCarouselApiHandler({
    images,
    imageName,
    onImageChange,
    onReady: onCarouselReady,
  });
  const { handleSlideLoaded, effectiveRange } = useProgressiveLoading(currentIndex, images.length);
  return (
    <Carousel.Carousel opts={opts} setApi={setApi} className={CAROUSEL_CLASS}>
      <ImageSlides
        images={images}
        currentIndex={currentIndex}
        effectiveRange={effectiveRange}
        canLoadImages={canLoadImages}
        onSlideLoaded={handleSlideLoaded}
      />
      <Carousel.CarouselPrevious className={NAV_BUTTON_CLASS} />
      <Carousel.CarouselNext className={NAV_NEXT_CLASS} />
    </Carousel.Carousel>
  );
}

function CloseButton({ onClose }: { onClose: () => void }): React.JSX.Element {
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

function useKeyboardNavigation(api: CarouselApi | undefined, onClose: () => void): void {
  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        onClose();
        return;
      }
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
  }, [api, onClose]);
}

export default function ImageViewer({
  folderPath,
  imageName,
  onImageChange,
  onClose,
}: ImageViewerProps): React.JSX.Element {
  const { data } = useFolderContent(folderPath);
  const images = useMemo(
    () => (data?.pages.flatMap((page) => page.items) ?? []).filter((entry) => isImage(entry)),
    [data],
  );
  const [carouselApi, setCarouselApi] = useState<CarouselApi | undefined>();
  const handleCarouselReady = useCallback((api: CarouselApi | undefined) => {
    setCarouselApi(api);
  }, []);
  useKeyboardNavigation(carouselApi, onClose);
  return (
    <div className={OVERLAY_CLASS}>
      <CloseButton onClose={onClose} />
      {images.length > 0 && (
        <ImageCarousel
          images={images}
          imageName={imageName}
          onImageChange={onImageChange}
          onCarouselReady={handleCarouselReady}
        />
      )}
    </div>
  );
}
