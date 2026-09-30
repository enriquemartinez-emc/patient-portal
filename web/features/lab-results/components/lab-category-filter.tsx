import Link from "next/link"

import { buttonVariants } from "@/components/ui/button"
import { groupByCategory } from "@/features/lab-results/summary"
import type { LabCategory, LabResult } from "@/features/lab-results/types"
import { cn } from "@/lib/utils"

export function LabCategoryFilter({
  results,
  selected,
  basePath,
}: {
  results: readonly LabResult[]
  selected: LabCategory | null
  basePath: string
}) {
  const groups = groupByCategory(results)

  if (groups.length < 2) {
    return null
  }

  return (
    <nav aria-label="Filter by category">
      <div className="flex flex-wrap gap-2">
        <Link
          href={basePath}
          aria-current={selected === null ? "page" : undefined}
          className={cn(
            buttonVariants({
              variant: selected === null ? "default" : "outline",
              size: "sm",
            })
          )}
        >
          All
          <span className="tabular-nums opacity-75">{results.length}</span>
        </Link>

        {groups.map((group) => (
          <Link
            key={group.category}
            href={`${basePath}?category=${group.category}`}
            aria-current={selected === group.category ? "page" : undefined}
            className={cn(
              buttonVariants({
                variant: selected === group.category ? "default" : "outline",
                size: "sm",
              })
            )}
            style={
              { "--lab": `var(--lab-${group.category})` } as React.CSSProperties
            }
          >
            <span className="size-2 rounded-full bg-(--lab)" aria-hidden />
            <span className="capitalize">{group.category}</span>
            <span className="tabular-nums opacity-75">
              {group.results.length}
            </span>
          </Link>
        ))}
      </div>
    </nav>
  )
}
