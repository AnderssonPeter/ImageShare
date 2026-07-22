# ImageShare — Backend Handoff for Frontend

## What it is
ImageShare is an ASP.NET Core minimal-API service that lets authenticated users browse a server-side directory tree of images, view them (in client-preferred formats with auto-generated thumbnails), download images as a zip, and get random images. It exposes an OpenAPI spec for client generation.

The frontend (`/app/frontend`) is a fresh Vite + React 19 + TypeScript scaffold with `oxlint` — currently only the starter template. Nothing image-share related has been built yet.

## Base URL & dev access
- In **Development**, the API serves Scalar at the root and the OpenAPI JSON at `/openapi.json` (`ImageShare/Program.cs:74`).
- All `/content/**` and `/user` endpoints require authorization (`ContentEndpoints.cs:11`, `AuthenticationEndpoints.cs:30`).
- Health check: `GET /` → `"pong"` (`HealthEndpoints.cs:7`).
- CORS is **not** configured. If the FE runs on a different origin during dev, ask backend to enable it (or proxy via Vite `server.proxy`).

## Authentication — three coexisting schemes
Configured in `AuthenticationExtensions.cs`. The policy scheme picks one per request:

1. **OpenID Connect (Cookie)** — interactive browser login. Endpoints:
   - `GET /login?returnUrl=...` → 302 challenge to OIDC provider (`AuthenticationEndpoints.cs:13`).
   - `GET /logout` → signs out of cookie + OIDC (`AuthenticationEndpoints.cs:21`).
   - `GET /user` → current user as `IUser` JSON (401 if not authed) (`AuthenticationEndpoints.cs:26`).
   - OIDC callback path is `/signin-oidc`; logout callback `/signout-callback-oidc`. Frontend just needs to link/navigate to `/login` and `/logout`.

2. **API key** — header **`X-API-Key`** (or same name as query param). Keys are configured server-side in `ApiKeys` settings (`appsettings.json:17`); each key carries a `name`, a `Filter`, and optional `IsAdmin`. Good for headless/automated clients.

3. **JWT (issued by this API)** — for short-lived, scoped access granted to a non-interactive client:
   - `GET /token/generate?Name=...&Filter=...&EndDate=<ISO 8601>` → returns a JWT string. Requires an authenticated **admin** (401/403) (`AuthenticationEndpoints.cs:37`, `GenerateTokenQuery.cs`).
   - `GET /login/jwt/{token}` → validates the JWT and signs the caller in via cookie, then redirects. This is the browser-friendly way to "sign in with a token" (`AuthenticationEndpoints.cs:44`, `LoginWithJwtCommand.cs`).

### Claims the FE-developer-relevant user object
`GET /user` returns `IUser` (`IUser.cs`):
```json
{ "isAuthenticated": true, "isAdmin": false, "name": "Jane" }
```
`isAdmin` is `true` only when the user has the admin role configured under `OpenIdConnect:AdminRole` (default `"admin"`).

### ImageShareFilter (authorization scoping)
Every authenticated user has an `image_share_filter` claim — a glob-like pattern (`*` = within a segment, `|` = OR, `?` = single char, case-insensitive, matches whole relative path). It determines which root folders the user can see/download. Example: `photos/*|public/*`. Folders outside the filter are silently hidden from listings and return 404 on direct access. Implemented in `ImageShareFilterCompiler.cs`.

## Path convention: `RelativePath`
All folder/file paths in the API are **relative, forward-slash-delimited** strings, never rooted, never containing `..` (`RelativePath.cs:9-18`). When sent as a path parameter they must be URL-encoded (e.g. `/` → `%2F`). The OpenAPI schema for `RelativePath` is just `type: string`. Examples:
- Root listing: `GET /content` (no path).
- Subfolder: `GET /content/photos%2F2024` or `GET /content/photos/2024` (use encodeURI on the segment).
- Thumbnail infix is a fixed string in file names — thumbnails appear as `{name}{ThumbnailInfix}.{ext}`. The FE never needs to construct these; listings hide thumbnails and show files by name without extension.

## Endpoints (all under the `ImageShare` tag in OpenAPI)

| Method & path | Query params | Returns | Notes |
|---|---|---|---|
| `GET /` | — | `text "pong"` | health |
| `GET /login` | `returnUrl` | 302 | OIDC challenge |
| `GET /logout` | — | 302 | sign out |
| `GET /user` | — | `IUser` (401) | current user |
| `GET /token/generate` | `Name`, `Filter`, `EndDate` (date-time) | `string` JWT (401/403/400) | admin only |
| `GET /login/jwt/{token}` | — | 302 redirect (400) | sign in via JWT |
| `GET /content` | `page=1`, `pageSize=50` | `PaginatedResult<FolderEntry>` (401/400/404) | list root |
| `GET /content/{path}` | `Page=1`, `PageSize=50` | `PaginatedResult<FolderEntry>` (401/400/404) | list folder |
| `GET /content/download/{folder}` | `Format` (string[], repeated) | `application/zip` stream (401/400/403/404) | recursive zip download of `Format` files |
| `GET /content/random/{folder}` | `Thumbnail` (bool), `Recursive` (bool) | image bytes (401/400/403/404/**406**) | uses `Accept` header to pick format |
| `GET /content/image/{path}` | `Thumbnail` (bool) | image bytes (401/400/403/404/**406**) | uses `Accept` header to pick format |

Note the **casing mismatch**: `/content` uses lowercase `page`/`pageSize`; `/content/{path}`, `/content/download/{folder}`, `/content/random/{folder}`, `/content/image/{path}` use PascalCase `Page`/`PageSize`/`Format`/`Thumbnail`/`Recursive`. The OpenAPI spec (`/app/ImageShare/openapi.json`) is the source of truth — generate the client from it.

### Response shapes
`FolderEntry` (`FolderEntry.cs`):
```json
{ "name": "2024", "path": "photos/2024", "type": "Folder" }   // type: "Folder" | "File"
```
`PaginatedResult<FolderEntry>` (`PaginatedResult.cs`):
```json
{ "items": [ ... ], "page": 1, "pageSize": 50, "totalCount": 137 }
```
- `pageSize` max is **500** (`GetEntriesQueryHandler.cs:14`).
- Folders are listed first, then files, each sorted ordinally (`GetEntriesQueryHandler.cs:92`).
- Empty folders are hidden; only files with a supported image extension are shown; file names are returned **without extension**.
- Root folder listing shows only folders the user's filter allows; subfolder listings show both folders and image files.

### Image serving & content negotiation
Image endpoints use the **`Accept`** header to pick a format from the server's `ImageFormats:SupportedFormats` (`appsettings.json:25`, currently `avif`, `webp`, `jpg`). If none match → **406 Not Acceptable**. Send e.g. `Accept: image/avif,image/webp`. Use `Thumbnail=true` for the 200×200 (max) version. Supported MIME: `image/avif`, `image/webp`, `image/jpeg`, `image/png` (`Program.cs:44-49`).

A background `ImageConverterJob` automatically generates all configured formats and thumbnails for every source image on startup and on filesystem changes — the FE just requests whichever format it prefers.

### Download
`GET /content/download/{folder}?Format=avif&Format=webp` streams a zip. Per the README, when only a single top-level folder is requested, the zip does **not** wrap it in a subfolder. `Format` is a repeated query param.

### Errors
All error responses use RFC 7807 **`application/problem+json`** (`ProblemDetails`): `{ type, title, status, detail, instance }`. Map on `status`:
- `400` bad request (validation, bad path, etc.)
- `401` not authenticated (redirect to `/login` for browser flows)
- `403` forbidden (e.g. folder outside filter, non-admin calling `/token/generate`)
- `404` folder/file not found — note: a forbidden folder is also reported as 404 to avoid leaking existence (`GetEntriesQueryHandler.cs:25-28`)
- `406` no acceptable image format

## Frontend stack & next steps (from README `FE` section)
Already scaffolded: Vite + React 19 + TS, `oxlint`. Outstanding FE work (per `README.md:116-123`):
1. Add MCP servers for TanStack and shadcn.
2. **Client generation** from `openapi.json` (e.g. `openapi-typescript` / `orval` / HeyApi).
3. `.editorconfig` + linting pass.
4. UI (browse folders → grid of thumbnails → full image view; admin token-generation page gated on `isAdmin`).
5. Add the FE to the backend `Dockerfile`.

Suggested flow for the UI:
- On 401, redirect to `/login?returnUrl=<current>`.
- Use `GET /user` to read `isAdmin` and gate admin features (token generation form).
- `GET /content` for root, then `GET /content/{path}` to navigate; render folders as cards using `GET /content/random/{path}?Thumbnail=true&Recursive=true` for cover images.
- `GET /content/image/{path}?Thumbnail=true` with `Accept: image/webp` for thumbnails; without `Thumbnail` and best Accept for full view.
- `GET /content/download/{path}?Format=avif&Format=webp` for "download all" buttons.

OpenAPI spec lives at `/app/ImageShare/openapi.json` and is regenerated on build — point the client generator there.
