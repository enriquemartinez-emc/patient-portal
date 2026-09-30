import { PortalShell } from "@/features/auth/components/portal-shell"
import { requireSessionPage } from "@/features/auth/session"

export default async function PortalLayout({
  children,
}: {
  children: React.ReactNode
}) {
  const session = await requireSessionPage()
  return <PortalShell session={session}>{children}</PortalShell>
}
