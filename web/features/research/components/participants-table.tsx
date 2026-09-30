import Link from "next/link"

import { Badge } from "@/components/ui/badge"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  categoryLabel,
  sortCategories,
} from "@/core/lab-results/lab-results.rules"
import { participantLabel } from "@/core/research/research.rules"
import type { ResearchParticipant } from "@/core/research/research.types"

export function ParticipantsTable({
  participants,
}: {
  participants: readonly ResearchParticipant[]
}) {
  if (participants.length === 0) {
    return (
      <p className="text-muted-foreground">
        No participants yet. Patients appear here when they share results with
        your institution.
      </p>
    )
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Participant</TableHead>
          <TableHead>Shared categories</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {participants.map((participant) => (
          <TableRow key={participant.patientId}>
            <TableCell>
              {/* No prefetch: opening a participant's results is recorded in their audit trail. */}
              <Link
                href={`/researcher/participants/${participant.patientId}`}
                prefetch={false}
                className="font-medium underline-offset-4 hover:underline"
              >
                {participantLabel(participant.patientId)}
              </Link>
            </TableCell>
            <TableCell>
              <div className="flex flex-wrap gap-2">
                {sortCategories(participant.categories).map((category) => (
                  <Badge key={category} variant="secondary">
                    {categoryLabel(category)}
                  </Badge>
                ))}
              </div>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
