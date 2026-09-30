import Link from "next/link"

import { Badge } from "@/components/ui/badge"
import {
  Card,
  CardAction,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import type { ActiveConsent } from "@/features/consents/types"
import { formatDate } from "@/lib/format"

export function SharingCard({
  consents,
  className,
}: {
  consents: readonly ActiveConsent[]
  className?: string
}) {
  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle>Who can see your results</CardTitle>
        <CardAction>
          <Link
            href="/patient/consents"
            className="text-sm text-muted-foreground hover:text-foreground"
          >
            Manage
          </Link>
        </CardAction>
      </CardHeader>
      <CardContent>
        {consents.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            You aren&apos;t sharing your results with anyone.{" "}
            <Link
              href="/patient/consents"
              className="text-foreground underline underline-offset-4"
            >
              Share results
            </Link>
          </p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Shared with</TableHead>
                <TableHead>Results</TableHead>
                <TableHead>Until</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {consents.map((consent) => (
                <TableRow key={consent.id}>
                  <TableCell className="font-medium whitespace-normal">
                    {consent.granteeName}
                  </TableCell>
                  <TableCell className="whitespace-normal">
                    <div className="flex flex-wrap gap-1">
                      {consent.categories.map((category) => (
                        <Badge
                          key={category}
                          variant="secondary"
                          className="capitalize"
                        >
                          {category}
                        </Badge>
                      ))}
                    </div>
                  </TableCell>
                  <TableCell className="whitespace-normal">
                    {consent.expiry.kind === "on"
                      ? formatDate(consent.expiry.at)
                      : "No end date"}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </CardContent>
    </Card>
  )
}
