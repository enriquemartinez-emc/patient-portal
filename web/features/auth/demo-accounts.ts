import "server-only"

import { createHash, timingSafeEqual } from "node:crypto"

import type { Session } from "@/features/auth/types"

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

export function findDemoAccount(email: string): DemoAccount | undefined {
  return demoAccounts.find(
    (account) => account.email === email.trim().toLowerCase()
  )
}

// Returns the account for a matching email and password, otherwise undefined.
export function authenticateDemoAccount(
  email: string,
  password: string
): DemoAccount | undefined {
  const account = findDemoAccount(email)
  return account && passwordMatches(password) ? account : undefined
}

// Hashing first gives both sides the same length, which timingSafeEqual requires.
function passwordMatches(password: string): boolean {
  const digest = (value: string) => createHash("sha256").update(value).digest()
  return timingSafeEqual(digest(password), digest(DEMO_PASSWORD))
}
