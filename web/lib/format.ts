// Fixed locale and time zone, so the same instant always reads the same on the server.
const dateFormat = new Intl.DateTimeFormat("en-GB", {
  dateStyle: "medium",
  timeZone: "UTC",
})

const dateTimeFormat = new Intl.DateTimeFormat("en-GB", {
  dateStyle: "medium",
  timeStyle: "short",
  timeZone: "UTC",
})

export function formatDate(iso: string): string {
  return dateFormat.format(new Date(iso))
}

export function formatDateTime(iso: string): string {
  return `${dateTimeFormat.format(new Date(iso))} UTC`
}

// "a", "a and b", "a, b and c".
export function joinWithAnd(items: readonly string[]): string {
  if (items.length <= 1) {
    return items.join("")
  }
  return `${items.slice(0, -1).join(", ")} and ${items[items.length - 1]}`
}

// "research_institution" -> "Research institution".
export function humanize(value: string): string {
  const spaced = value.replaceAll("_", " ")
  return spaced.charAt(0).toUpperCase() + spaced.slice(1)
}
