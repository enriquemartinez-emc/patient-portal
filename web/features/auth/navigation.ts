import {
  type LucideIcon,
  LayoutDashboard,
  FlaskConical,
  ShieldCheck,
  History,
  Users,
} from "lucide-react"

import type { SessionKind } from "@/features/auth/types"

export type NavItem = {
  label: string
  href: string
  icon: LucideIcon
  // Deeper paths that still belong to this item (a detail page under a list, for example).
  activeOn: readonly string[]
}

export const navigation: Record<SessionKind, readonly NavItem[]> = {
  patient: [
    {
      label: "Dashboard",
      href: "/patient",
      icon: LayoutDashboard,
      activeOn: [],
    },
    {
      label: "Lab results",
      href: "/patient/lab-results",
      icon: FlaskConical,
      activeOn: ["/patient/lab-results"],
    },
    {
      label: "Consents",
      href: "/patient/consents",
      icon: ShieldCheck,
      activeOn: [],
    },
    {
      label: "Access history",
      href: "/patient/access-history",
      icon: History,
      activeOn: [],
    },
  ],
  clinician: [
    {
      label: "Patients",
      href: "/clinician",
      icon: Users,
      activeOn: ["/clinician/patients"],
    },
  ],
  researcher: [
    {
      label: "Participants",
      href: "/researcher",
      icon: Users,
      activeOn: ["/researcher/participants"],
    },
  ],
}
