/**
 * FilterBuilder — root-folder picker for share-link filters.
 *
 * Replaces a free-text Filter input with a checkbox list: an "All folders"
 * option plus one row per root folder. The selection is encoded into the
 * backend filter grammar — `*` for all folders, `|` separator, `!` prefix
 * for denies (folders excluded when "All folders" is checked). The
 * component is fully controlled: it parses the incoming `value` string and
 * emits a new string via `onChange` whenever a checkbox toggles.
 */
import { useCallback, useMemo } from "react";
import { Ban } from "lucide-react";
import Checkbox from "@components/ui/Checkbox";
import Label from "@components/ui/Label";

const ALL_FOLDERS = "*";

interface FilterBuilderProps {
  folders: string[];
  value: string;
  error?: string;
  onChange: (filter: string) => void;
}

interface ParsedFilter {
  allFolders: boolean;
  folders: Set<string>;
}

function parseFilter(value: string): ParsedFilter {
  if (value === "") {
    return { allFolders: false, folders: new Set() };
  }
  const folders = new Set<string>();
  let allFolders = false;
  for (const part of value.split("|")) {
    if (part === ALL_FOLDERS) {
      allFolders = true;
    } else {
      folders.add(part.startsWith("!") ? part.slice(1) : part);
    }
  }
  return { allFolders, folders };
}

function buildFilter(allFolders: boolean, folders: Set<string>): string {
  const parts: string[] = [];
  if (allFolders) {
    parts.push(ALL_FOLDERS);
    for (const folder of folders) {
      parts.push(`!${folder}`);
    }
  } else {
    for (const folder of folders) {
      parts.push(folder);
    }
  }
  return parts.join("|");
}

interface AllFoldersRowProps {
  checked: boolean;
  onToggle: (checked: boolean) => void;
}

function AllFoldersRow({ checked, onToggle }: AllFoldersRowProps): React.JSX.Element {
  return <Checkbox checked={checked} onChange={onToggle} label="All folders" />;
}

interface FilterFolderRowProps {
  name: string;
  checked: boolean;
  deny: boolean;
  onToggle: (name: string, checked: boolean) => void;
}

function FilterFolderRow({
  name,
  checked,
  deny,
  onToggle,
}: FilterFolderRowProps): React.JSX.Element {
  const handleChange = useCallback((next: boolean) => onToggle(name, next), [onToggle, name]);
  const className = deny ? "text-muted-foreground" : "";
  return (
    <Checkbox checked={checked} onChange={handleChange} label={name} className={className}>
      {deny && <Ban className="size-3" />}
    </Checkbox>
  );
}

interface FilterFolderListProps {
  folders: string[];
  selected: Set<string>;
  deny: boolean;
  onToggle: (name: string, checked: boolean) => void;
}

function FilterFolderList({
  folders,
  selected,
  deny,
  onToggle,
}: FilterFolderListProps): React.JSX.Element {
  return (
    <div className="flex flex-col gap-1">
      {folders.map((name) => (
        <FilterFolderRow
          key={name}
          name={name}
          checked={selected.has(name)}
          deny={deny}
          onToggle={onToggle}
        />
      ))}
    </div>
  );
}

export default function FilterBuilder({
  folders,
  value,
  error,
  onChange,
}: FilterBuilderProps): React.JSX.Element {
  const parsed = useMemo(() => parseFilter(value), [value]);
  const handleAllFolders = useCallback(
    (checked: boolean) => onChange(buildFilter(checked, parsed.folders)),
    [onChange, parsed.folders],
  );
  const handleFolderToggle = useCallback(
    (name: string, checked: boolean) => {
      const next = new Set(parsed.folders);
      if (checked) {
        next.add(name);
      } else {
        next.delete(name);
      }
      onChange(buildFilter(parsed.allFolders, next));
    },
    [onChange, parsed.allFolders, parsed.folders],
  );
  return (
    <div className="flex flex-col gap-1">
      <Label>Filter</Label>
      <AllFoldersRow checked={parsed.allFolders} onToggle={handleAllFolders} />
      <FilterFolderList
        folders={folders}
        selected={parsed.folders}
        deny={parsed.allFolders}
        onToggle={handleFolderToggle}
      />
      {error !== undefined && <span className="text-xs text-destructive">{error}</span>}
    </div>
  );
}
