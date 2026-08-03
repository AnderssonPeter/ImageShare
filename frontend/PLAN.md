# ImageShare Frontend — Implementation Plan

## Decisions (confirmed)

1. **Client generator:** `orval` (TanStack Query native, fetch mutator).
2. **Folder URL scheme:** TanStack Router catch-all splat → `/browse/photos/2024` (bookmarkable).
3. **Share recipient deep-linking:** if needed, implemented in the backend; FE just links to `/login/jwt/{token}`.
4. **Dev backend port:** hardcoded in the Vite proxy.
5. **Theming:** support **dark + light**; detect via `prefers-color-scheme`; fall back to **dark** when detection fails or is unavailable. Accent color = **blue** (`#0078D4` Windows 8 accent).

---

## 0. Key findings that shape the plan

- **React Compiler is already wired** (`vite.config.ts` → `babel({ presets: [reactCompilerPreset()] })`). No change needed; avoid patterns that break compiler optimizations (no manual memo where unnecessary).
- **The OpenAPI spec has NO `200` response on the binary endpoints** (`/content/image/{path}`, `/content/random/{folder}`, `/content/download/{folder}` — only `400/401/403/404/406`). A code generator can't produce typed success hooks for those.
  - **Decision:** generate typed TanStack Query hooks only for `/user`, `/content`, `/content/{path}`, `/token/generate`; consume image/zip endpoints as **direct URLs** (the browser sends a correct `Accept` header for `<img>`, and `image/avif,image/webp` is in the default image Accept).
- **Pagination param casing mismatch** (`page`/`pageSize` for `/content` vs `Page`/`PageSize` for `/content/{path}`). orval's `useInfiniteQueryParam` is global, so it can't drive both.
  - **Decision:** generate the typed request functions + `useQuery` hooks with hey-api, then write our own thin `useInfiniteQuery` wrappers around the generated fetchers — full control, no casing conflict.
- **No CORS configured.** Use a Vite `server.proxy` in dev (same-origin), and ship behind the backend in prod (handoff step 5).

---

## Phase 1 — Project setup & tooling

### 1.1 Dependencies to install

```
# routing / data / virtualization
@tanstack/react-router @tanstack/router-plugin @tanstack/react-query @tanstack/react-virtual

# styling
tailwindcss @tailwindcss/vite
pnpm dlx shadcn@latest init        # creates components.json, lib/utils, CSS vars
pnpm dlx shadcn@latest add button dialog carousel input label tooltip dropdown-menu sonner skeleton

# client generation (devDep)
orval -D

# runtime libs
react-zoom-pan-pinch               # pan/zoom in fullscreen carousel
qrcode.react                       # QR code for admin share link
```

### 1.2 Vite config

Add Tailwind + Router plugins, `@` alias, and a hardcoded dev proxy port:

```ts
import { defineConfig } from "vite";
import { resolve } from "node:path";
import react, { reactCompilerPreset } from "@vitejs/plugin-react";
import babel from "@rolldown/plugin-babel";
import tailwindcss from "@tailwindcss/vite";
import { tanstackRouter } from "@tanstack/router-plugin/vite";

const BACKEND_PORT = 5000; // hardcoded dev backend port

export default defineConfig({
  plugins: [
    tailwindcss(),
    tanstackRouter({
      routesDirectory: "./src/routes",
      generatedRouteTree: "./src/routeTree.gen.ts",
    }),
    react(),
    babel({ presets: [reactCompilerPreset()] }),
  ],
  resolve: { alias: { "@": resolve(__dirname, "src") } },
  server: {
    proxy: {
      "/content": `http://localhost:${BACKEND_PORT}`,
      "/user": `http://localhost:${BACKEND_PORT}`,
      "/login": `http://localhost:${BACKEND_PORT}`,
      "/logout": `http://localhost:${BACKEND_PORT}`,
      "/token": `http://localhost:${BACKEND_PORT}`,
    },
  },
});
```

### 1.3 tsconfig

Add `"baseUrl":"."` and `"paths":{"@/*":["./src/*"]}` to `tsconfig.app.json` (and `tsconfig.json`).

### 1.4 Tailwind + shadcn CSS

Replace `src/index.css` with:

```css
@import "tailwindcss";
```

Plus shadcn theme variables (accent = blue, full light + dark token sets — see Phase 3).

### 1.5 Scripts

Add to `package.json`:

```json
"gen:api": "orval --config orval.config.ts",
"build": "pnpm gen:api && tsc -b && vite build"
```

---

## Phase 2 — API client generation (orval)

### 2.1 `orval.config.ts` (repo root)

```ts
import { defineConfig } from "orval";

export default defineConfig({
  imageShare: {
    input: "../ImageShare/openapi.json",
    output: {
      target: "src/lib/api/generated/imageShare.ts",
      httpClient: "fetch",
      mode: "tags-split",
      baseUrl: "", // same-origin via Vite proxy
      override: {
        mutator: {
          path: "./src/lib/api/custom-fetcher.ts",
          name: "customFetcher",
        },
        query: {
          useQuery: true,
          useMutation: true,
          options: { staleTime: 30_000 },
        },
      },
    },
  },
});
```

### 2.2 Custom fetcher (`src/lib/api/custom-fetcher.ts`)

- Sets `credentials: 'include'` (cookie auth).
- Adds `Accept` header for image endpoints (`image/avif,image/webp,image/png`).
- Unwraps RFC 7807 `ProblemDetails` into thrown errors carrying `status` (enables 401→login redirect).
- Returns the parsed JSON / blob / text depending on `Content-Type`.

### 2.3 Manual `useInfiniteQuery` wrappers (`src/lib/api/content-queries.ts`)

`useFolderContent(path?: string)`:

- Calls generated `getContent` (root) or `getContentPath` (subfolder) with the **correct page param casing** per endpoint (`page`/`pageSize` vs `Page`/`PageSize`).
- `getNextPageParam: last => (last.page * last.pageSize < last.totalCount) ? last.page + 1 : undefined`.
- `queryKey`: `['content', path ?? 'root']`.

### 2.4 Image URL helpers (`src/lib/api/urls.ts`)

Pure string builders used by `<img src>` / `<a href download>`:

- `imageUrl(path, thumbnail)` → `/content/image/{encodedPath}?Thumbnail={bool}`
- `randomFolderUrl(folder, thumbnail, recursive)` → `/content/random/{encodedFolder}?Thumbnail={bool}&Recursive={bool}`
- `downloadUrl(folder, formats[])` → `/content/download/{encodedFolder}?Format=avif&Format=webp`

---

## Phase 3 — Metro UI design system + theming

Windows 8 / Windows Phone 8 "Metro" aesthetic, layered onto shadcn primitives.

### 3.1 Theme detection & tokens

- Read `window.matchMedia('(prefers-color-scheme: dark)')`:
  - If `matchMedia` is unsupported or throws → default to **dark**.
  - Otherwise use `matches ? dark : light`, and listen for changes (live theme switch).
- Persist explicit override in `localStorage` (optional manual toggle in app bar).
- shadcn theme class strategy: `dark` class on `<html>` toggled; default (no class) = light. Add `dark` class when dark.

### 3.2 Accent color (blue)

CSS variables (both themes), Windows 8 blue accent `#0078D4`:

```css
:root,
.light {
  --background: 0 0% 100%;
  --foreground: 0 0% 13%;
  --accent: 202 90% 50%; /* #0078D4 */
  --accent-foreground: 0 0% 100%;
  --radius: 2px;
}
.dark {
  --background: 0 0% 13%;
  --foreground: 0 0% 100%;
  --accent: 202 90% 55%;
  --accent-foreground: 0 0% 100%;
  --radius: 2px;
}
```

### 3.3 Metro visual rules

- **Typography:** `'Segoe UI', system-ui` (scaffold default) — bold, tight tracking, sentence-case headers.
- **Tiles, not cards:** near-zero radius (2–4px), **no box-shadow**, **flat fills**, **2px gutters** (the iconic Metro grid spacing). Override shadcn `--radius` to ~2px.
- **Flat fills, no gradients, no skeuomorphism.**
- **Tile content silhouettes:** title bottom-left, iconography top-right (mirroring Win8 live tiles). Folder tiles show the random cover image full-bleed with a bottom gradient + name overlay.
- **Hover:** subtle lighten of the tile. **Press:** full accent fill (Metro button behavior).
- **Header:** flat app bar (app title left, breadcrumb center, user chip + "Share"/Admin button right).

### 3.4 Layout shell

- `__root.tsx` renders `MetroAppBar` (app title, breadcrumb, theme toggle, user chip, admin Share button) + `<Outlet/>` + `Toaster`.
- No max-width container — full-bleed grid (Metro panorama feel).
- Optional: horizontal "panorama" strip of recent folders (WinPhone signature); core browse view is a responsive **grid**.

---

## Phase 4 — Routing (TanStack Router, file-based)

```
src/routes/
  __root.tsx          # layout: MetroAppBar, <Outlet/>, Toaster; beforeLoad auth + theme init
  index.tsx           # redirect → /browse
  browse.$.tsx        # SPLAT route → /browse/photos/2024 ; params._splat → 'photos/2024'
  share.$token.tsx    # /share/:token  (nice URL → triggers /login/jwt/{token} via backend)
  admin.tsx           # gated: beforeLoad checks isAdmin → 403 redirect
```

### 4.1 Auth in `beforeLoad` (`__root.tsx`)

- Call generated `getUser()` (orval) via the provided `queryClient`.
- If 401 → full browser navigation to `${API}/login?returnUrl=${encodeURIComponent(location.href)}` (OIDC cookie flow; not an in-app route).
- Expose `user` + `queryClient` via router context (`routerContext`).
- Theme initialization: read `prefers-color-scheme` (fallback dark), apply `dark` class.

### 4.2 `browse.$.tsx`

- Splat captures arbitrary folder depth; reconstruct the `RelativePath` by joining segments with `/`.
- `loader` prefetches the first page via `queryClient.ensureQueryData` so the grid renders immediately on navigation.

### 4.3 Route tree

Auto-generated to `src/routeTree.gen.ts` by the Vite plugin; committed.

---

## Phase 5 — Browse grid (TanStack Query × TanStack Virtual)

`src/features/browse/FolderGrid.tsx`:

- **`useInfiniteQuery`** (`useFolderContent(path)`) → `data.pages` each a `PaginatedResult<FolderEntry>` (pageSize default 50, max 500).
- **Flatten** all pages' `items` into one flat array (folders first, then files — server already sorts).
- **Virtual grid via `useVirtualizer`:**
  - Compute `columns` from container width (ResizeObserver / `useMeasure`), `rowCount = ceil(items.length / columns)`, virtualize **rows** where each row renders `columns` tiles.
  - `estimateSize` = tile size + gutter.
- **Autoload:** read `virtualizer.getVirtualItems()`; when the last visible row is within N rows of the end and `hasNextPage`, call `fetchNextPage()`. Render a `<Skeleton/>` tile-grid placeholder while `isFetchingNextPage`.

### 5.1 Tile rendering

- **Folder tile:**
  - Cover `<img src={randomFolderUrl(path, true, true)} loading="lazy">` (thumbnail + recursive random image).
  - Name overlay (bottom gradient + name).
  - Click → `navigate({ to: '/browse/$', params: { _splat: path } })`.
  - Hover "Download" affordance → `downloadUrl(path, ['avif','webp'])`.
- **File tile:**
  - `<img src={imageUrl(path, true)} loading="lazy">` (thumbnail).
  - Click → open carousel at this image's index.

### 5.2 Breadcrumb

Derived from `splat` segments in `MetroAppBar`; clickable ancestors navigate up the tree.

---

## Phase 6 — Fullscreen carousel

`src/features/viewer/ImageViewer.tsx` (route-level overlay or controlled `<Dialog>`):

- **shadcn `carousel`** (Embla) for slide navigation; opens at the clicked image's index.
- **Pan/zoom/move** via `react-zoom-pan-pinch` `<TransformWrapper>` / `<TransformComponent>`:
  - Drag to pan, pinch/wheel to zoom.
  - Reset transform on slide change.
- **Keyboard:** ← / → navigate, Esc close, zoom controls (+/−).
- **Full image src:** `imageUrl(path, false)` (no thumbnail) — browser negotiates best format via Accept.
- Preload neighbors for smooth swiping.
- Embla handles swipe; the pan library handles zoom-drag.

---

## Phase 7 — Admin share (token + QR)

`src/features/admin/ShareLinkDialog.tsx`, shown only when `user.isAdmin` (button in header):

- **Form fields:**
  - **Name** (string, required)
  - **Filter** (glob, e.g. `photos/*`, required)
  - **EndDate** (datetime-local, must be in the future)
- **Submit:** generated `useGenerateToken` mutation (orval) → returns JWT string.
- **Shareable URL:** `${origin}/login/jwt/${token}` (backend signs the recipient in via cookie, then redirects).
- **Surfaces:**
  - Copyable text input + **QR code** (`qrcode.react` `<QRCodeSVG value={url} />`)
  - "Download QR" (serialize SVG → PNG)
  - "Copy link" button
- **Validation:** required fields, EndDate in the future; show RFC 7807 `detail` on 400/403.

---

## Phase 8 — Error handling, loading, polish

- Global **QueryClient** `onError`:
  - 401 → redirect to `/login` (backend OIDC).
  - 404 → "folder empty / not found" tile state.
  - 403 / 406 → toast.
- **Skeletons** for initial load; **empty states** (folder has no images); **error boundaries** per route.
- `sonner` toasts for download started, link copied, token generated.
- Keep React-Compiler-friendly code (stable refs, no unnecessary `useMemo`).
- `pnpm lint` (oxlint) + `tsc -b` in CI.

---

## Proposed file tree (feature-based)

```
src/
  lib/
    api/
      generated/            # orval output (committed)
      custom-fetcher.ts
      content-queries.ts   # useInfiniteQuery wrappers
      urls.ts              # image/random/download URL builders
    utils.ts               # shadcn cn()
  components/ui/            # shadcn primitives
  routes/                  # TanStack Router file-based (Phase 4)
  features/
    auth/                  # useUser, AuthGate, UserChip
    browse/                # FolderGrid, Tile, FolderTile, ImageTile, Breadcrumb
    viewer/                # ImageViewer carousel
    admin/                 # ShareLinkDialog, TokenForm
    layout/                # MetroAppBar, theme tokens, ThemeToggle
  router.tsx
  main.tsx
  index.css
```

---

## Phase summary

| #   | Phase                            | Outcome                                                         |
| --- | -------------------------------- | --------------------------------------------------------------- |
| 1   | Project setup & tooling          | Tailwind, Router plugin, alias, proxy, deps                     |
| 2   | API client generation            | orval → typed TanStack Query hooks + URL builders               |
| 3   | Metro UI design system + theming | blue accent, light/dark auto-detect (fallback dark), flat tiles |
| 4   | Routing                          | file-based splat routes, beforeLoad auth, theme init            |
| 5   | Browse grid                      | infinite query × virtual rows, autoload, folder/image tiles     |
| 6   | Fullscreen carousel              | Embla + zoom/pan, keyboard nav, neighbor preload                |
| 7   | Admin share                      | token form + QR + copy link, isAdmin-gated                      |
| 8   | Errors / loading / polish        | global error handlers, skeletons, toasts, CI lint+typecheck     |
