"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"

import { isCurrentPath } from "@/core/auth/auth.rules"
import type { NavItem } from "@/core/auth/auth.types"

export function MainNav({ items }: { items: readonly NavItem[] }) {
  const pathname = usePathname()

  return (
    <nav
      aria-label="Main"
      className="flex flex-1 flex-wrap items-center gap-4 text-sm"
    >
      {items.map((item) => {
        const current = isCurrentPath(item, pathname)
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
