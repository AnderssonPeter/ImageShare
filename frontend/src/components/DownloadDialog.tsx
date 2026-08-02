/**
 * DownloadDialog — folder download dialog contents.
 *
 * Renders the `DialogContent` for the folder download flow: a recursive
 * toggle, a format radio group (avif/webp/jpg/all), and a Download action.
 * This is a controlled component — the owning trigger button holds the
 * open/format/recursive state and passes it in.
 *
 * The backend currently always downloads recursively (the `recursive`
 * toggle is a forward-looking UI option; it has no effect until the
 * download endpoint gains a `recursive` query parameter).
 */
import { type ChangeEvent, type ReactElement, useCallback, useMemo } from "react";
import Button from "@components/ui/Button";
import Dialog from "@components/ui/Dialog";

/** Supported image formats from the backend config (appsettings.json). */
const FORMATS = ["avif", "webp", "jpg"] as const;

interface DownloadDialogProps {
  /** Whether subfolders should be included (forward-looking; backend ignores). */
  recursive: boolean;
  /** Currently selected format ("" = all formats). */
  format: string;
  /** Fired when the recursive toggle changes. */
  onRecursiveChange: (recursive: boolean) => void;
  /** Fired when the format selection changes. */
  onFormatChange: (format: string) => void;
  /** Fired when the user confirms the download. */
  onDownload: () => void;
}

function FormatOption({
  format,
  label,
  checked,
  onChange,
}: {
  format: string;
  label: string;
  checked: boolean;
  onChange: (event: ChangeEvent<HTMLInputElement>) => void;
}) {
  return (
    <label className="flex items-center gap-2 text-sm">
      <input type="radio" name="format" value={format} checked={checked} onChange={onChange} />
      {label}
    </label>
  );
}

function FormatRadioGroup({
  value,
  onChange,
}: {
  value: string;
  onChange: (format: string) => void;
}) {
  const handleChange = useCallback(
    (event: ChangeEvent<HTMLInputElement>) => onChange(event.target.value),
    [onChange],
  );
  return (
    <fieldset className="flex flex-col gap-1">
      <legend className="mb-1 text-sm text-muted-foreground">Format</legend>
      {FORMATS.map((format) => (
        <FormatOption
          key={format}
          format={format}
          label={format.toUpperCase()}
          checked={value === format}
          onChange={handleChange}
        />
      ))}
      <FormatOption format="" label="All formats" checked={value === ""} onChange={handleChange} />
    </fieldset>
  );
}

function RecursiveToggle({
  checked,
  onChange,
}: {
  checked: boolean;
  onChange: (recursive: boolean) => void;
}) {
  const handleChange = useCallback(
    (event: ChangeEvent<HTMLInputElement>) => onChange(event.target.checked),
    [onChange],
  );
  return (
    <label className="flex items-center gap-2 text-sm">
      <input type="checkbox" checked={checked} onChange={handleChange} />
      Recursive (include subfolders)
    </label>
  );
}

function DownloadDialogHeader() {
  return (
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>Download folder</Dialog.DialogTitle>
      <Dialog.DialogDescription>
        Choose the image format and whether to include subfolders.
      </Dialog.DialogDescription>
    </Dialog.DialogHeader>
  );
}

function DownloadDialogBody({
  recursive,
  format,
  onRecursiveChange,
  onFormatChange,
}: {
  recursive: boolean;
  format: string;
  onRecursiveChange: (recursive: boolean) => void;
  onFormatChange: (format: string) => void;
}) {
  return (
    <div className="flex flex-col gap-3">
      <RecursiveToggle checked={recursive} onChange={onRecursiveChange} />
      <FormatRadioGroup value={format} onChange={onFormatChange} />
    </div>
  );
}

function DownloadDialogFooter({ onDownload }: { onDownload: () => void }) {
  const downloadButton = useMemo<ReactElement>(
    () => <Button variant="ghost" onClick={onDownload} />,
    [onDownload],
  );
  return (
    <Dialog.DialogFooter>
      <Dialog.DialogClose render={downloadButton}>Download</Dialog.DialogClose>
    </Dialog.DialogFooter>
  );
}

export default function DownloadDialog({
  recursive,
  format,
  onRecursiveChange,
  onFormatChange,
  onDownload,
}: DownloadDialogProps): React.JSX.Element {
  return (
    <Dialog.DialogContent>
      <DownloadDialogHeader />
      <DownloadDialogBody
        recursive={recursive}
        format={format}
        onRecursiveChange={onRecursiveChange}
        onFormatChange={onFormatChange}
      />
      <DownloadDialogFooter onDownload={onDownload} />
    </Dialog.DialogContent>
  );
}
