import { ThemeProvider } from '@/lib/themeContext'

export default function App(): React.JSX.Element {
  return (
    <ThemeProvider>
      <div className="flex min-h-screen items-center justify-center">
        <p className="text-foreground">ImageShare</p>
      </div>
    </ThemeProvider>
  )
}