import type { LabCategory } from "@/core/lab-results/lab-results.types"

export type ConsentExpiry = { kind: "never" } | { kind: "on"; at: string }

type ConsentDetails = {
  id: string
  granteeOrganizationId: string
  granteeName: string
  categories: readonly LabCategory[]
  purpose: string
  grantedAt: string
}

export type ActiveConsent = ConsentDetails & {
  status: "active"
  expiry: ConsentExpiry
}

export type ExpiredConsent = ConsentDetails & {
  status: "expired"
  expiredAt: string
}

export type RevokedConsent = ConsentDetails & {
  status: "revoked"
  revokedAt: string
}

export type Consent = ActiveConsent | ExpiredConsent | RevokedConsent

export type ConsentGroups = {
  active: readonly ActiveConsent[]
  expired: readonly ExpiredConsent[]
  revoked: readonly RevokedConsent[]
}

export type Organization = {
  id: string
  name: string
  kind: "clinic" | "research_institution"
}
