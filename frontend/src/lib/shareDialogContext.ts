/**
 * Share-dialog context — the consumer-facing API for opening the admin share
 * dialog from anywhere.
 *
 * The provider implementation lives in `@components/ShareDialogProvider` (a UI
 * component, default-exported per the `src/components` lint rules); the
 * context + hook live here so `useShareDialog` can be a named export (named
 * exports are allowed under `src/lib`).
 */
import { createContext, useContext } from "react";

export interface ShareDialogValue {
  /** Open the share form, optionally prefilling the Return URL field. */
  openShare: (initialReturnUrl?: string) => void;
}

export const ShareDialogContext = createContext<ShareDialogValue | undefined>(undefined);

/**
 * Read the share-dialog API. Must be used within a `ShareDialogProvider`.
 * Throws if the provider is missing so misuse fails loudly rather than
 * silently no-opping.
 */
export function useShareDialog(): ShareDialogValue {
  const value = useContext(ShareDialogContext);
  if (value === undefined) {
    throw new Error("useShareDialog must be used within a ShareDialogProvider");
  }
  return value;
}
