/** ContentGrid — virtualized grid of folder/file tiles for the browse view. */
import {
  type CSSProperties,
  type ReactNode,
  type RefObject,
  createContext,
  useContext,
  useEffect,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { type ReactVirtualizer, type VirtualItem, useVirtualizer } from "@tanstack/react-virtual";
import { type FolderEntry } from "@lib/api/generated";
import FolderTile from "@components/FolderTile";
import GridBackground from "@components/GridBackground";
import ImageTile from "@components/ImageTile";
import Skeleton from "@components/ui/Skeleton";
import { tw } from "@lib/utils";
import { useFolderContent } from "@lib/api/contentQueries";

/** Tile width in pixels (Metro wide tile, 3:2 aspect ratio). */
const TILE_WIDTH = 180;
/** Tile height in pixels (TILE_WIDTH * 2/3 = 120, 3:2 aspect ratio). */
const TILE_HEIGHT = 120;
/** Gap between tiles in pixels (matches --spacing-gutter). */
const GUTTER = 4;
/** Rows from the end before auto-loading the next page. */
const AUTOLOAD_THRESHOLD = 3;
/** Rows rendered beyond the viewport for smooth scrolling. */
const OVERSCAN = 4;
/** Total row height including the gutter below each row. */
const ROW_HEIGHT = TILE_HEIGHT + GUTTER;

/** Static styles — module-level so react-perf/jsx-no-new-object-as-prop is satisfied. */
const TILE_SIZE_CLASS = tw`w-[180px] h-[120px]`;
const ROW_POSITION_BASE_STYLE: CSSProperties = {
  position: "absolute",
  top: 0,
  left: 0,
  width: "100%",
};
const SCROLL_ACTIVE_CLASS = tw`relative h-[calc(100dvh-3rem)] p-gutter overflow-auto`;
const SCROLL_LOCKED_CLASS = tw`relative h-[calc(100dvh-3rem)] p-gutter overflow-hidden`;
const CONTENT_WRAPPER_CLASS = tw`relative z-10`;

type GridVirtualizer = ReactVirtualizer<HTMLDivElement, Element>;

/** Tile interactions supplied by `ContentGrid` and consumed by `GridTile`. */
interface GridActions {
  onNavigateFolder: (path: string) => void;
  onImageOpen: (path: string) => void;
}

const GridActionsContext = createContext<GridActions | undefined>(undefined);

interface GridShape {
  columns: number;
  skeletonRows: number;
}

/**
 * Measure the scroll container to derive grid columns and the skeleton row
 * count that covers the full viewport (ceil, min 1; `overflow-hidden` during
 * load clips any overshoot). Recomputed on resize via a single observer.
 */
function useGridShape(containerRef: RefObject<HTMLDivElement | null>): GridShape {
  const [shape, setShape] = useState<GridShape>({ columns: 1, skeletonRows: 1 });

  useLayoutEffect(() => {
    const element = containerRef.current;
    if (element === null) {
      return;
    }
    function update() {
      const { current } = containerRef;
      if (current === null) {
        return;
      }
      const innerHeight = current.clientHeight - 2 * GUTTER;
      setShape({
        columns: Math.max(1, Math.floor((current.clientWidth + GUTTER) / (TILE_WIDTH + GUTTER))),
        skeletonRows: Math.max(1, Math.ceil(innerHeight / ROW_HEIGHT)),
      });
    }
    update();
    const observer = new ResizeObserver(update);
    observer.observe(element);
    return () => observer.disconnect();
  }, [containerRef]);

  return shape;
}

interface AutoloadOptions {
  rowVirtualizer: GridVirtualizer;
  rowCount: number;
  hasNextPage: boolean;
  isFetchingNextPage: boolean;
  fetchNextPage: () => void;
}

function useAutoload({
  rowVirtualizer,
  rowCount,
  hasNextPage,
  isFetchingNextPage,
  fetchNextPage,
}: AutoloadOptions): void {
  const virtualItems = rowVirtualizer.getVirtualItems();
  const lastVisibleRow = virtualItems.at(-1);

  useEffect(() => {
    if (lastVisibleRow === undefined) {
      return;
    }
    if (
      lastVisibleRow.index >= rowCount - AUTOLOAD_THRESHOLD &&
      hasNextPage &&
      !isFetchingNextPage
    ) {
      fetchNextPage();
    }
  }, [lastVisibleRow, rowCount, hasNextPage, isFetchingNextPage, fetchNextPage]);
}

function GridTile({ entry }: { entry: FolderEntry }) {
  const actions = useContext(GridActionsContext);
  if (actions === undefined) {
    throw new Error("GridTile must be used within a ContentGrid");
  }
  const { onNavigateFolder, onImageOpen } = actions;
  if (entry.type === "Folder") {
    return <FolderTile entry={entry} onNavigate={onNavigateFolder} className={TILE_SIZE_CLASS} />;
  }
  return <ImageTile entry={entry} onOpen={onImageOpen} className={TILE_SIZE_CLASS} />;
}

function GridRow({ items }: { items: FolderEntry[] }) {
  return (
    <div className="flex gap-gutter">
      {items.map((entry) => (
        <GridTile key={entry.path} entry={entry} />
      ))}
    </div>
  );
}

function VirtualRow({
  virtualRow,
  items,
  startIndex,
  columns,
}: {
  virtualRow: VirtualItem;
  items: FolderEntry[];
  startIndex: number;
  columns: number;
}) {
  const style = useMemo<CSSProperties>(
    () => ({
      ...ROW_POSITION_BASE_STYLE,
      height: virtualRow.size,
      transform: `translateY(${virtualRow.start}px)`,
    }),
    [virtualRow.size, virtualRow.start],
  );

  const rowItems = useMemo(
    () => items.slice(startIndex, startIndex + columns),
    [items, startIndex, columns],
  );

  return (
    <div style={style}>
      <GridRow items={rowItems} />
    </div>
  );
}

function VirtualGridBody({
  rowVirtualizer,
  items,
  columns,
}: {
  rowVirtualizer: GridVirtualizer;
  items: FolderEntry[];
  columns: number;
}) {
  const virtualItems = rowVirtualizer.getVirtualItems();
  const sizerStyle = useMemo<CSSProperties>(
    () => ({
      height: rowVirtualizer.getTotalSize(),
      position: "relative",
    }),
    [rowVirtualizer],
  );

  return (
    <div style={sizerStyle}>
      {virtualItems.map((virtualRow) => (
        <VirtualRow
          key={virtualRow.key}
          virtualRow={virtualRow}
          items={items}
          startIndex={virtualRow.index * columns}
          columns={columns}
        />
      ))}
    </div>
  );
}

function SkeletonGrid({ columns, rows }: { columns: number; rows: number }): ReactNode[] {
  const renderedRows: ReactNode[] = [];
  for (let row = 0; row < rows; row++) {
    const tiles: ReactNode[] = [];
    for (let column = 0; column < columns; column++) {
      tiles.push(<Skeleton key={column} className={TILE_SIZE_CLASS} />);
    }
    renderedRows.push(
      <div key={row} className="mb-gutter">
        <div className="flex gap-gutter">{tiles}</div>
      </div>,
    );
  }
  return renderedRows;
}

function renderContent({
  isPending,
  items,
  columns,
  skeletonRows,
  rowVirtualizer,
}: {
  isPending: boolean;
  items: FolderEntry[];
  columns: number;
  skeletonRows: number;
  rowVirtualizer: GridVirtualizer;
}): ReactNode {
  if (isPending) {
    return <SkeletonGrid columns={columns} rows={skeletonRows} />;
  }
  if (items.length === 0) {
    return (
      <div className="flex h-full items-center justify-center p-8">
        <p className="text-sm text-muted-foreground">This folder is empty.</p>
      </div>
    );
  }
  return <VirtualGridBody rowVirtualizer={rowVirtualizer} items={items} columns={columns} />;
}

interface ContentGridProps {
  path?: string;
  onNavigateFolder: (path: string) => void;
  onImageOpen: (path: string) => void;
}

export default function ContentGrid({
  path,
  onNavigateFolder,
  onImageOpen,
}: ContentGridProps): React.JSX.Element {
  const scrollRef = useRef<HTMLDivElement>(null);
  const { columns, skeletonRows } = useGridShape(scrollRef);
  const { data, fetchNextPage, hasNextPage, isFetchingNextPage, isPending } =
    useFolderContent(path);
  const items = useMemo(() => data?.pages.flatMap((page) => page.items) ?? [], [data]);
  const rowCount = Math.ceil(items.length / columns);
  const rowVirtualizer = useVirtualizer({
    count: rowCount,
    getScrollElement: () => scrollRef.current,
    estimateSize: () => ROW_HEIGHT,
    overscan: OVERSCAN,
  });
  useAutoload({
    rowVirtualizer,
    rowCount,
    hasNextPage: hasNextPage ?? false,
    isFetchingNextPage,
    fetchNextPage,
  });
  const actions = useMemo<GridActions>(
    () => ({ onNavigateFolder, onImageOpen }),
    [onNavigateFolder, onImageOpen],
  );
  const content = renderContent({ isPending, items, columns, skeletonRows, rowVirtualizer });
  return (
    <GridActionsContext.Provider value={actions}>
      <div ref={scrollRef} className={isPending ? SCROLL_LOCKED_CLASS : SCROLL_ACTIVE_CLASS}>
        <GridBackground path={path} />
        <div className={CONTENT_WRAPPER_CLASS}>{content}</div>
      </div>
    </GridActionsContext.Provider>
  );
}
