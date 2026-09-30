import { describe, expect, it } from "vitest"

import { isCurrentPath, navigationFor } from "./auth.rules"
import type { NavItem, Session } from "./auth.types"

const labResults: NavItem = {
  label: "Lab results",
  href: "/patient",
  activeOn: ["/patient/lab-results"],
}
const consents: NavItem = {
  label: "Consents",
  href: "/patient/consents",
  activeOn: [],
}

describe("isCurrentPath", () => {
  it("is current on its own path and the deeper paths it lists", () => {
    expect(isCurrentPath(labResults, "/patient")).toBe(true)
    expect(isCurrentPath(labResults, "/patient/lab-results/abc")).toBe(true)
  })

  it("is not current on a sibling section, even though it shares a prefix", () => {
    expect(isCurrentPath(labResults, "/patient/consents")).toBe(false)
    expect(isCurrentPath(labResults, "/patient/access-history")).toBe(false)
  })

  it("is not current on a path that only starts with the same letters", () => {
    expect(isCurrentPath(consents, "/patient/consents-archive")).toBe(false)
    expect(isCurrentPath(consents, "/patient/consents")).toBe(true)
  })
})

describe("navigationFor", () => {
  it("gives each kind of user only their own sections", () => {
    const patient: Session = { kind: "patient", patientId: "p", name: "P" }
    const clinician: Session = {
      kind: "clinician",
      clinicianId: "c",
      name: "C",
    }

    expect(navigationFor(patient).map((item) => item.label)).toEqual([
      "Lab results",
      "Consents",
      "Access history",
    ])
    expect(navigationFor(clinician).map((item) => item.label)).toEqual([
      "Patients",
    ])
  })
})
