import type { ActiveConsent, Consent } from "./consents.types"

// Only an active consent can be revoked; the type guard lets callers narrow before acting.
export function canRevoke(consent: Consent): consent is ActiveConsent {
  return consent.status === "active"
}

// The earliest expiry a patient can choose: tomorrow (UTC), as a yyyy-mm-dd date.
export function earliestExpiryDate(now: Date): string {
  const tomorrow = new Date(
    Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate() + 1)
  )
  return tomorrow.toISOString().slice(0, 10)
}

export function isValidExpiryDate(date: string, now: Date): boolean {
  return date >= earliestExpiryDate(now)
}

// A chosen expiry date is the end of that day, so access lasts through it.
export function expiryInstant(date: string): string {
  return `${date}T23:59:59Z`
}
