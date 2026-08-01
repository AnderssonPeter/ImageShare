import { ThemeProvider, useThemeContext } from './themeContext'
import { act, renderHook } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { type ReactNode } from 'react'

type StorageReturn = string | undefined

interface MatchMediaResult {
  matches: boolean
  media: string
  addEventListener: ReturnType<typeof vi.fn>
  removeEventListener: ReturnType<typeof vi.fn>
}

type ChangeListener = (event: MediaQueryListEvent) => void

function createLocalStorage() {
  const store = new Map<string, string>()
  return {
    getItem: vi.fn<(key: string) => StorageReturn>((key) => store.get(key)),
    setItem: vi.fn<(key: string, value: string) => void>((key, value) => {
      store.set(key, value)
    }),
    removeItem: vi.fn<(key: string) => void>((key) => {
      store.delete(key)
    }),
    clear: vi.fn<() => void>(() => {
      store.clear()
    }),
    key: vi.fn<(index: number) => StorageReturn>((index) => [...store.keys()][index]),
    get length() {
      return store.size
    },
  }
}

function createChangeableMatchMedia(systemDark: boolean) {
  const listeners: ChangeListener[] = []
  let matches = systemDark
  const addEventListener = vi
    .fn<(event: string, listener: ChangeListener) => void>()
    .mockImplementation((event, listener) => {
      if (event === 'change') {
        listeners.push(listener)
      }
    })
  const removeEventListener = vi
    .fn<(event: string, listener: ChangeListener) => void>()
    .mockImplementation((event, listener) => {
      if (event === 'change') {
        const index = listeners.indexOf(listener)
        if (index !== -1) {
          listeners.splice(index, 1)
        }
      }
    })
  vi.stubGlobal(
    'matchMedia',
    vi.fn<(query: string) => MatchMediaResult>().mockImplementation((query: string) => ({
      matches,
      media: query,
      addEventListener,
      removeEventListener,
    })),
  )
  return {
    addEventListener,
    removeEventListener,
    setSystemTheme(isDark: boolean) {
      matches = isDark
      for (const listener of listeners) {
        listener({ matches } as MediaQueryListEvent)
      }
    },
    listenerCount: () => listeners.length,
  }
}

function setupEnvironment(systemDark: boolean) {
  vi.stubGlobal('localStorage', createLocalStorage())
  return createChangeableMatchMedia(systemDark)
}

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
      setupEnvironment(false)
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
      setupEnvironment(false)
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
      const media = setupEnvironment(false)
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
      const media = setupEnvironment(false)
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
      const media = setupEnvironment(false)
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
      setupEnvironment(false)
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
      const media = setupEnvironment(false)
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
      setupEnvironment(false)
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