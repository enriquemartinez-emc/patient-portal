import "server-only"

import { z } from "zod"

import type { Consent, Organization } from "@/core/consents/consents.types"
import { LAB_CATEGORIES } from "@/core/lab-results/lab-results.types"
import { apiDelete, apiGet, apiPost } from "@/lib/api/server"

const consentWireSchema = z.object({
  id: z.guid(),
  granteeOrganizationId: z.guid(),
  granteeName: z.string(),
  categories: z.array(z.enum(LAB_CATEGORIES)),
  purpose: z.string(),
  grantedAt: z.iso.datetime({ offset: true }),
  expiresAt: z.iso.datetime({ offset: true }).nullable(),
  revokedAt: z.iso.datetime({ offset: true }).nullable(),
  status: z.enum(["active", "expired", "revoked"]),
})

type ConsentWire = z.infer<typeof consentWireSchema>

// The API reports one flat shape with nullable dates; each status carries the date that belongs to it.
function toConsent(wire: ConsentWire): Consent {
  const details = {
    id: wire.id,
    granteeOrganizationId: wire.granteeOrganizationId,
    granteeName: wire.granteeName,
    categories: wire.categories,
    purpose: wire.purpose,
    grantedAt: wire.grantedAt,
  }

  switch (wire.status) {
    case "active":
      return {
        ...details,
        status: "active",
        expiry: wire.expiresAt
          ? { kind: "on", at: wire.expiresAt }
          : { kind: "never" },
      }
    case "expired":
      if (!wire.expiresAt) {
        throw new Error(`Expired consent ${wire.id} has no expiry date.`)
      }
      return { ...details, status: "expired", expiredAt: wire.expiresAt }
    case "revoked":
      if (!wire.revokedAt) {
        throw new Error(`Revoked consent ${wire.id} has no revocation date.`)
      }
      return { ...details, status: "revoked", revokedAt: wire.revokedAt }
  }
}

export async function listConsents(patientId: string): Promise<Consent[]> {
  const { items } = await apiGet(
    `/patients/${patientId}/consents`,
    z.object({ items: z.array(consentWireSchema) })
  )
  return items.map(toConsent)
}

const organizationSchema = z.object({
  id: z.guid(),
  name: z.string(),
  kind: z.enum(["clinic", "research_institution"]),
}) satisfies z.ZodType<Organization>

export async function listOrganizations(): Promise<Organization[]> {
  const { items } = await apiGet(
    "/organizations",
    z.object({ items: z.array(organizationSchema) })
  )
  return items
}

export type GrantConsentInput = {
  granteeOrganizationId: string
  categories: readonly string[]
  purpose: string
  expiresAt?: string
}

export async function grantConsent(
  patientId: string,
  input: GrantConsentInput
): Promise<Consent> {
  return toConsent(
    await apiPost(`/patients/${patientId}/consents`, input, consentWireSchema)
  )
}

export async function revokeConsent(
  patientId: string,
  consentId: string
): Promise<void> {
  await apiDelete(`/patients/${patientId}/consents/${consentId}`)
}
