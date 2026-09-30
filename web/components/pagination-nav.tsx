import Link from "next/link"

import { Button } from "@/components/ui/button"

export function PaginationNav({
  page,
  hasNextPage,
  hrefFor,
}: {
  page: number
  hasNextPage: boolean
  hrefFor: (page: number) => string
}) {
  if (page === 1 && !hasNextPage) {
    return null
  }

  return (
    <nav aria-label="Pagination" className="flex items-center gap-3">
      {page > 1 ? (
        <Button
          variant="outline"
          size="sm"
          render={<Link href={hrefFor(page - 1)} />}
        >
          Previous
        </Button>
      ) : (
        <Button variant="outline" size="sm" disabled>
          Previous
        </Button>
      )}
      <span className="text-sm text-muted-foreground" aria-current="page">
        Page {page}
      </span>
      {hasNextPage ? (
        <Button
          variant="outline"
          size="sm"
          render={<Link href={hrefFor(page + 1)} />}
        >
          Next
        </Button>
      ) : (
        <Button variant="outline" size="sm" disabled>
          Next
        </Button>
      )}
    </nav>
  )
}
