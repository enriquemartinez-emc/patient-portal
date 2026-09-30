import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import type { Session } from "@/core/auth/auth.types"
import { signOutAction } from "@/features/auth/actions"
import { MainNav } from "@/features/auth/components/main-nav"
import { navigationFor } from "@/features/auth/navigation"
import { humanize } from "@/lib/format"

export function PortalShell({
  session,
  children,
}: {
  session: Session
  children: React.ReactNode
}) {
  return (
    <div className="flex min-h-svh flex-col">
      <a
        href="#content"
        className="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 focus:z-50 focus:rounded-md focus:bg-background focus:px-3 focus:py-2"
      >
        Skip to content
      </a>
      <header className="border-b">
        <div className="mx-auto flex w-full max-w-5xl flex-wrap items-center gap-x-6 gap-y-3 px-6 py-3">
          <span className="font-semibold">Patient Portal</span>
          <MainNav items={navigationFor(session)} />
          <div className="flex items-center gap-3 text-sm">
            <Badge variant="secondary">{humanize(session.kind)}</Badge>
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
      <main id="content" className="mx-auto w-full max-w-5xl flex-1 px-6 py-8">
        {children}
      </main>
    </div>
  )
}
