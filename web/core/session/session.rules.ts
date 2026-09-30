import type { NavItem, Session, SessionKind } from "./session.types"

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

export function personaLabel(kind: SessionKind): string {
  switch (kind) {
    case "patient":
      return "Patient"
    case "clinician":
      return "Clinician"
    case "researcher":
      return "Researcher"
  }
}

// Each persona's navigation. Feature pages add their entries here as they are built.
export function navigationFor(session: Session): readonly NavItem[] {
  return [{ label: "Overview", href: homePathFor(session) }]
}
