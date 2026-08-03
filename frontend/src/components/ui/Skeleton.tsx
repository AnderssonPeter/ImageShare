import { type ComponentProps } from "react";
import cn from "@lib/utils";

/**
 * Skeleton — animated placeholder sharing the Metro tile's radius token
 * (`--radius`) and `bg-muted` fill (the same color a tile adopts on hover),
 * so a skeleton tile reads as the same shape/color as a real `MetroTile`
 * waiting for its cover image.
 */
function Skeleton({ className, ...props }: ComponentProps<"div">) {
  return (
    <div
      data-slot="skeleton"
      className={cn("animate-pulse rounded-[var(--radius)] bg-muted", className)}
      {...props}
    />
  );
}

export default Skeleton;
