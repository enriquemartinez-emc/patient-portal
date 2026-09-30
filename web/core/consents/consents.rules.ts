import {
  categoryLabel,
  sortCategories,
} from "@/core/lab-results/lab-results.rules"
import { formatDate, joinWithAnd } from "@/core/shared/dates"

import type {
  ActiveConsent,
  Consent,
  ConsentGroups,
  Organization,
} from "./consents.types"

function categoryList(consent: Consent): string {
  return joinWithAnd(
    sortCategories(consent.categories).map((category) =>
      categoryLabel(category).toLowerCase()
    )
  )
}

// What a consent means, in words a patient would use.
export function describeConsent(consent: Consent): string {
  const who = consent.granteeName
  const what = categoryList(consent)

  switch (consent.status) {
    case "active":
      return consent.expiry.kind === "never"
        ? `${who} can view your ${what} results. This access does not expire.`
        : `${who} can view your ${what} results until ${formatDate(consent.expiry.at)}.`
    case "expired":
      return `${who} could view your ${what} results until ${formatDate(consent.expiredAt)}. This access has expired.`
    case "revoked":
      return `${who} could view your ${what} results. You revoked this access on ${formatDate(consent.revokedAt)}.`
  }
}

// Only an active consent can be revoked; the type guard lets callers narrow before acting.
export function canRevoke(consent: Consent): consent is ActiveConsent {
  return consent.status === "active"
}

export function groupByStatus(consents: readonly Consent[]): ConsentGroups {
  return {
    active: consents.filter((c) => c.status === "active"),
    expired: consents.filter((c) => c.status === "expired"),
    revoked: consents.filter((c) => c.status === "revoked"),
  }
}

export function organizationKindLabel(kind: Organization["kind"]): string {
  switch (kind) {
    case "clinic":
      return "Clinic"
    case "research_institution":
      return "Research institution"
  }
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

export type ConsentFormError =
  "organization" | "categories" | "purpose" | "expiry" | "failed"

const formErrorMessages: Record<ConsentFormError, string> = {
  organization: "Choose who to share your results with.",
  categories: "Choose at least one category of results to share.",
  purpose: "Say why you are sharing, in 500 characters or fewer.",
  expiry: "Choose an expiry date from tomorrow onward, or leave it empty.",
  failed: "Your consent could not be saved. Please try again.",
}

export function formErrorMessage(code: string | undefined): string | undefined {
  return code && code in formErrorMessages
    ? formErrorMessages[code as ConsentFormError]
    : undefined
}
