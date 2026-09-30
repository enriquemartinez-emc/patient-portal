import type { SessionKind } from "@/features/auth/types"

export type NavItem = {
  label: string
  href: string
  // Deeper paths that still belong to this item (a detail page under a list, for example).
  activeOn: readonly string[]
}

export const navigation: Record<SessionKind, readonly NavItem[]> = {
  patient: [
    { label: "Dashboard", href: "/patient", activeOn: [] },
    {
      label: "Lab results",
      href: "/patient/lab-results",
      activeOn: ["/patient/lab-results"],
    },
    { label: "Consents", href: "/patient/consents", activeOn: [] },
    { label: "Access history", href: "/patient/access-history", activeOn: [] },
  ],
  clinician: [
    {
      label: "Patients",
      href: "/clinician",
      activeOn: ["/clinician/patients"],
    },
  ],
  researcher: [
    {
      label: "Participants",
      href: "/researcher",
      activeOn: ["/researcher/participants"],
    },
  ],
}
