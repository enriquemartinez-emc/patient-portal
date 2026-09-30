"use client"

import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog"
import { Button } from "@/components/ui/button"

export function RevokeConsentDialog({
  consentId,
  granteeName,
  action,
}: {
  consentId: string
  granteeName: string
  action: (formData: FormData) => Promise<void>
}) {
  return (
    <AlertDialog>
      <AlertDialogTrigger render={<Button variant="outline" size="sm" />}>
        Revoke access
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Revoke access for {granteeName}?</AlertDialogTitle>
          <AlertDialogDescription>
            {granteeName} will stop being able to view your results straight
            away. You can share them again later.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Keep access</AlertDialogCancel>
          <form action={action}>
            <input type="hidden" name="consentId" value={consentId} />
            <AlertDialogAction type="submit" variant="destructive">
              Revoke access
            </AlertDialogAction>
          </form>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
