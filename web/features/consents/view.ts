import type { Consent, ConsentGroups } from "@/core/consents/consents.types"
import { formatDate, joinWithAnd } from "@/lib/format"

// What a consent means, in words a patient would use.
export function describeConsent(consent: Consent): string {
  const who = consent.granteeName
  const what = joinWithAnd(consent.categories)

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

export function groupByStatus(consents: readonly Consent[]): ConsentGroups {
  return {
    active: consents.filter((c) => c.status === "active"),
    expired: consents.filter((c) => c.status === "expired"),
    revoked: consents.filter((c) => c.status === "revoked"),
  }
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
