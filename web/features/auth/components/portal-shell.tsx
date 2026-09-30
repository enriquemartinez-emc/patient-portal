import Link from "next/link"

import { signOutAction } from "@/features/auth/actions"
import { MainNav } from "@/features/auth/components/main-nav"
import { UserMenu } from "@/features/auth/components/user-menu"
import { navigation } from "@/features/auth/navigation"
import type { Session } from "@/features/auth/types"

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
        <div className="mx-auto flex w-full max-w-6xl flex-wrap items-center gap-x-6 gap-y-2 px-6 py-3">
          <Link href={`/${session.kind}`} className="font-semibold">
            Patient Portal
          </Link>
          <div className="ml-auto sm:order-last">
            <UserMenu
              name={session.name}
              role={session.kind}
              signOutAction={signOutAction}
            />
          </div>
          <MainNav items={navigation[session.kind]} />
        </div>
      </header>
      <main id="content" className="mx-auto w-full max-w-6xl flex-1 px-6 py-8">
        {children}
      </main>
    </div>
  )
}
