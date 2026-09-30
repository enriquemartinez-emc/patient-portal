import { redirect } from "next/navigation"

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { signInAction, signOutAction } from "@/features/auth/actions"
import { DEMO_PASSWORD, demoAccounts } from "@/features/auth/demo-accounts"
import { getAuthSession, getSession } from "@/features/auth/session"

export const metadata = { title: "Sign in · Patient Portal" }

export default async function LoginPage() {
  const session = await getSession()
  if (session) {
    redirect(`/${session.kind}`)
  }
  const unlinked = (await getAuthSession()) !== null

  return (
    <main className="mx-auto flex min-h-svh w-full max-w-md flex-col justify-center gap-6 px-6 py-12">
      <div>
        <h1 className="text-2xl font-semibold">Patient Portal</h1>
        <p className="text-sm text-muted-foreground">
          Sign in to view lab results and manage who can see them.
        </p>
      </div>

      {unlinked ? (
        <Alert variant="destructive">
          <AlertTitle>This account has no portal access</AlertTitle>
          <AlertDescription className="flex flex-col items-start gap-3">
            <span>
              You are signed in, but the account is not linked to a patient,
              clinician or researcher record.
            </span>
            <form action={signOutAction}>
              <Button type="submit" variant="outline" size="sm">
                Log out
              </Button>
            </form>
          </AlertDescription>
        </Alert>
      ) : (
        <Card>
          <CardHeader>
            <CardTitle>Sign in</CardTitle>
            <CardDescription>
              You will sign in on the Keycloak page and come back here.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form action={signInAction}>
              <Button type="submit" className="w-full">
                Sign in
              </Button>
            </form>
          </CardContent>
        </Card>
      )}

      {process.env.SHOW_DEMO_ACCOUNTS === "true" && (
        <Card>
          <CardHeader>
            <CardTitle>Demo accounts</CardTitle>
            <CardDescription>
              Sample data for trying the portal. Every account uses the password{" "}
              <code className="font-mono text-foreground">{DEMO_PASSWORD}</code>
              .
            </CardDescription>
          </CardHeader>
          <CardContent>
            <ul className="flex flex-col gap-2 text-sm">
              {demoAccounts.map((account) => (
                <li key={account.email} className="flex flex-col">
                  <span className="font-mono">{account.email}</span>
                  <span className="text-muted-foreground">
                    {account.name} ·{" "}
                    <span className="capitalize">{account.role}</span>
                  </span>
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>
      )}
    </main>
  )
}
