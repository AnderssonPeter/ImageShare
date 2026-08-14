import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";

export default function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs));
}

/** Tagged template that marks a static Tailwind class string so the scanner detects it. */
export function tw(strings: TemplateStringsArray): string {
  return strings[0];
}

/** Splits an array into fixed-size chunks (rows). */
export function chunk<TItem>(items: TItem[], size: number): TItem[][] {
  const rows: TItem[][] = [];
  for (let index = 0; index < items.length; index += size) {
    rows.push(items.slice(index, index + size));
  }
  return rows;
}
