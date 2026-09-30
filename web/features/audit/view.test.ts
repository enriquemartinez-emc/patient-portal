import { describe, expect, it } from "vitest"

import type { AuditEntry } from "@/core/audit/audit.types"

import { describeAuditEntry } from "./view"

const clinician = {
  kind: "clinician",
  name: "Dr. Sarah Thompson",
  organization: "Northside Clinic",
} as const
const patient = { kind: "patient", name: "Emily Carter" } as const

const entry = (
  actor: AuditEntry["actor"],
  action: AuditEntry["action"]
): AuditEntry => ({
  id: "1",
  occurredAt: "2026-06-01T10:00:00Z",
  actor,
  action,
})

describe("describeAuditEntry", () => {
  it("names the clinician, their organization and how many results they viewed", () => {
    expect(
      describeAuditEntry(
        entry(clinician, { type: "lab_results_read", labResultCount: 3 })
      )
    ).toBe("Dr. Sarah Thompson (Northside Clinic) viewed 3 lab results.")
  })

  it("uses the singular for one result", () => {
    expect(
      describeAuditEntry(
        entry(clinician, { type: "lab_results_read", labResultCount: 1 })
      )
    ).toBe("Dr. Sarah Thompson (Northside Clinic) viewed 1 lab result.")
  })

  it("says so when a read found nothing to share", () => {
    expect(
      describeAuditEntry(
        entry(clinician, { type: "lab_results_read", labResultCount: 0 })
      )
    ).toBe(
      "Dr. Sarah Thompson (Northside Clinic) asked to view your lab results, but none were available to share."
    )
  })

  it("speaks of the patient's own actions as 'You'", () => {
    expect(
      describeAuditEntry(
        entry(patient, { type: "consent_granted", consentId: "c" })
      )
    ).toBe("You granted a consent.")
    expect(
      describeAuditEntry(
        entry(patient, { type: "consent_revoked", consentId: "c" })
      )
    ).toBe("You revoked a consent.")
  })
})
