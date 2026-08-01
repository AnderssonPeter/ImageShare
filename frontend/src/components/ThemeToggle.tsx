/**
 * ThemeToggle — app-bar control that switches between light and dark mode.
 *
 * A single icon button reflecting the effective theme: a sun when light, a
 * moon when dark. Clicking pins the opposite theme as an explicit override.
 *
 * The effective theme comes from the ThemeProvider context, which resolves
 * an explicit override > system preference > default. Clicking always pins
 * an override; the toggle does not expose a "follow system" mode.
 */
import { Moon, Sun } from 'lucide-react'
import Button from '@components/ui/Button'
import Tooltip from '@components/ui/Tooltip'
import { useCallback } from 'react'
import { useThemeContext } from '@lib/themeContext'

interface ThemeToggleTriggerProps {
  isDark: boolean
  onToggle: () => void
}

function ThemeToggleTrigger({ isDark, onToggle }: ThemeToggleTriggerProps) {
  const Icon = isDark ? Moon : Sun
  return (
    <Tooltip.TooltipTrigger
      className={Button.buttonVariants({ variant: 'ghost', size: 'icon' })}
      onClick={onToggle}
      aria-label={isDark ? 'Switch to light theme' : 'Switch to dark theme'}
    >
      <Icon className="size-4" />
    </Tooltip.TooltipTrigger>
  )
}

export default function ThemeToggle(): React.JSX.Element {
  const { theme, setTheme } = useThemeContext()
  const isDark = theme === 'dark'

  const handleToggle = useCallback(() => {
    setTheme(isDark ? 'light' : 'dark')
  }, [isDark, setTheme])

  return (
    <Tooltip.Tooltip>
      <ThemeToggleTrigger isDark={isDark} onToggle={handleToggle} />
      <Tooltip.TooltipContent>
        {isDark ? 'Light theme' : 'Dark theme'}
      </Tooltip.TooltipContent>
    </Tooltip.Tooltip>
  )
}
