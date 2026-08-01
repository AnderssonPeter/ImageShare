import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";

export default function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs));
}

/** Tagged template that marks a static Tailwind class string so the scanner detects it. */
export function tw(strings: TemplateStringsArray): string {
  return strings[0];
}
