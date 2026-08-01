/**
 * Browse splat route — `GET /browse`, `GET /browse/photos`, `GET /browse/photos/2024`, …
 *
 * The splat (`$`) captures everything after `/browse/` as `_splat`. Each
 * segment of the splat is a path component of a `RelativePath` (relative,
 * forward-slash-delimited, never rooted). Segments arrive already
 * URL-decoded by the router, so they are joined with `/` to reconstruct the
 * `RelativePath` for the content-listing query.
 *
 * The `loader` prefetches the first page of folder content via
 * `ensureInfiniteQueryData` so the grid has data on first paint (critical
 * data that must block navigation). The `useFolderContent` hook in the
 * component reads the same cache.
 */
import { createFileRoute } from '@tanstack/react-router'
import { folderContentQueryOptions } from '@lib/api/contentQueries'

export const Route = createFileRoute('/browse/$')({
  loader: async ({ context, params }) => {
    const segments = params._splat === undefined ? [] : params._splat.split('/')
    const relativePath = segments.join('/')
    const path = relativePath === '' ? undefined : relativePath
    await context.queryClient.ensureInfiniteQueryData(folderContentQueryOptions(path))
  },
  component: BrowseComponent,
})

function BrowseComponent(): React.JSX.Element {
  return <div className="text-foreground">Browse content grid</div>
}