# ImageShare Frontend — Todo List

Generated from `PLAN.md`. Check off items as you complete them.

---

## Phase 1 — Project setup & tooling

- [X] Install runtime dependencies: `@tanstack/react-router`, `@tanstack/router-plugin`, `@tanstack/react-query`, `@tanstack/react-virtual`, `react-zoom-pan-pinch`, `qrcode.react`
- [X] Install dev dependencies: `tailwindcss`, `@tailwindcss/vite`, `orval`
- [X] Configure oxlint with more strict rules
- [X] Add vitest
- [X] Adapt .editorconfig for typescript and react
- [X] Initialize shadcn-ui: `pnpm dlx shadcn@latest init`
- [X] Add shadcn components: `pnpm dlx shadcn@latest add button dialog carousel input label tooltip dropdown-menu sonner skeleton`
- [X] Update `vite.config.ts`: add Tailwind + Router plugins, `@` alias, hardcoded dev proxy port
- [X] Update `tsconfig.app.json` / `tsconfig.json`: add `baseUrl` and `paths` for `@/*`
- [X] Replace `src/index.css` with `@import "tailwindcss";` + shadcn theme variables
- [X] Add `gen:api` and update `build` scripts in `package.json`
- [X] Create a SVG Icon for the solution
- [X] Build a favicon based on the svg icon
- [X] Update all dependencies
- [X] Exclude generated files from git and docker
---

## Phase 2 — API client generation (orval)

- [X] Create `orval.config.ts` at repo root (input: `../ImageShare/openapi.json`, fetch mutator, TanStack Query overrides)
- [X] Implement `src/lib/api/custom-fetcher.ts` (credentials, Accept header, ProblemDetails error unwrapping)
- [X] Do we need both @testing-library/dom and @testing-library/jest-dom
- [X] Disable `import/exports-last` and place the export directly where they have been placed in the end just to fix that linting error
- [X] Implement `src/lib/api/content-queries.ts` — `useFolderContent(path?)` `useInfiniteQuery` wrapper handling page/pageSize vs Page/PageSize casing
- [X] Implement `src/lib/api/urls.ts` — `imageUrl`, `randomFolderUrl`, `downloadUrl` string builders
- [X] Run `pnpm gen:api` and verify generated client compiles

---

## Phase 3 — Metro UI design system + theming

- [X] Define light + dark theme CSS variables (accent = blue `#0078D4`, radius ~2px)
- [X] Implement theme detection: `matchMedia('(prefers-color-scheme: dark)') with fallback to dark
- [X] Add live theme-switch listener + optional localStorage override
- [X] Create `ThemeToggle` component for app bar
- [X] Create `MetroAppBar` layout (app title, breadcrumb slot, theme toggle, user chip, admin button)
- [X] Establish Metro tile base styles (flat, no shadow, 2px gutters, accent on press)
- [X] Create a showcase page that displays all components that should only be included in develop builds

---

## Phase 4 — Routing (TanStack Router, file-based)

- [X] Create `src/routes/__root.tsx` — layout shell, `beforeLoad` auth (401 → backend `/login`), expose `user` + `queryClient` via router context, theme init
- [X] Create `src/routes/index.tsx` — redirect to `/browse`
- [X] Create `src/routes/browse.$.tsx` — splat route, reconstruct `RelativePath` from segments, prefetch first page in `loader`
- [X] Create `src/routes/admin.tsx` — `beforeLoad` isAdmin gate (403 redirect)
- [X] Create `src/routes/admin/share.$token.tsx` — create link to `/api/login/jwt/{token}`
- [X] Verify `src/routeTree.gen.ts` auto-generates via Vite plugin
- [X] Wire `router.tsx` with `createRouter` + `scrollRestoration`
- [X] Do not use @/ when importing, add linting that stops this pattern, add new rows in tsconfig under paths, also don't do relative imports always use @

---

## Phase 5 — Browse grid (TanStack Query × TanStack Virtual)

- [X] Implement `ContentGrid.tsx` — flatten infinite-query pages into flat item array
- [X] Implement responsive column count (ResizeObserver / `useMeasure`)
- [X] Implement `useVirtualizer` row-based virtualization (`estimateSize` = tile + gutter)
- [X] Implement autoload: trigger `fetchNextPage()` when last visible row near end; skeleton placeholder while fetching
- [ ] Implement `FolderTile` — cover from `randomFolderUrl(thumbnail, recursive)`, name overlay, navigate on click, download affordance
- [ ] Implement `ImageTile` — thumbnail from `imageUrl(path, true)`, click opens carousel at index
- [ ] Implement `Breadcrumb` in app bar from splat segments (clickable ancestors)
- [ ] Move the styling of metro tile out of the index.css and use tailwind inside the component instead
---

## Phase 6 — Fullscreen carousel

- [ ] Implement `ImageViewer.tsx` — shadcn `carousel` (Embla), opens at clicked index
- [ ] Add `react-zoom-pan-pinch` `TransformWrapper` / `TransformComponent` for pan/zoom; reset on slide change
- [ ] Add keyboard navigation (←/→ navigate, Esc close, +/- zoom)
- [ ] Use full-res `imageUrl(path, false)` for slides
- [ ] Preload neighbor images for smooth swiping

---

## Phase 7 — Admin share (token + QR)

- [ ] Implement `ShareLinkDialog.tsx` — Name, Filter, EndDate form with validation
- [ ] Wire `useGenerateToken` mutation (orval) → JWT string
- [ ] Build shareable URL `${origin}/login/jwt/${token}`
- [ ] Render QR code via `qrcode.react` `<QRCodeSVG>`
- [ ] Add "Copy link" + "Download QR" (SVG→PNG) actions
- [ ] Handle 400/403 RFC 7807 errors in UI
- [ ] Gate visibility on `user.isAdmin`

---

## Phase 8 — Error handling, loading, polish
- [ ] Global QueryClient `onError`: 401 → `/login`, 404 → empty state, 403/406 → toast
- [ ] Skeletons for initial grid load
- [ ] Empty-state tiles for folders with no images
- [ ] Per-route error boundaries
- [ ] `sonner` toasts: download started, link copied, token generated
- [ ] Review code for React-Compiler friendliness (stable refs, no unnecessary `useMemo`)
- [ ] Run `pnpm lint` (oxlint) and `tsc -b`; fix any issues
- [ ] Create a better icon

## Phase 9 - Extra features
- [ ] Add cast functionality to chrome cast or airplay
- [ ] 
