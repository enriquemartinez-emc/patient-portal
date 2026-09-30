import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"

export function AccessDenied({ children }: { children?: React.ReactNode }) {
  return (
    <Alert variant="destructive">
      <AlertTitle>You don&apos;t have access to this</AlertTitle>
      <AlertDescription>
        {children ??
          "The records aren't shared with you, or the access has ended."}
      </AlertDescription>
    </Alert>
  )
}
