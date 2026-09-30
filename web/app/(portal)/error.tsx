"use client"

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"

export default function PortalError({
  reset,
}: {
  error: Error & { digest?: string }
  reset: () => void
}) {
  return (
    <div className="flex flex-col items-start gap-4">
      <Alert variant="destructive">
        <AlertTitle>Something went wrong</AlertTitle>
        <AlertDescription>
          We couldn&apos;t load this page. Your records have not been changed.
        </AlertDescription>
      </Alert>
      <Button variant="outline" onClick={reset}>
        Try again
      </Button>
    </div>
  )
}
