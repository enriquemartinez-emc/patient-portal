export const consentFormErrors = {
  organization: "Choose who to share your results with.",
  categories: "Choose at least one category of results to share.",
  purpose: "Say why you are sharing, in 500 characters or fewer.",
  expiry: "Choose an expiry date from tomorrow onward, or leave it empty.",
  failed: "Your consent could not be saved. Please try again.",
} as const

export type ConsentFormError = keyof typeof consentFormErrors
