import { describe, expect, it } from "vitest"

import {
  canRevoke,
  describeConsent,
  earliestExpiryDate,
  expiryInstant,
  formErrorMessage,
  groupByStatus,
  isValidExpiryDate,
} from "./consents.rules"
import type {
  ActiveConsent,
  Consent,
  ExpiredConsent,
  RevokedConsent,
} from "./consents.types"

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

describe("describeConsent ordering", () => {
  it("lists categories in the fixed order however they were sent", () => {
    const shuffled: ActiveConsent = {
      ...active,
      categories: ["lipids", "hematology"],
    }
    expect(describeConsent(shuffled)).toContain("hematology and lipids results")
  })
})

describe("consent state", () => {
  it("only an active consent can be revoked", () => {
    const all: Consent[] = [active, expiring, expired, revoked]
    expect(all.map(canRevoke)).toEqual([true, true, false, false])
  })

  it("groups consents by status", () => {
    const groups = groupByStatus([revoked, active, expired, expiring])

    expect(groups.active).toEqual([active, expiring])
    expect(groups.expired).toEqual([expired])
    expect(groups.revoked).toEqual([revoked])
  })
})

describe("expiry dates", () => {
  const now = new Date("2026-09-29T22:30:00Z")

  it("the earliest expiry is tomorrow in UTC", () => {
    expect(earliestExpiryDate(now)).toBe("2026-09-30")
    expect(earliestExpiryDate(new Date("2026-12-31T23:59:59Z"))).toBe(
      "2027-01-01"
    )
  })

  it("accepts tomorrow or later and rejects today or earlier", () => {
    expect(isValidExpiryDate("2026-09-30", now)).toBe(true)
    expect(isValidExpiryDate("2027-01-01", now)).toBe(true)
    expect(isValidExpiryDate("2026-09-29", now)).toBe(false)
    expect(isValidExpiryDate("2026-01-01", now)).toBe(false)
  })

  it("makes access last through the chosen day", () => {
    expect(expiryInstant("2026-10-01")).toBe("2026-10-01T23:59:59Z")
  })
})

describe("formErrorMessage", () => {
  it("knows its own error codes and ignores anything else", () => {
    expect(formErrorMessage("categories")).toMatch(/at least one category/)
    expect(formErrorMessage("<script>")).toBeUndefined()
    expect(formErrorMessage(undefined)).toBeUndefined()
  })
})
