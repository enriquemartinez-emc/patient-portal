import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { formErrorMessage, groupByStatus } from "@/core/consents/consents.rules"
import type { Consent } from "@/core/consents/consents.types"
import { requireSessionPage } from "@/features/auth/session"
import { ConsentCard } from "@/features/consents/components/consent-card"
import { GrantConsentForm } from "@/features/consents/components/grant-consent-form"
import { listConsents, listOrganizations } from "@/features/consents/repository"

export const metadata = { title: "Consents · Patient Portal" }

function ConsentSection({
  title,
  consents,
  emptyText,
}: {
  title: string
  consents: readonly Consent[]
  emptyText: string
}) {
  return (
    <section aria-label={title} className="flex flex-col gap-3">
      <h2 className="text-lg font-medium">{title}</h2>
      {consents.length === 0 ? (
        <p className="text-sm text-muted-foreground">{emptyText}</p>
      ) : (
        consents.map((consent) => (
          <ConsentCard key={consent.id} consent={consent} />
        ))
      )}
    </section>
  )
}

export default async function ConsentsPage({
  searchParams,
}: {
  searchParams: Promise<{ error?: string; granted?: string; revoked?: string }>
}) {
  const session = await requireSessionPage("patient")
  const { error, granted, revoked } = await searchParams

  // Independent requests start together rather than one after the other.
  const [consents, organizations] = await Promise.all([
    listConsents(session.patientId),
    listOrganizations(),
  ])
  const groups = groupByStatus(consents)
  const errorMessage = formErrorMessage(error)

  return (
    <div className="flex flex-col gap-8">
      <div>
        <h1 className="text-2xl font-semibold">Consents</h1>
        <p className="text-sm text-muted-foreground">
          Decide which clinics and researchers can see your lab results.
        </p>
      </div>

      {errorMessage && (
        <Alert variant="destructive">
          <AlertTitle>We couldn&apos;t share your results</AlertTitle>
          <AlertDescription>{errorMessage}</AlertDescription>
        </Alert>
      )}
      {granted && (
        <Alert>
          <AlertTitle>Results shared</AlertTitle>
          <AlertDescription>Your consent is now active.</AlertDescription>
        </Alert>
      )}
      {revoked && (
        <Alert>
          <AlertTitle>Access revoked</AlertTitle>
          <AlertDescription>
            They can no longer see your results.
          </AlertDescription>
        </Alert>
      )}

      <div className="grid gap-8 lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)]">
        <GrantConsentForm organizations={organizations} now={new Date()} />
        <div className="flex flex-col gap-8">
          <ConsentSection
            title="Active"
            consents={groups.active}
            emptyText="You are not sharing your results with anyone."
          />
          <ConsentSection
            title="Expired"
            consents={groups.expired}
            emptyText="No consents have expired."
          />
          <ConsentSection
            title="Revoked"
            consents={groups.revoked}
            emptyText="You haven't revoked any consents."
          />
        </div>
      </div>
    </div>
  )
}
