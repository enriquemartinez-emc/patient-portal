import { PortalShell } from "@/features/session/components/portal-shell"
import { requireSessionPage } from "@/shell/session"

export default async function PortalLayout({
  children,
}: {
  children: React.ReactNode
}) {
  const session = await requireSessionPage()
  return <PortalShell session={session}>{children}</PortalShell>
}
