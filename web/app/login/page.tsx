import { notFound, redirect } from "next/navigation"

import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { homePathFor, personaLabel } from "@/core/auth/auth.rules"
import { signInAction } from "@/features/auth/actions"
import { DEMO_PASSWORD, demoAccounts } from "@/features/auth/demo-accounts"
import { getSession, isDemoAuthEnabled } from "@/features/auth/session"

export default async function LoginPage({
  searchParams,
}: {
  searchParams: Promise<{ error?: string }>
}) {
  const session = await getSession()
  if (!isDemoAuthEnabled()) {
    notFound()
  }
  if (session) {
    redirect(homePathFor(session))
  }

  const { error } = await searchParams

  return (
    <main className="mx-auto flex min-h-svh w-full max-w-md flex-col justify-center gap-6 px-6 py-12">
      <div>
        <h1 className="text-2xl font-semibold">Patient Portal</h1>
        <p className="text-sm text-muted-foreground">
          Sign in to view lab results and manage who can see them.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Sign in</CardTitle>
        </CardHeader>
        <CardContent>
          <form action={signInAction} className="flex flex-col gap-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                name="email"
                type="email"
                autoComplete="username"
                required
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                name="password"
                type="password"
                autoComplete="current-password"
                required
              />
            </div>
            {error === "invalid" && (
              <p role="alert" className="text-sm text-destructive">
                Incorrect email or password.
              </p>
            )}
            <Button type="submit">Sign in</Button>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Demo accounts</CardTitle>
          <CardDescription>
            Sample data for trying the portal. Every account uses the password{" "}
            <code className="font-mono text-foreground">{DEMO_PASSWORD}</code>.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <ul className="flex flex-col gap-2 text-sm">
            {demoAccounts.map((account) => (
              <li key={account.email} className="flex flex-col">
                <span className="font-mono">{account.email}</span>
                <span className="text-muted-foreground">
                  {account.session.name} · {personaLabel(account.session.kind)}
                </span>
              </li>
            ))}
          </ul>
        </CardContent>
      </Card>
    </main>
  )
}
