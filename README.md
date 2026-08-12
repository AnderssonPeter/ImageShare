# Image Share

A stateless application to share images.
It provides an image gallery and rest API.
The goal was to create a lightweight, self-contained image sharing solution that can be deployed in a containerized environment, with as few moving parts as possible.
Authentication is provided through OpenID Connect (e.g.
Pocket ID) and/or API keys, with JWT token issuance for programmatic access.

## Getting started

The application ships as a single container built from
[`ImageShare/Dockerfile`](ImageShare/Dockerfile).

### 1. Build the image

```bash
 docker buildx build . -f .\Dockerfile -t image-share:latest
```

### 2. Provide configuration

The container reads configuration from, in order of precedence (later overrides
earlier):

1. `ImageShare/appsettings.json` (baked into the image).
2. `ImageShare/appsettings.{Environment}.json`, where `{Environment}` is
   `ASPNETCORE_ENVIRONMENT` (defaults to `Production`).
3. Environment variables passed to the container.
4. Docker secret files mounted under `/run/secrets`.

See the [Settings](#settings) section for every supported key. At minimum you
must supply an OIDC authority, client id/secret, a JWT signing key (≥ 32
characters) and a storage path.

### 3. Run with Docker Compose

Create a `docker-compose.yml` next to the repository root:

```yaml
secrets:
  OpenIdConnect__ClientSecret:
    file: ./secrets/OpenIdConnect__ClientSecret
  Jwt__SigningKey:
    file: ./secrets/Jwt__SigningKey

services:
  imageshare:
    image: ghcr.io/anderssonpeter/imageshare:latest
    container_name: imageshare
    ports:
      - "8080:8080"
    environment:
      OpenIdConnect__Authority: https://your-pocket-id-domain
      OpenIdConnect__ClientId: imageshare
      OpenIdConnect__AdminRole: admin
      Jwt__Issuer: ImageShare
      Jwt__Audience: ImageShare
      Storage__BasePath: /data/images
      ReverseProxy__Enabled: "true"
      ReverseProxy__KnownProxies__0: "172.16.0.0/12"   # Docker bridge network
    volumes:
      - ./image-share/data:/data/images
    secrets:
      - OpenIdConnect__ClientSecret
      - Jwt__SigningKey
    restart: unless-stopped
```

Secret files are plain text files containing only the secret value (no quotes, no
newlines are significant — trailing whitespace is trimmed). Compose mounts each
declared secret at `/run/secrets/<secret_name>` (read-only), so name the secret
after the configuration key it should populate

Start the stack:

```bash
docker compose up -d
```

The API is then available at `https://localhost:7127/scalar`

> In production put ImageShare behind your own TLS-terminating reverse proxy and
> set `ReverseProxy__Enabled=true` together with the proxy's IP address(es) in
> `ReverseProxy__KnownProxies` so forwarded headers are honoured. The container
> runs as a non-root user (`$APP_UID`) and listens on port 8080 (HTTP) / 8081
> (HTTPS).

## Settings

All settings are bound to validated options classes, so an invalid value
prevents startup with a clear error message rather than failing at runtime.

### Configuration sources

| Source | Location | Notes |
| --- | --- | --- |
| Environment JSON | `ImageShare/appsettings.Production.json` | Selected by `ASPNETCORE_ENVIRONMENT`. |
| Environment variables | container env | Use `__` (double underscore, replace all `:` in the tables below with `__`) for nesting, e.g. `OpenIdConnect__ClientId`. Array indices are numeric, e.g. `ImageFormats__SupportedFormats__0`. |
| Docker secrets | `/run/secrets/<name>` | Same naming as the equivalent environment variable — the file's name (with `__`, replace all `:` in the tables below with `__`) maps to the configuration key. Loaded via the `Mcrio.Configuration.Provider.Docker.Secrets` provider. |

### Settings reference

#### `Logging`

| Path | Type | Default | Description |
| --- | --- | --- | --- |
| `Logging:LogLevel:Default` | string | `Information` | Default log severity. |

#### `ReverseProxy`


| Path | Type | Default | Description |
| --- | --- | --- | --- |
| `ReverseProxy:Enabled` | bool | `false` | Enables `ForwardedHeaders` handling. |
| `ReverseProxy:KnownProxies` | string[] | `[]` | Trusted proxy IP addresses (validated). |

#### `OpenIdConnect`

| Path | Type | Default | Description |
| --- | --- | --- | --- |
| `OpenIdConnect:Authority` | string | — | OIDC provider authority URL. **Required.** |
| `OpenIdConnect:ClientId` | string | — | Client id registered with the provider. **Required.** |
| `OpenIdConnect:ClientSecret` | string | — | Client secret. **Required, secret.** |
| `OpenIdConnect:ResponseType` | string | `code` | OIDC response type. |
| `OpenIdConnect:GetClaimsFromUserInfoEndpoint` | bool | `true` | Fetch extra claims from the user info endpoint. |
| `OpenIdConnect:AdminRole` | string | `admin` | Role claim value that grants admin access. |

#### `ApiKeys`

| Path | Type | Default | Description |
| --- | --- | --- | --- |
| `ApiKeys:Keys:<Name>:Key` | string | — | The API key value. **Required, secret.** Sent via the `X-API-Key` header or query parameter. |
| `ApiKeys:Keys:<Name>:Filter` | string | — | Image filter [glob](#filter-globs), e.g. `*` or `vacation/**`. **Required.** |
| `ApiKeys:Keys:<Name>:IsAdmin` | bool | `false` | Grants admin privileges to the key. |

`<Name>` is the human-friendly label used as the dictionary key — repeat it
for each key, e.g. `ApiKeys:Keys:Developer:Key`, `ApiKeys:Keys:Mobile:Key`, it will also be used as username, both in logs and in the user endpoint.

#### `Jwt`

Bound to [`JwtSettings`](ImageShare/Authentication/JwtSettings.cs). Used by
`JwtTokenIssuer`/`JwtTokenValidator` to mint and validate API tokens.

| Path | Type | Default | Description |
| --- | --- | --- | --- |
| `Jwt:Issuer` | string | — | Token issuer (`iss`). **Required.** |
| `Jwt:Audience` | string | — | Token audience (`aud`). **Required.** |
| `Jwt:SigningKey` | string | — | HMAC signing key, **must be ≥ 32 characters**. **Required, secret.** |

#### `RateLimit`
Only unauthenticated requests are rate-limited. Authenticated users (OIDC, JWT or API key) are exempt.

| Path | Type | Default | Description |
| --- | --- | --- | --- |
| `RateLimit:PermitLimit` | int | `10` | Maximum requests per window. Must be > 0. |
| `RateLimit:WindowSeconds` | int | `60` | Fixed window length in seconds. Must be > 0. |

#### `Storage`

The directory is created on startup if it does not exist.

| Path | Type | Default | Description |
| --- | --- | --- | --- |
| `Storage:BasePath` | string | `images` | Root directory for the image library. **Required.** Mount a volume here in containers. |

#### `ImageFormats`

| Path | Type | Default | Description |
| --- | --- | --- | --- |
| `ImageFormats:SupportedFormats` | string[] | `["avif","webp","jpg"]` | File extensions served/browsed. At least one required. |

#### `ImageConversion`

The background `ImageConverterJob` generates full-quality and `.thumb` variants.

| Path | Type | Default | Description |
| --- | --- | --- | --- |
| `ImageConversion:FullQuality` | uint | `95` | Quality (0–100) for full-size conversions. |
| `ImageConversion:ThumbnailQuality` | uint | `85` | Quality (0–100) for thumbnails. |
| `ImageConversion:ThumbnailMaxWidth` | int | `200` | Max thumbnail width in pixels. ≥ 1. |
| `ImageConversion:ThumbnailMaxHeight` | int | `200` | Max thumbnail height in pixels. ≥ 1. |

#### `UsageAgreement`

Agreement enforcement is only active when at least one agreement is configured.

| Path | Type | Default | Description |
| --- | --- | --- | --- |
| `UsageAgreement:Agreements:0:Language` | string | — | BCP-47 language tag, e.g. `en`, `sv`. |
| `UsageAgreement:Agreements:0:Text` | string | — | Agreement body text. |

### Recommended secret files

The file name must equal the equivalent environment variable name.

| Secret file name | Maps to | Notes |
| --- | --- | --- |
| `OpenIdConnect__ClientSecret` | `OpenIdConnect:ClientSecret` | OIDC client secret. |
| `Jwt__SigningKey` | `Jwt:SigningKey` | HMAC signing key (≥ 32 chars). |
| `ApiKeys__Keys__<Name>__Key` | `ApiKeys:Keys:<Name>:Key` | API key value for the named key. |

Non-sensitive settings (authorities, role names, limits, formats) are fine as
environment variables; only the three values above are worth treating as
secrets.

## Using API key authentication

API keys are a stateless alternative to OIDC for programmatic access. Each key
is defined under `ApiKeys:Keys:<Name>` (see the [`ApiKeys`](#apikeys) settings)
and carries a [`Filter`](#filter-globs) glob that restricts which root folders the
key can read, plus an optional `IsAdmin` flag that grants admin privileges
(Allows creation of jwt tokens).

### Authenticating a request

A key may be sent in the `X-API-Key` request header **or** the `?X-API-Key=`
query parameter. The header is preferred for real clients; the query parameter is
handy for URLs that cannot set headers (e.g. an `<img src>` or a browser address
bar).

Header (recommended):

```bash
curl -H "X-API-Key: super-secret-key" https://localhost:7127/api/content/vacation
```

Query parameter:

```bash
curl "https://localhost:7127/api/content/vacation?X-API-Key=super-secret-key"
```

## Filter globs

Each API key and JWT token carries an `image_share_filter` claim that restricts
which top-level folders the caller can see. The claim value is a glob expression
compiled by [`ImageShareFilterCompiler`](ImageShare/Authentication/ImageShareFilterCompiler.cs)
and evaluated against the **root folder** of every request (the first path
segment, before any `/`). Access to a root folder grants access to everything
beneath it; subfolders are never re-checked, so a filter cannot expose or hide
individual nested paths.

### Syntax

A filter is a `|`-separated list of patterns. Each pattern is matched against
the root folder name, case-insensitively and anchored to the whole name
(`^pattern$`).

| Token | Meaning |
| --- | --- |
| `*` | Any run of characters within the same segment — matches `[^/]*`, so it never crosses a `/`. |
| `?` | A single character within the same segment — matches `[^/]`. |
| `!` | Prefix a pattern with `!` to negate it (a deny pattern). |
| <code>\|</code> | Separates patterns. At least one allow (non-negated) pattern is required. |

There is no recursive `**` wildcard — `*` is single-segment only, and since
matching is performed on the root folder name alone, deep-path patterns are not
meaningful.

A request is allowed when the root folder matches at least one allow pattern
**and** matches no deny pattern. Deny patterns take precedence over allow
patterns.

### Examples

| Filter | Effect |
| --- | --- |
| `*` | Access to every root folder (and thus everything beneath them). |
| `vacation` | Only the `vacation` root folder. |
| <code>vacation\|public</code> | The `vacation` or `public` root folders. |
| <code>*\|!private</code> | Every root folder except `private`. |
| `pub*` | Every root folder whose name starts with `pub` (e.g. `public`, `pub_2024`). |
| `202?` | Root folders named `2024`, `2025`, etc. |

Administrators (`IsAdmin` / the configured `OpenIdConnect:AdminRole`) are not
subject to the filter — admin callers see every folder.

## AI/LLM disclaimer

The code in this repository was written with the assistance of AI tools. The
following model configuration was used:

- **deepseek v4 pro** — primary authoring assistant.
- **GLM-5.2** (`openrouter/z-ai/glm-5.2`) — secondary assistant.

Human review depth varied by area:

- **Backend (`ImageShare/`), excluding unit tests** — carefully reviewed.
- **Frontend (`frontend/`)** — lightly reviewed.
- **Unit tests (`ImageShare.Tests/`)** — glossed over only.

Treat the frontend and tests with particular scrutiny before relying on them in
production, and assume that bugs may be present despite the AI assistance.

opencode was used in conjunction with docker to sandbox it.
