# ImageShare Frontend — Todo List

Generated from `PLAN.md`. Check off items as you complete them.

---

## Phase 1 — Project setup & tooling

- [x] Install runtime dependencies: `@tanstack/react-router`, `@tanstack/router-plugin`, `@tanstack/react-query`, `@tanstack/react-virtual`, `react-zoom-pan-pinch`, `qrcode.react`
- [x] Install dev dependencies: `tailwindcss`, `@tailwindcss/vite`, `orval`
- [x] Configure oxlint with more strict rules
- [x] Add vitest
- [x] Adapt .editorconfig for typescript and react
- [x] Initialize shadcn-ui: `pnpm dlx shadcn@latest init`
- [x] Add shadcn components: `pnpm dlx shadcn@latest add button dialog carousel input label tooltip dropdown-menu sonner skeleton`
- [x] Update `vite.config.ts`: add Tailwind + Router plugins, `@` alias, hardcoded dev proxy port
- [x] Update `tsconfig.app.json` / `tsconfig.json`: add `baseUrl` and `paths` for `@/*`
- [x] Replace `src/index.css` with `@import "tailwindcss";` + shadcn theme variables
- [x] Add `gen:api` and update `build` scripts in `package.json`
- [x] Create a SVG Icon for the solution
- [x] Build a favicon based on the svg icon
- [x] Update all dependencies
- [x] Exclude generated files from git and docker
- [ ] Add an id-denylist rule with common abbreviations (val, args, prev, req, res, err, cb, fn, etc.) and rename val → value in the schema
  - `val → value` rename complete (zod/mini `refine` callbacks in `ShareLinkDialog.tsx`).
  - **Blocked**: oxlint v1.74.0 has no `id-denylist`, no `no-restricted-syntax`, and `id-match` uses Rust regex (no lookaround) so a negative-lookahead denylist is impossible. Revisit when oxlint adds one of these.
- [x] Remove orval and use @hey-api/openapi-ts instead, as it generates useQuery, useMutation, suspense, infinity queries and so on for us
- [x] Remove all cases of `as unknown as` in production code (use a runtime guard + `ensure` helper); test mocks in `src/test/setup.ts` are excluded (jsdom no-op stubs that would require fully implementing browser interfaces)

---

## Phase 2 — API client generation (orval)

- [x] Create `orval.config.ts` at repo root (input: `../ImageShare/openapi.json`, fetch mutator, TanStack Query overrides)
- [x] Implement `src/lib/api/custom-fetcher.ts` (credentials, Accept header, ProblemDetails error unwrapping)
- [x] Do we need both @testing-library/dom and @testing-library/jest-dom
- [x] Disable `import/exports-last` and place the export directly where they have been placed in the end just to fix that linting error
- [x] Implement `src/lib/api/content-queries.ts` — `useFolderContent(path?)` `useInfiniteQuery` wrapper handling page/pageSize vs Page/PageSize casing
- [x] Implement `src/lib/api/urls.ts` — `imageUrl`, `randomFolderUrl`, `downloadUrl` string builders
- [x] Run `pnpm gen:api` and verify generated client compiles

---

## Phase 3 — Metro UI design system + theming

- [x] Define light + dark theme CSS variables (accent = blue `#0078D4`, radius ~2px)
- [x] Implement theme detection: `matchMedia('(prefers-color-scheme: dark)') with fallback to dark
- [x] Add live theme-switch listener + optional localStorage override
- [x] Create `ThemeToggle` component for app bar
- [x] Create `MetroAppBar` layout (app title, breadcrumb slot, theme toggle, user chip, admin button)
- [x] Establish Metro tile base styles (flat, no shadow, 2px gutters, accent on press)
- [x] Create a showcase page that displays all components that should only be included in develop builds

---

## Phase 4 — Routing (TanStack Router, file-based)

- [x] Create `src/routes/__root.tsx` — layout shell, `beforeLoad` auth (401 → backend `/login`), expose `user` + `queryClient` via router context, theme init
- [x] Create `src/routes/index.tsx` — redirect to `/browse`
- [x] Create `src/routes/browse.$.tsx` — splat route, reconstruct `RelativePath` from segments, prefetch first page in `loader`
- [x] Create `src/routes/admin.tsx` — `beforeLoad` isAdmin gate (403 redirect)
- [x] Create `src/routes/admin/share.$token.tsx` — create link to `/api/login/jwt/{token}`
- [x] Verify `src/routeTree.gen.ts` auto-generates via Vite plugin
- [x] Wire `router.tsx` with `createRouter` + `scrollRestoration`
- [x] Do not use @/ when importing, add linting that stops this pattern, add new rows in tsconfig under paths, also don't do relative imports always use @

---

## Phase 5 — Browse grid (TanStack Query × TanStack Virtual)

- [x] Implement `ContentGrid.tsx` — flatten infinite-query pages into flat item array
- [x] Implement responsive column count (ResizeObserver / `useMeasure`)
- [x] Implement `useVirtualizer` row-based virtualization (`estimateSize` = tile + gutter)
- [x] Implement autoload: trigger `fetchNextPage()` when last visible row near end; skeleton placeholder while fetching
- [x] Implement `FolderTile` — cover from `randomFolderUrl(thumbnail, recursive)`, name overlay, navigate on click, download affordance
- [x] Implement `ImageTile` — thumbnail from `imageUrl(path, true)`, click opens carousel at index
- [x] Implement `Breadcrumb` in app bar from splat segments (clickable ancestors)
- [x] Add folder download action to the app-bar/breadcrumb menu (zip of current folder via `downloadUrl`)
- [x] Move the styling of metro tile out of the index.css and use tailwind inside the component instead
- [x] The metro tile should have a 3:2 aspect ratio, add a bit more space between metro tiles
- [x] Disallow download on the root folder

---

## Phase 6 — Fullscreen carousel

- [x] Implement `ImageViewer.tsx` — shadcn `carousel` (Embla), opens at clicked index
- [x] Add keyboard navigation (←/→ navigate, Esc close)
- [x] Use full-res `imageUrl(path, false)` for slides
- [x] Preload neighbor images for smooth swiping
- [x] Use a random full resolution image as background in the gird, in dark mode make it dark and in light mode make it light so that its almost not visible

---

## Phase 7 — Admin share (token + QR)

- [x] Implement `ShareLinkDialog.tsx` — Name, Filter, EndDate form with validation
- [x] Wire `useGenerateToken` mutation (orval) → JWT string
- [x] Instead of having a input to define the filter, there should be a builder, it should list an "all folders" option and then each folder
  - If all folders is selected, then any root folder selected is a deny and should have a deny icon (prefix the folder name with an !)
  - If the all folders is not selected then selecting a root folder is an allow
  - Separator between folders are |
  - all folders should be encoded as *
- [x] The share button should move up on hover like all other buttons
- [x] Build shareable URL `${origin}/login/jwt/${token}`
- [x] Render QR code via `qrcode.react` `<QRCodeSVG>` add logo to qrcode
- [x] Add "Copy link" + "Download QR" (SVG→PNG) actions
- [x] Handle 400/403 RFC 7807 errors in UI
- [x] Gate visibility on `user.isAdmin`

---

## Phase 8 — Error handling, loading, polish

- [x] Global QueryClient `onError`: 401 → `/login`, 404 → empty state, 403/406 → toast
- [x] Skeletons for initial grid load
  - Use shadcn skeleton component, the normal component and the skeleton should share as much variables and css as possible
  - Hide the scrollbar while rendering the skeleton
- [x] There are raw http calls in contentQueries.ts, if possible move them to use tanstack query, there should be generated code for it in the output of hey-api opents, the current solution is a ugly hack
  - Enabled the `@tanstack/react-query` hey-api plugin (generates query/infinite-query/mutation options).
  - Added a minimal oxlint override for `src/lib/api/generated/**` (3 rules: `consistent-type-definitions`, `ban-ts-comment`, `prefer-ts-expect-error`) so the generated `*Responses` stay `type` aliases (data narrows) and hey-api's `@ts-ignore` directives stay intact — both required for the generated options to compile.
  - `folderContentQueryOptions` now spreads the generated infinite-query options for `getContent`/`getContentByPath` (unified with a single `as` cast, no `as unknown as`); the manual `ensurePage` runtime guard is gone (data is statically narrow).
  - `useRootFolders` uses the generated `getContentOptions` + `select`.
  - `useShareMutation` now uses the generated `generateTokenMutation()`. Required a backend change: `generateToken` is now `POST` (was GET `GenerateTokenQuery`/`IQueryHandler` → now `GenerateTokenCommand`/`ICommandHandler` with a `[FromBody]`), so hey-api classifies it as a mutation. The `ShareFormValues` become the request body.
  - Dropped the now-unused `ensurePage`/`ensureString` guards from `guards.ts`.
  - Test mocks retargeted to `@lib/api/generated/sdk.gen` (the generated options import the SDK directly, bypassing the barrel).
- [x] Error boundaries that handles common errors like 404, 403 and so on
- [x] `sonner` toasts: download started, link copied, token generated
- [x] Review code for React-Compiler friendliness (stable refs, no unnecessary `useMemo`)
- [X] Run `pnpm lint` (oxlint) and `tsc -b`; fix any issues
- [x] Preload the random image when changing folder, before changing it
- [ ] The share dialog should show a loading animation using suspense when fetching the root folders

## Phase 9 - Extra features

- [ ] Add cast functionality to chrome cast or airplay (Create a plan first of what changes to make)
- [ ] Add support to email share url
- [ ] Create a better icon
