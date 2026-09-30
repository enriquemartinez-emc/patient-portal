import "server-only"

import { z } from "zod"

import type { AuditActionType, AuditEntry } from "@/core/audit/audit.types"
import { apiGet } from "@/lib/api/server"
import { pagedSchema, type Paged } from "@/lib/api/schemas"

const auditEntryWireSchema = z.object({
  id: z.guid(),
  occurredAt: z.iso.datetime({ offset: true }),
  actorKind: z.enum(["patient", "clinician", "researcher"]),
  actorName: z.string(),
  actorOrganization: z.string().nullable(),
  action: z.enum(["lab_results_read", "consent_granted", "consent_revoked"]),
  labResultIds: z.array(z.guid()),
  consentGrantId: z.guid().nullable(),
})

type AuditEntryWire = z.infer<typeof auditEntryWireSchema>

function toEntry(wire: AuditEntryWire): AuditEntry {
  const actor: AuditEntry["actor"] =
    wire.actorKind === "patient"
      ? { kind: "patient", name: wire.actorName }
      : {
          kind: wire.actorKind,
          name: wire.actorName,
          organization: wire.actorOrganization ?? "Unknown organization",
        }

  switch (wire.action) {
    case "lab_results_read":
      return {
        id: wire.id,
        occurredAt: wire.occurredAt,
        actor,
        action: {
          type: "lab_results_read",
          labResultCount: wire.labResultIds.length,
        },
      }
    case "consent_granted":
    case "consent_revoked":
      if (!wire.consentGrantId) {
        throw new Error(`Audit entry ${wire.id} names no consent.`)
      }
      return {
        id: wire.id,
        occurredAt: wire.occurredAt,
        actor,
        action: { type: wire.action, consentId: wire.consentGrantId },
      }
  }
}

export async function listAuditTrail(
  patientId: string,
  options: { page: number; action?: AuditActionType }
): Promise<Paged<AuditEntry>> {
  const query = new URLSearchParams({ page: String(options.page) })
  if (options.action) {
    query.set("action", options.action)
  }

  const page = await apiGet(
    `/patients/${patientId}/audit?${query}`,
    pagedSchema(auditEntryWireSchema)
  )
  return { ...page, items: page.items.map(toEntry) }
}
