// Untrusted query-string value -> a page number of at least 1.
export function parsePageNumber(value: string | undefined): number {
  const page = Number.parseInt(value ?? "", 10)
  return Number.isSafeInteger(page) && page >= 1 ? page : 1
}

// Untrusted query-string value -> one of the allowed values, or undefined.
export function parseOneOf<T extends string>(
  value: string | undefined,
  allowed: readonly T[]
): T | undefined {
  return allowed.find((candidate) => candidate === value)
}
