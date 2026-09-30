"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"

import type { NavItem } from "@/features/auth/navigation"

export function MainNav({ items }: { items: readonly NavItem[] }) {
  const pathname = usePathname()

  return (
    <nav
      aria-label="Main"
      className="flex flex-1 flex-wrap items-center gap-4 text-sm"
    >
      {items.map((item) => {
        const current =
          pathname === item.href ||
          item.activeOn.some((path) => pathname.startsWith(path))
        return (
          <Link
            key={item.href}
            href={item.href}
            aria-current={current ? "page" : undefined}
            className={
              current
                ? "font-medium text-foreground"
                : "text-muted-foreground hover:text-foreground"
            }
          >
            {item.label}
          </Link>
        )
      })}
    </nav>
  )
}
