import {
  type ReactNode,
  type RefObject,
  createContext,
  useContext,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { type FolderEntry } from "@lib/api/generated";
import FolderTile from "@components/FolderTile";
import GridBackground from "@components/GridBackground";
import ImageTile from "@components/ImageTile";
import Skeleton from "@components/ui/Skeleton";
import { chunk, tw } from "@lib/utils";
import { useFolderContent } from "@lib/api/contentQueries";
import { useTranslation } from "@lib/i18n";

const TILE_WIDTH = 180;
const TILE_HEIGHT = 120;
const GUTTER = 4;
const ROW_HEIGHT = TILE_HEIGHT + GUTTER;

const TILE_SIZE_CLASS = tw`w-[180px] h-[120px]`;
const CONTAINER_CLASS = tw`relative h-[calc(100dvh-3rem)]`;
const SCROLL_ACTIVE_CLASS = tw`relative z-10 h-full overflow-auto p-gutter`;
const SCROLL_LOCKED_CLASS = tw`relative z-10 h-full overflow-hidden p-gutter`;
const ROW_CLASS = tw`mb-gutter flex gap-gutter`;

interface GridActions {
  onNavigateFolder: (path: string) => void;
  onImageOpen: (path: string) => void;
}

const GridActionsContext = createContext<GridActions | undefined>(undefined);

interface GridShape {
  columns: number;
  skeletonRows: number;
}

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

function SkeletonGrid({ columns, rows }: { columns: number; rows: number }): ReactNode[] {
  const renderedRows: ReactNode[] = [];
  for (let row = 0; row < rows; row++) {
    const tiles: ReactNode[] = [];
    for (let column = 0; column < columns; column++) {
      tiles.push(<Skeleton key={column} className={TILE_SIZE_CLASS} />);
    }
    renderedRows.push(
      <div key={row} className={ROW_CLASS}>
        {tiles}
      </div>,
    );
  }
  return renderedRows;
}

function EmptyFolder(): React.JSX.Element {
  const { t: translate } = useTranslation();
  return (
    <div className="flex h-full items-center justify-center p-8">
      <p className="text-sm text-muted-foreground">{translate("content.emptyFolder")}</p>
    </div>
  );
}

interface GridContentProps {
  isPending: boolean;
  data: FolderEntry[] | undefined;
  columns: number;
  skeletonRows: number;
  rows: FolderEntry[][];
}

function renderGridContent({
  isPending,
  data,
  columns,
  skeletonRows,
  rows,
}: GridContentProps): ReactNode {
  if (isPending) {
    return <SkeletonGrid columns={columns} rows={skeletonRows} />;
  }
  if ((data ?? []).length === 0) {
    return <EmptyFolder />;
  }
  return rows.map((rowItems) => (
    <div key={rowItems[0].path} className={ROW_CLASS}>
      {rowItems.map((entry) => (
        <GridTile key={entry.path} entry={entry} />
      ))}
    </div>
  ));
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
  const { data, isPending } = useFolderContent(path);
  const rows = useMemo(() => chunk(data ?? [], columns), [data, columns]);
  const actions = useMemo<GridActions>(
    () => ({ onNavigateFolder, onImageOpen }),
    [onNavigateFolder, onImageOpen],
  );
  const content = renderGridContent({ isPending, data, columns, skeletonRows, rows });
  return (
    <GridActionsContext.Provider value={actions}>
      <div className={CONTAINER_CLASS}>
        <GridBackground path={path} />
        <div ref={scrollRef} className={isPending ? SCROLL_LOCKED_CLASS : SCROLL_ACTIVE_CLASS}>
          {content}
        </div>
      </div>
    </GridActionsContext.Provider>
  );
}
