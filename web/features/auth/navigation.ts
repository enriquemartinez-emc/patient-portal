import type { Session } from "@/core/auth/auth.types"

export type NavItem = {
  label: string
  href: string
  // Deeper paths that still belong to this item (a detail page under a list, for example).
  activeOn: readonly string[]
}

export function homePathFor(session: Session): string {
  switch (session.kind) {
    case "patient":
      return "/patient"
    case "clinician":
      return "/clinician"
    case "researcher":
      return "/researcher"
  }
}

export function navigationFor(session: Session): readonly NavItem[] {
  switch (session.kind) {
    case "patient":
      return [
        {
          label: "Lab results",
          href: "/patient",
          activeOn: ["/patient/lab-results"],
        },
        { label: "Consents", href: "/patient/consents", activeOn: [] },
        {
          label: "Access history",
          href: "/patient/access-history",
          activeOn: [],
        },
      ]
    case "clinician":
      return [
        {
          label: "Patients",
          href: "/clinician",
          activeOn: ["/clinician/patients"],
        },
      ]
    case "researcher":
      return [
        {
          label: "Participants",
          href: "/researcher",
          activeOn: ["/researcher/participants"],
        },
      ]
  }
}

// An item is current on its own path and on the deeper paths it lists, never on a sibling's.
export function isCurrentPath(item: NavItem, pathname: string): boolean {
  return [item.href, ...item.activeOn].some(
    (path) =>
      pathname === path ||
      (path !== item.href && pathname.startsWith(`${path}/`))
  )
}
