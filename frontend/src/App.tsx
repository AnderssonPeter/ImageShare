import { Suspense, lazy } from 'react'
import { ThemeProvider } from '@/lib/themeContext'

const ShowcasePage = import.meta.env.DEV
  ? lazy(() => import('@/components/ShowcasePage'))
  : undefined

const loadingFallback: React.JSX.Element = (
  <div className="flex min-h-screen items-center justify-center">
    <p className="text-foreground">Loading…</p>
  </div>
)

export default function App(): React.JSX.Element {
  if (ShowcasePage !== undefined) {
    return (
      <ThemeProvider>
        <Suspense fallback={loadingFallback}>
          <ShowcasePage />
        </Suspense>
      </ThemeProvider>
    )
  }
  return (
    <ThemeProvider>
      <div className="flex min-h-screen items-center justify-center">
        <p className="text-foreground">ImageShare</p>
      </div>
    </ThemeProvider>
  )
}
