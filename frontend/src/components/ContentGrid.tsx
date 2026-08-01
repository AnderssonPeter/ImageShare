/**
 * ContentGrid — virtualized grid of folder/file tiles for the browse view.
 *
 * Flattens the infinite-query pages into a flat item array, computes a
 * responsive column count from the container width (ResizeObserver), and
 * virtualizes rows (each row renders `columns` tiles). Auto-loads the next
 * page when the last visible row approaches the end.
 */
import {
  type CSSProperties,
  type ReactNode,
  type RefObject,
  useEffect,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { type ReactVirtualizer, type VirtualItem, useVirtualizer } from "@tanstack/react-virtual";
import { type FolderEntry } from "@lib/api/generated/imageShare.schemas";
import MetroTile from "@components/MetroTile";
import { tw } from "@lib/utils";
import { useFolderContent } from "@lib/api/contentQueries";

/** Square tile size in pixels (Metro medium tile). */
const TILE_SIZE = 160;

/** Gap between tiles in pixels (matches --spacing-gutter). */
const GUTTER = 2;

/** Rows from the end before auto-loading the next page. */
const AUTOLOAD_THRESHOLD = 3;

/** Rows rendered beyond the viewport for smooth scrolling. */
const OVERSCAN = 4;

/** Total row height including the gutter below each row. */
const ROW_HEIGHT = TILE_SIZE + GUTTER;

/** Skeleton placeholder rows while the first page loads. */
const SKELETON_ROWS = 4;

/** Static styles — module-level so react-perf/jsx-no-new-object-as-prop is satisfied. */
const TILE_SIZE_CLASS = tw`w-[160px] h-[160px]`;
const SKELETON_TILE_CLASS = tw`animate-pulse bg-muted rounded-sm w-[160px] h-[160px]`;
const ROW_POSITION_BASE_STYLE: CSSProperties = {
  position: "absolute",
  top: 0,
  left: 0,
  width: "100%",
};
const SCROLL_CONTAINER_CLASS = tw`overflow-auto h-[calc(100dvh-3rem)] p-gutter`;

type GridVirtualizer = ReactVirtualizer<HTMLDivElement, Element>;

function useColumnCount(containerRef: RefObject<HTMLDivElement | null>): number {
  const [columns, setColumns] = useState(1);

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
      const width = current.clientWidth;
      setColumns(Math.max(1, Math.floor((width + GUTTER) / (TILE_SIZE + GUTTER))));
    }
    update();
    const observer = new ResizeObserver(update);
    observer.observe(element);
    return () => observer.disconnect();
  }, [containerRef]);

  return columns;
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
  return (
    <MetroTile className={TILE_SIZE_CLASS}>
      <span className="absolute bottom-1 left-1 text-sm leading-none">{entry.name}</span>
    </MetroTile>
  );
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

interface VirtualRowProps {
  virtualRow: VirtualItem;
  items: FolderEntry[];
  startIndex: number;
  columns: number;
}

function VirtualRow({ virtualRow, items, startIndex, columns }: VirtualRowProps) {
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

interface VirtualGridBodyProps {
  rowVirtualizer: GridVirtualizer;
  items: FolderEntry[];
  columns: number;
}

function VirtualGridBody({ rowVirtualizer, items, columns }: VirtualGridBodyProps) {
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

function SkeletonRow({ columns }: { columns: number }) {
  const tiles: ReactNode[] = [];
  for (let column = 0; column < columns; column++) {
    tiles.push(<div key={column} className={SKELETON_TILE_CLASS} />);
  }
  return <div className="flex gap-gutter">{tiles}</div>;
}

function SkeletonGrid({ columns }: { columns: number }): ReactNode[] {
  const rows: ReactNode[] = [];
  for (let row = 0; row < SKELETON_ROWS; row++) {
    rows.push(
      <div key={row} className="mb-gutter">
        <SkeletonRow columns={columns} />
      </div>,
    );
  }
  return rows;
}

function EmptyState() {
  return (
    <div className="flex h-full items-center justify-center p-8">
      <p className="text-sm text-muted-foreground">This folder is empty.</p>
    </div>
  );
}

interface ContentOptions {
  isPending: boolean;
  items: FolderEntry[];
  columns: number;
  rowVirtualizer: GridVirtualizer;
}

function renderContent({ isPending, items, columns, rowVirtualizer }: ContentOptions): ReactNode {
  if (isPending) {
    return <SkeletonGrid columns={columns} />;
  }
  if (items.length === 0) {
    return <EmptyState />;
  }
  return <VirtualGridBody rowVirtualizer={rowVirtualizer} items={items} columns={columns} />;
}

export default function ContentGrid({ path }: { path?: string }): React.JSX.Element {
  const scrollRef = useRef<HTMLDivElement>(null);
  const columns = useColumnCount(scrollRef);
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
  const content = renderContent({ isPending, items, columns, rowVirtualizer });
  return (
    <div ref={scrollRef} className={SCROLL_CONTAINER_CLASS}>
      {content}
    </div>
  );
}
