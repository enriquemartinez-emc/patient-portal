import { notFound } from "next/navigation"
import { connection } from "next/server"

import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { personaLabel } from "@/core/session/session.rules"
import type { SessionKind } from "@/core/session/session.types"
import { actAs } from "@/features/session/actions"
import { devPersonas, personaId } from "@/shell/dev-personas"
import { isDevSessionEnabled } from "@/shell/session"

const kinds: readonly SessionKind[] = ["patient", "clinician", "researcher"]

export default async function ActAsPage() {
  // Read the flag per request, not at build time, so one image works in every environment.
  await connection()
  if (!isDevSessionEnabled()) {
    notFound()
  }

  return (
    <main className="mx-auto flex min-h-svh w-full max-w-3xl flex-col gap-6 px-6 py-12">
      <div>
        <h1 className="text-2xl font-semibold">Patient Portal</h1>
        <p className="text-sm text-muted-foreground">
          Development only: choose which seeded person to act as. Real sign-in
          replaces this.
        </p>
      </div>
      {kinds.map((kind) => (
        <Card key={kind}>
          <CardHeader>
            <CardTitle>{personaLabel(kind)}s</CardTitle>
            <CardDescription>Seeded {kind} accounts</CardDescription>
          </CardHeader>
          <CardContent className="flex flex-wrap gap-3">
            {devPersonas
              .filter((persona) => persona.kind === kind)
              .map((persona) => (
                <form key={personaId(persona)} action={actAs}>
                  <input
                    type="hidden"
                    name="personaId"
                    value={personaId(persona)}
                  />
                  <Button type="submit" variant="outline">
                    {persona.name}
                  </Button>
                </form>
              ))}
          </CardContent>
        </Card>
      ))}
    </main>
  )
}
