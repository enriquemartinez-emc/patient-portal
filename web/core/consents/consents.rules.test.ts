import { describe, expect, it } from "vitest"

import {
  canRevoke,
  earliestExpiryDate,
  expiryInstant,
  isValidExpiryDate,
} from "./consents.rules"
import type { Consent } from "./consents.types"

const base = {
  id: "1",
  granteeOrganizationId: "org",
  granteeName: "Riverside Clinic",
  categories: ["hematology", "lipids"],
  purpose: "Second opinion",
  grantedAt: "2026-01-15T10:00:00Z",
} as const

describe("canRevoke", () => {
  it("only an active consent can be revoked", () => {
    const consents: Consent[] = [
      { ...base, status: "active", expiry: { kind: "never" } },
      {
        ...base,
        status: "active",
        expiry: { kind: "on", at: "2030-01-01T23:59:59Z" },
      },
      { ...base, status: "expired", expiredAt: "2026-03-01T00:00:00Z" },
      { ...base, status: "revoked", revokedAt: "2026-02-01T09:00:00Z" },
    ]

    expect(consents.map(canRevoke)).toEqual([true, true, false, false])
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
