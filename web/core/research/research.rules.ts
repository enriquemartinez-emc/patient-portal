// Researchers never see a name: a participant is a short, stable reference.
export function participantLabel(patientId: string): string {
  return `Participant ${patientId.slice(-8).toUpperCase()}`
}
