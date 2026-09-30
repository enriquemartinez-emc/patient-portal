import "server-only"

import { createHash, timingSafeEqual } from "node:crypto"

import type { Session } from "@/core/auth/auth.types"

// Demo sign-in data: the people created by the API's dev seed
// (api/PatientPortal.Migrations/Scripts/DevSeed), each with an email to sign in with. All demo
// accounts share one password. Replaced by the identity provider when Keycloak is added.
export const DEMO_PASSWORD = "demo-password"

export type DemoAccount = {
  email: string
  session: Session
}

export const demoAccounts: readonly DemoAccount[] = [
  {
    email: "emily.carter@demo.example",
    session: {
      kind: "patient",
      patientId: "b0000000-0000-0000-0000-000000000001",
      name: "Emily Carter",
    },
  },
  {
    email: "james.wilson@demo.example",
    session: {
      kind: "patient",
      patientId: "b0000000-0000-0000-0000-000000000002",
      name: "James Wilson",
    },
  },
  {
    email: "sarah.thompson@demo.example",
    session: {
      kind: "clinician",
      clinicianId: "c0000000-0000-0000-0000-000000000001",
      name: "Dr. Sarah Thompson",
    },
  },
  {
    email: "michael.brown@demo.example",
    session: {
      kind: "clinician",
      clinicianId: "c0000000-0000-0000-0000-000000000002",
      name: "Dr. Michael Brown",
    },
  },
  {
    email: "laura.davies@demo.example",
    session: {
      kind: "researcher",
      researcherId: "d0000000-0000-0000-0000-000000000001",
      name: "Dr. Laura Davies",
    },
  },
]

export function sessionId(session: Session): string {
  switch (session.kind) {
    case "patient":
      return session.patientId
    case "clinician":
      return session.clinicianId
    case "researcher":
      return session.researcherId
  }
}

export function findSessionById(id: string): Session | undefined {
  return demoAccounts.find((account) => sessionId(account.session) === id)
    ?.session
}

// Returns the session for a matching email and password, otherwise undefined.
export function authenticateDemoAccount(
  email: string,
  password: string
): Session | undefined {
  const account = demoAccounts.find(
    (candidate) => candidate.email === email.trim().toLowerCase()
  )
  return account && passwordMatches(password) ? account.session : undefined
}

// Hashing first gives both sides the same length, which timingSafeEqual requires.
function passwordMatches(password: string): boolean {
  const digest = (value: string) => createHash("sha256").update(value).digest()
  return timingSafeEqual(digest(password), digest(DEMO_PASSWORD))
}
