import { signOutAction } from "@/features/auth/actions"
import { AppSidebar } from "@/features/auth/components/app-sidebar"
import type { Session } from "@/features/auth/types"
import {
  SidebarInset,
  SidebarProvider,
  SidebarTrigger,
} from "@/components/ui/sidebar"

export function PortalShell({
  session,
  defaultOpen,
  children,
}: {
  session: Session
  defaultOpen: boolean
  children: React.ReactNode
}) {
  return (
    <>
      <a
        href="#content"
        className="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 focus:z-50 focus:rounded-md focus:bg-background focus:px-3 focus:py-2"
      >
        Skip to content
      </a>
      <SidebarProvider defaultOpen={defaultOpen}>
        <AppSidebar
          kind={session.kind}
          name={session.name}
          signOutAction={signOutAction}
        />
        <SidebarInset>
          <header className="flex h-12 items-center gap-2 border-b px-4 md:px-6">
            <SidebarTrigger />
          </header>
          <main
            id="content"
            className="mx-auto w-full max-w-6xl flex-1 px-6 py-8"
          >
            {children}
          </main>
        </SidebarInset>
      </SidebarProvider>
    </>
  )
}
