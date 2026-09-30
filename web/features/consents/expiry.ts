// The earliest expiry a patient can choose: tomorrow (UTC), as a yyyy-mm-dd date.
export function earliestExpiryDate(now: Date): string {
  const tomorrow = new Date(
    Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate() + 1)
  )
  return tomorrow.toISOString().slice(0, 10)
}
