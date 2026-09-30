import type { AccessBasis } from "./patients.types"

export function accessBasisLabel(basis: AccessBasis): string {
  switch (basis) {
    case "treatment":
      return "You treat this patient"
    case "consent":
      return "Shared with your organization"
  }
}
