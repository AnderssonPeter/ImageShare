/**
 * Sonner Toaster — the app's toast surface.
 *
 * Wraps `sonner`'s `<Toaster>` and drives its `theme` prop from the app's
 * own `ThemeProvider` (`src/lib/themeContext.tsx`) rather than `next-themes`,
 * so there is a single source of truth for light/dark. The global
 * `QueryClient` error handler (`src/lib/api/queryErrorHandler.ts`) and
 * feature code call `toast.error(...)` / `toast.success(...)` from `sonner`
 * directly; this component only needs to be mounted once, inside the
 * `ThemeProvider` (done in the root route layout).
 */
import { Toaster } from "sonner";
import { useThemeContext } from "@lib/themeContext";

/** Hoisted so the prop is a stable reference across re-renders. */
const TOAST_OPTIONS = { duration: 6000 };

export default function Sonner(): React.JSX.Element {
  const { theme } = useThemeContext();
  return (
    <Toaster
      theme={theme}
      richColors
      closeButton
      position="bottom-right"
      toastOptions={TOAST_OPTIONS}
    />
  );
}
