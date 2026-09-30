"use client"

import * as React from "react"

type Theme = "light" | "dark" | "system"

const STORAGE_KEY = "theme"

const ThemeContext = React.createContext<{
  theme: Theme
  resolvedTheme: "light" | "dark"
  setTheme: (theme: Theme) => void
} | null>(null)

const listeners = new Set<() => void>()

function subscribe(listener: () => void) {
  listeners.add(listener)
  window.addEventListener("storage", listener)
  return () => {
    listeners.delete(listener)
    window.removeEventListener("storage", listener)
  }
}

function getStoredTheme(): Theme {
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    return stored === "light" || stored === "dark" ? stored : "system"
  } catch {
    return "system"
  }
}

function subscribeToSystemTheme(listener: () => void) {
  const query = window.matchMedia("(prefers-color-scheme: dark)")
  query.addEventListener("change", listener)
  return () => query.removeEventListener("change", listener)
}

function getSystemDark() {
  return window.matchMedia("(prefers-color-scheme: dark)").matches
}

function setTheme(next: Theme) {
  try {
    localStorage.setItem(STORAGE_KEY, next)
  } catch {
    // Storage can be blocked. The choice then only lasts until the page reloads.
  }
  listeners.forEach((listener) => listener())
}

function ThemeProvider({ children }: { children: React.ReactNode }) {
  // The server renders "system"/light. The browser values take over after hydration.
  const theme = React.useSyncExternalStore<Theme>(
    subscribe,
    getStoredTheme,
    () => "system"
  )
  const systemDark = React.useSyncExternalStore(
    subscribeToSystemTheme,
    getSystemDark,
    () => false
  )

  const resolvedTheme =
    theme === "system" ? (systemDark ? "dark" : "light") : theme

  React.useEffect(() => {
    document.documentElement.classList.toggle("dark", resolvedTheme === "dark")
  }, [resolvedTheme])

  const value = React.useMemo(
    () => ({ theme, resolvedTheme, setTheme }),
    [theme, resolvedTheme]
  )

  return (
    <ThemeContext.Provider value={value}>
      <ThemeHotkey />
      {children}
    </ThemeContext.Provider>
  )
}

function useTheme() {
  const context = React.useContext(ThemeContext)
  if (!context) {
    throw new Error("useTheme must be used inside ThemeProvider")
  }
  return context
}

function isTypingTarget(target: EventTarget | null) {
  if (!(target instanceof HTMLElement)) {
    return false
  }

  return (
    target.isContentEditable ||
    target.tagName === "INPUT" ||
    target.tagName === "TEXTAREA" ||
    target.tagName === "SELECT"
  )
}

function ThemeHotkey() {
  const { resolvedTheme, setTheme } = useTheme()

  React.useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.defaultPrevented || event.repeat) {
        return
      }

      if (event.metaKey || event.ctrlKey || event.altKey) {
        return
      }

      if (event.key.toLowerCase() !== "d") {
        return
      }

      if (isTypingTarget(event.target)) {
        return
      }

      setTheme(resolvedTheme === "dark" ? "light" : "dark")
    }

    window.addEventListener("keydown", onKeyDown)

    return () => {
      window.removeEventListener("keydown", onKeyDown)
    }
  }, [resolvedTheme, setTheme])

  return null
}

export { ThemeProvider, useTheme }
export type { Theme }
