import { ThemeProvider, useThemeContext } from '@lib/themeContext'
import { act, renderHook } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { type ReactNode } from 'react'
import setupThemeEnvironment from '@test/themeEnvironment'

function wrapper({ children }: { children: ReactNode }) {
  return <ThemeProvider>{children}</ThemeProvider>
}

function renderThemeHook() {
  return renderHook(() => useThemeContext(), { wrapper })
}

describe('themeProvider applies the resolved system theme on mount', () => {
  it(
    'leaves the dark class absent when the system prefers light',
    () => {
      expect.assertions(2)
      // Arrange + Act
      setupThemeEnvironment(false)
      document.documentElement.classList.remove('dark')
      const { result } = renderThemeHook()

      // Assert
      expect(result.current.theme).toBe('light')
      expect(document.documentElement.classList.contains('dark')).toBe(false)
    },
    1000,
  )
})

describe('themeProvider applies a stored override on mount', () => {
  it(
    'keeps the dark class when the override is dark despite a light system',
    () => {
      expect.assertions(2)
      // Arrange + Act
      setupThemeEnvironment(false)
      document.documentElement.classList.remove('dark')
      localStorage.setItem('theme', 'dark')
      const { result } = renderThemeHook()

      // Assert
      expect(result.current.theme).toBe('dark')
      expect(document.documentElement.classList.contains('dark')).toBe(true)
    },
    1000,
  )
})

describe('themeProvider follows the system theme when no override is set', () => {
  it(
    'switches to dark when the system changes to dark',
    () => {
      expect.assertions(2)
      // Arrange
      const media = setupThemeEnvironment(false)
      document.documentElement.classList.remove('dark')
      const { result } = renderThemeHook()
      expect(result.current.theme).toBe('light')

      // Act
      act(() => {
        media.setSystemTheme(true)
      })

      // Assert
      expect(result.current.theme).toBe('dark')
    },
    1000,
  )
})

describe('themeProvider ignores system changes when an override is set', () => {
  it(
    'stays on the overridden dark theme when the system changes to light',
    () => {
      expect.assertions(1)
      // Arrange
      const media = setupThemeEnvironment(false)
      document.documentElement.classList.remove('dark')
      localStorage.setItem('theme', 'dark')
      const { result } = renderThemeHook()

      // Act
      act(() => {
        media.setSystemTheme(false)
      })

      // Assert
      expect(result.current.theme).toBe('dark')
    },
    1000,
  )
})

describe('themeProvider unsubscribes on unmount', () => {
  it(
    'removes the change listener when the provider unmounts',
    () => {
      expect.assertions(2)
      // Arrange
      const media = setupThemeEnvironment(false)
      const { unmount } = renderThemeHook()
      expect(media.listenerCount()).toBe(1)

      // Act
      unmount()

      // Assert
      expect(media.listenerCount()).toBe(0)
    },
    1000,
  )
})

describe('themeProvider setTheme persists the override and applies it', () => {
  it(
    'stores the theme and toggles the dark class',
    () => {
      expect.assertions(3)
      // Arrange
      setupThemeEnvironment(false)
      document.documentElement.classList.remove('dark')
      const { result } = renderThemeHook()

      // Act
      act(() => {
        result.current.setTheme('dark')
      })

      // Assert
      expect(result.current.theme).toBe('dark')
      expect(localStorage.getItem('theme')).toBe('dark')
      expect(document.documentElement.classList.contains('dark')).toBe(true)
    },
    1000,
  )
})

describe('themeProvider setTheme makes subsequent system changes ignored', () => {
  it(
    'keeps the overridden theme after the system flips',
    () => {
      expect.assertions(2)
      // Arrange
      const media = setupThemeEnvironment(false)
      document.documentElement.classList.remove('dark')
      const { result } = renderThemeHook()

      // Act
      act(() => {
        result.current.setTheme('dark')
      })
      act(() => {
        media.setSystemTheme(false)
      })

      // Assert
      expect(result.current.theme).toBe('dark')
      expect(document.documentElement.classList.contains('dark')).toBe(true)
    },
    1000,
  )
})

describe('themeProvider clearOverride drops the override and re-follows the system', () => {
  it(
    'returns to the system theme and removes the stored value',
    () => {
      expect.assertions(4)
      // Arrange
      setupThemeEnvironment(false)
      document.documentElement.classList.add('dark')
      localStorage.setItem('theme', 'dark')
      const { result } = renderThemeHook()
      expect(result.current.theme).toBe('dark')

      // Act
      act(() => {
        result.current.clearOverride()
      })

      // Assert
      expect(result.current.theme).toBe('light')
      expect(localStorage.getItem('theme')).toBeUndefined()
      expect(document.documentElement.classList.contains('dark')).toBe(false)
    },
    1000,
  )
})

describe('useThemeContext guards against missing provider', () => {
  it(
    'throws when rendered outside a ThemeProvider',
    () => {
      expect.assertions(1)
      // Arrange + Act + Assert
      expect(() => renderHook(() => useThemeContext())).toThrow(
        'useThemeContext must be used within a ThemeProvider',
      )
    },
    1000,
  )
})