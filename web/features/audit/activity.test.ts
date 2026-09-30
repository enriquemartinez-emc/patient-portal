import { describe, expect, it } from "vitest"

import type { AuditEntry } from "@/features/audit/types"

import { activityByDay } from "./activity"

const now = new Date("2026-09-30T10:00:00Z")
const clinician = {
  kind: "clinician",
  name: "Dr. Sarah Thompson",
  organization: "Northside Clinic",
} as const

const read = (occurredAt: string): AuditEntry => ({
  id: occurredAt,
  occurredAt,
  actor: clinician,
  action: { type: "lab_results_read", labResultCount: 4 },
})

describe("activityByDay", () => {
  it("has one entry per day, oldest first, ending today, with zeros where nothing happened", () => {
    const days = activityByDay([], 7, now)

    expect(days.map((day) => day.date)).toEqual([
      "2026-09-24",
      "2026-09-25",
      "2026-09-26",
      "2026-09-27",
      "2026-09-28",
      "2026-09-29",
      "2026-09-30",
    ])
    expect(days.every((day) => day.views === 0)).toBe(true)
  })

  it("counts each read on the day it happened and ignores other actions and older days", () => {
    const entries: AuditEntry[] = [
      read("2026-09-30T08:00:00+00:00"),
      read("2026-09-30T09:30:00+00:00"),
      read("2026-09-27T23:59:59+00:00"),
      read("2026-09-10T12:00:00+00:00"),
      {
        id: "g",
        occurredAt: "2026-09-30T07:00:00+00:00",
        actor: { kind: "patient", name: "Emily Carter" },
        action: { type: "consent_granted", consentId: "c" },
      },
    ]

    const days = activityByDay(entries, 7, now)

    expect(
      Object.fromEntries(
        days.filter((d) => d.views > 0).map((d) => [d.date, d.views])
      )
    ).toEqual({
      "2026-09-30": 2,
      "2026-09-27": 1,
    })
  })

  it("puts a read on the UTC day even when it is written with another offset", () => {
    const days = activityByDay([read("2026-09-30T01:30:00+05:00")], 3, now)

    expect(days.find((day) => day.views > 0)?.date).toBe("2026-09-29")
  })
})
