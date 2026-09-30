import type { LabCategory } from "@/features/lab-results/types"

// A patient who has shared data with the researcher's institution. Deliberately has no name.
export type ResearchParticipant = {
  patientId: string
  categories: readonly LabCategory[]
}
