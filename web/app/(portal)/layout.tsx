import { cookies } from "next/headers"

import { PortalShell } from "@/features/auth/components/portal-shell"
import { requireSessionPage } from "@/features/auth/session"

export default async function PortalLayout({
  children,
}: {
  children: React.ReactNode
}) {
  const session = await requireSessionPage()
  const cookieStore = await cookies()
  const defaultOpen = cookieStore.get("sidebar_state")?.value !== "false"

  return (
    <PortalShell session={session} defaultOpen={defaultOpen}>
      {children}
    </PortalShell>
  )
}
