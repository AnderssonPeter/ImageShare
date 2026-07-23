import { defineConfig } from 'orval'

/**
 * Orval configuration — generates a typed TanStack Query client from the
 * ImageShare OpenAPI spec.
 *
 * - httpClient: 'fetch'  -> native fetch via a custom mutator (credentials,
 *   Accept header, RFC 7807 ProblemDetails unwrapping).
 * - client: 'react-query' -> generates TanStack Query hooks (useQuery /
 *   useMutation). Infinite hooks are NOT auto-generated because the API has a
 *   pagination param casing mismatch (page/pageSize vs Page/PageSize); we write
 *   manual useInfiniteQuery wrappers in src/lib/api/content-queries.ts instead.
 */
export default defineConfig({
  imageShare: {
    input: '../ImageShare/openapi.json',
    output: {
      target: 'src/lib/api/generated/imageShare.ts',
      httpClient: 'fetch',
      client: 'react-query',
      baseUrl: '',
      clean: true,
      prettier: false,
      mock: false,
      override: {
        mutator: {
          path: './src/lib/api/custom-fetcher.ts',
          name: 'customFetcher',
        },
        query: {
          useQuery: true,
          useMutation: true,
          useInfinite: false,
          signal: true,
        },
      },
    },
    hooks: {
      afterAllFilesWrite: 'oxlint --fix',
    },
  },
})
