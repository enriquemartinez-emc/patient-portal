import { requireSessionPage } from "@/features/auth/session"

// Checked here, above this section's loading boundary, so a visitor of the wrong kind gets a real
// redirect rather than one that arrives after the page has started streaming.
export default async function PatientLayout({
  children,
}: {
  children: React.ReactNode
}) {
  await requireSessionPage("patient")
  return children
}
