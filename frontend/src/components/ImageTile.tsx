/**
 * ImageTile — a Metro tile representing an image file entry in the browse grid.
 *
 * The thumbnail comes from `imageUrl(path, true)` (the 200×200 variant).
 * The whole tile is the click target and opens the fullscreen carousel for
 * this image; the open intent is delegated to the caller through `onOpen`
 * (passed the image's RelativePath — its stable identity — so the carousel
 * can resolve its own position from the loaded items) keeping the tile free
 * of carousel coupling and unit-testable in isolation (the browse route
 * wires it to `ImageViewer` in Phase 6).
 */
import { type FolderEntry } from "@lib/api/generated/imageShare.schemas";
import MetroTile from "@components/MetroTile";
import { imageUrl } from "@lib/api/urls";
import { useCallback } from "react";

interface ImageTileProps {
  /** Image file entry to render (must have `type === 'File'`). */
  entry: FolderEntry;
  /** Invoked with the image's RelativePath when the tile is clicked. */
  onOpen: (path: string) => void;
  /** Additional classes merged onto the tile root (e.g. sizing). */
  className?: string;
}

function openLabel(name: string): string {
  return `Open image ${name}`;
}

export default function ImageTile({ entry, onOpen, className }: ImageTileProps): React.JSX.Element {
  const thumbnail = imageUrl(entry.path, true);
  const handleOpen = useCallback(() => onOpen(entry.path), [onOpen, entry.path]);
  return (
    <MetroTile
      imageUrl={thumbnail}
      className={className}
      onClick={handleOpen}
      ariaLabel={openLabel(entry.name)}
    />
  );
}
