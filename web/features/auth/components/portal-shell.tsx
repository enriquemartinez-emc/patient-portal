import Link from "next/link"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import { navigationFor, personaLabel } from "@/core/auth/auth.rules"
import type { Session } from "@/core/auth/auth.types"
import { signOutAction } from "@/features/auth/actions"

export function PortalShell({
  session,
  children,
}: {
  session: Session
  children: React.ReactNode
}) {
  return (
    <div className="flex min-h-svh flex-col">
      <header className="border-b">
        <div className="mx-auto flex w-full max-w-5xl items-center gap-6 px-6 py-3">
          <span className="font-semibold">Patient Portal</span>
          <nav
            aria-label="Main"
            className="flex flex-1 items-center gap-4 text-sm"
          >
            {navigationFor(session).map((item) => (
              <Link
                key={item.href}
                href={item.href}
                className="text-muted-foreground hover:text-foreground"
              >
                {item.label}
              </Link>
            ))}
          </nav>
          <div className="flex items-center gap-3 text-sm">
            <Badge variant="secondary">{personaLabel(session.kind)}</Badge>
            <span>{session.name}</span>
            <Separator orientation="vertical" className="h-5" />
            <form action={signOutAction}>
              <Button type="submit" variant="ghost" size="sm">
                Sign out
              </Button>
            </form>
          </div>
        </div>
      </header>
      <main className="mx-auto w-full max-w-5xl flex-1 px-6 py-8">
        {children}
      </main>
    </div>
  )
}
