/**
 * NotFound — the router's default not-found UI.
 *
 * Wired as `defaultNotFoundComponent` on `createRouter` (see `router.tsx`),
 * this renders when no route matches the URL or a route throws `notFound()`.
 * Offers a link back to the browse root.
 */
import Button from "@components/ui/Button";
import { FileQuestion } from "lucide-react";
import { Link } from "@tanstack/react-router";

const HOME_LINK_CLASS = Button.buttonVariants({ variant: "ghost", size: "sm" });
const ROOT_SPLAT_PARAMS = { _splat: undefined };

export default function NotFound(): React.JSX.Element {
  return (
    <div className="flex h-full flex-col items-center justify-center gap-3 p-8 text-center">
      <FileQuestion className="size-5 text-muted-foreground" />
      <p className="text-sm text-foreground">This page could not be found.</p>
      <Link to="/browse/$" params={ROOT_SPLAT_PARAMS} className={HOME_LINK_CLASS}>
        Go to library
      </Link>
    </div>
  );
}
