/**
 * FolderTile — a Metro tile representing a folder entry in the browse grid.
 *
 * The cover image is a random image picked recursively from the folder via
 * `randomFolderUrl(path, thumbnail, recursive)` (thumbnail + recursive so a
 * folder with images only in subfolders still gets a cover). The folder name
 * is overlaid bottom-left; the whole tile is the click target and navigates
 * into the folder on click.
 *
 * Navigation is delegated to the caller through `onNavigate` so the tile
 * stays free of router coupling and unit-testable in isolation (the browse
 * route wires it to TanStack Router's `navigate`). The per-folder download
 * action lives in the app-bar/breadcrumb menu, not on the tile.
 */
import { type FolderEntry } from "@lib/api/generated/imageShare.schemas";
import MetroTile from "@components/MetroTile";
import { randomFolderUrl } from "@lib/api/urls";
import { tw } from "@lib/utils";
import { useCallback } from "react";

const NAME_CLASS = tw`pointer-events-none absolute bottom-1 left-1 z-[1] text-sm leading-none text-tile-foreground`;

interface FolderTileProps {
  /** Folder entry to render (must have `type === 'Folder'`). */
  entry: FolderEntry;
  /** Invoked with the folder's RelativePath when the tile is clicked. */
  onNavigate: (path: string) => void;
  /** Additional classes merged onto the tile root (e.g. sizing). */
  className?: string;
}

function navigateLabel(name: string): string {
  return `Open folder ${name}`;
}

export default function FolderTile({
  entry,
  onNavigate,
  className,
}: FolderTileProps): React.JSX.Element {
  const cover = randomFolderUrl(entry.path, true, true);
  const handleNavigate = useCallback(() => onNavigate(entry.path), [onNavigate, entry.path]);
  return (
    <MetroTile
      imageUrl={cover}
      className={className}
      onClick={handleNavigate}
      ariaLabel={navigateLabel(entry.name)}
    >
      <span className={NAME_CLASS}>{entry.name}</span>
    </MetroTile>
  );
}
