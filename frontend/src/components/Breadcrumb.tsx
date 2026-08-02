/**
 * Breadcrumb — the folder path trail shown in the app bar.
 *
 * Builds a clickable ancestor trail from a RelativePath (relative,
 * forward-slash-delimited). A root crumb links to the browse root; each
 * ancestor segment links to its cumulative path; the final (current)
 * segment is plain text. Uses TanStack Router `<Link>` so ancestor crumbs
 * are real anchors (keyboard + middle-click friendly).
 */
import { ChevronRight, Home } from "lucide-react";
import { Link } from "@tanstack/react-router";
import { type ReactNode } from "react";
import { tw } from "@lib/utils";

const NAV_CLASS = tw`flex min-w-0 items-center gap-1 text-sm`;
const CRUMB_LINK_CLASS = tw`flex min-w-0 items-center text-muted-foreground hover:text-foreground`;
const CRUMB_CURRENT_CLASS = tw`truncate font-medium text-foreground`;
const NAME_CLASS = tw`truncate`;
const SEP_CLASS = tw`size-3 shrink-0 text-muted-foreground/60`;

interface BreadcrumbProps {
  /** Current folder RelativePath (undefined or empty = root listing). */
  path?: string;
}

interface Crumb {
  name: string;
  params: { _splat: string | undefined };
  current: boolean;
}

/** Build the crumb trail: a root crumb followed by one crumb per path segment. */
function buildCrumbs(path: string | undefined): Crumb[] {
  const segments =
    path === undefined || path === "" ? [] : path.split("/").filter((segment) => segment !== "");
  const crumbs: Crumb[] = [
    { name: "Home", params: { _splat: undefined }, current: segments.length === 0 },
  ];
  let cumulative = "";
  for (let index = 0; index < segments.length; index++) {
    const segment = segments[index];
    cumulative = cumulative === "" ? segment : `${cumulative}/${segment}`;
    const isLast = index === segments.length - 1;
    crumbs.push({ name: segment, params: { _splat: isLast ? undefined : cumulative }, current: isLast });
  }
  return crumbs;
}

function renderCrumb(crumb: Crumb, index: number): ReactNode {
  if (crumb.current) {
    return (
      <span key={index} className={CRUMB_CURRENT_CLASS} aria-current="page">
        {crumb.name}
      </span>
    );
  }
  return (
    <Link key={index} to="/browse/$" params={crumb.params} className={CRUMB_LINK_CLASS}>
      {crumb.name === "Home" ? (
        <Home className="size-4 shrink-0" />
      ) : (
        <span className={NAME_CLASS}>{crumb.name}</span>
      )}
    </Link>
  );
}

export default function Breadcrumb({ path }: BreadcrumbProps): React.JSX.Element {
  const crumbs = buildCrumbs(path);
  const items: ReactNode[] = [];
  for (let index = 0; index < crumbs.length; index++) {
    if (index > 0) {
      items.push(<ChevronRight key={`sep-${index}`} className={SEP_CLASS} />);
    }
    items.push(renderCrumb(crumbs[index], index));
  }
  return <nav className={NAV_CLASS}>{items}</nav>;
}
