import { Badge } from "@/components/ui/badge"
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { revokeConsentAction } from "@/features/consents/actions"
import { RevokeConsentDialog } from "@/features/consents/components/revoke-consent-dialog"
import type { Consent } from "@/features/consents/types"
import { formatDate } from "@/lib/format"

const list = new Intl.ListFormat("en-GB", { type: "conjunction" })

function describe(consent: Consent): string {
  const who = consent.granteeName
  const what = list.format(consent.categories)

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

export function ConsentCard({ consent }: { consent: Consent }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>{consent.granteeName}</CardTitle>
        <CardDescription>{describe(consent)}</CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-3 text-sm">
        <div className="flex flex-wrap gap-2">
          {consent.categories.map((category) => (
            <Badge key={category} variant="secondary" className="capitalize">
              {category}
            </Badge>
          ))}
        </div>
        <p>
          <span className="text-muted-foreground">Reason: </span>
          {consent.purpose}
        </p>
      </CardContent>
      {consent.status === "active" && (
        <CardFooter>
          <RevokeConsentDialog
            consentId={consent.id}
            granteeName={consent.granteeName}
            action={revokeConsentAction}
          />
        </CardFooter>
      )}
    </Card>
  )
}
