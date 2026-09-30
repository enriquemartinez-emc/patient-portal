import { Badge } from "@/components/ui/badge"
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { canRevoke } from "@/core/consents/consents.rules"
import type { Consent } from "@/core/consents/consents.types"
import { revokeConsentAction } from "@/features/consents/actions"
import { RevokeConsentDialog } from "@/features/consents/components/revoke-consent-dialog"
import { describeConsent } from "@/features/consents/view"
import { humanize } from "@/lib/format"

export function ConsentCard({ consent }: { consent: Consent }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>{consent.granteeName}</CardTitle>
        <CardDescription>{describeConsent(consent)}</CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-3 text-sm">
        <div className="flex flex-wrap gap-2">
          {consent.categories.map((category) => (
            <Badge key={category} variant="secondary">
              {humanize(category)}
            </Badge>
          ))}
        </div>
        <p>
          <span className="text-muted-foreground">Reason: </span>
          {consent.purpose}
        </p>
      </CardContent>
      {canRevoke(consent) && (
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
