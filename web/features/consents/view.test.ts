import { describe, expect, it } from "vitest"

import type {
  ActiveConsent,
  ExpiredConsent,
  RevokedConsent,
} from "@/core/consents/consents.types"

import { describeConsent, formErrorMessage, groupByStatus } from "./view"

const base = {
  id: "1",
  granteeOrganizationId: "org",
  granteeName: "Riverside Clinic",
  categories: ["hematology", "lipids"],
  purpose: "Second opinion",
  grantedAt: "2026-01-15T10:00:00Z",
} as const

const active: ActiveConsent = {
  ...base,
  status: "active",
  expiry: { kind: "never" },
}
const expiring: ActiveConsent = {
  ...base,
  status: "active",
  expiry: { kind: "on", at: "2030-10-12T23:59:59Z" },
}
const expired: ExpiredConsent = {
  ...base,
  status: "expired",
  expiredAt: "2026-03-01T00:00:00Z",
}
const revoked: RevokedConsent = {
  ...base,
  status: "revoked",
  revokedAt: "2026-02-01T09:00:00Z",
}

describe("describeConsent", () => {
  it("says an active consent without expiry does not expire", () => {
    expect(describeConsent(active)).toBe(
      "Riverside Clinic can view your hematology and lipids results. This access does not expire."
    )
  })

  it("names the date an active consent runs until", () => {
    expect(describeConsent(expiring)).toBe(
      "Riverside Clinic can view your hematology and lipids results until 12 Oct 2030."
    )
  })

  it("says when an expired consent lapsed", () => {
    expect(describeConsent(expired)).toBe(
      "Riverside Clinic could view your hematology and lipids results until 1 Mar 2026. This access has expired."
    )
  })

  it("says when a consent was revoked", () => {
    expect(describeConsent(revoked)).toBe(
      "Riverside Clinic could view your hematology and lipids results. You revoked this access on 1 Feb 2026."
    )
  })
})

describe("groupByStatus", () => {
  it("groups consents by status", () => {
    const groups = groupByStatus([revoked, active, expired, expiring])

    expect(groups.active).toEqual([active, expiring])
    expect(groups.expired).toEqual([expired])
    expect(groups.revoked).toEqual([revoked])
  })
})

describe("formErrorMessage", () => {
  it("knows its own error codes and ignores anything else", () => {
    expect(formErrorMessage("categories")).toMatch(/at least one category/)
    expect(formErrorMessage("<script>")).toBeUndefined()
    expect(formErrorMessage(undefined)).toBeUndefined()
  })
})
