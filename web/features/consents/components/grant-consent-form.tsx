import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { NativeSelect, NativeSelectOption } from "@/components/ui/native-select"
import { Textarea } from "@/components/ui/textarea"
import type { Organization } from "@/features/consents/types"
import { LAB_CATEGORIES } from "@/features/lab-results/types"
import { grantConsentAction } from "@/features/consents/actions"
import { earliestExpiryDate } from "@/features/consents/expiry"

export function GrantConsentForm({
  organizations,
  now,
}: {
  organizations: readonly Organization[]
  now: Date
}) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Share your results</CardTitle>
        <CardDescription>
          Choose who can see which kinds of results. You can stop sharing at any
          time.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form action={grantConsentAction} className="flex flex-col gap-5">
          <div className="flex flex-col gap-2">
            <Label htmlFor="organizationId">Share with</Label>
            <NativeSelect
              id="organizationId"
              name="organizationId"
              required
              defaultValue=""
            >
              <NativeSelectOption value="" disabled>
                Choose a clinic or institution
              </NativeSelectOption>
              {organizations.map((organization) => (
                <NativeSelectOption
                  key={organization.id}
                  value={organization.id}
                >
                  {organization.name} (
                  {organization.kind === "clinic"
                    ? "Clinic"
                    : "Research institution"}
                  )
                </NativeSelectOption>
              ))}
            </NativeSelect>
          </div>

          <fieldset className="flex flex-col gap-2">
            <legend className="mb-1 text-sm font-medium">
              Results to share
            </legend>
            <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
              {LAB_CATEGORIES.map((category) => (
                <label
                  key={category}
                  className="flex items-center gap-2 text-sm"
                >
                  <input
                    type="checkbox"
                    name="categories"
                    value={category}
                    className="size-4 accent-primary"
                  />
                  <span className="capitalize">{category}</span>
                </label>
              ))}
            </div>
          </fieldset>

          <div className="flex flex-col gap-2">
            <Label htmlFor="purpose">Why are you sharing?</Label>
            <Textarea
              id="purpose"
              name="purpose"
              required
              maxLength={500}
              rows={3}
            />
          </div>

          <div className="flex flex-col gap-2">
            <Label htmlFor="expiresOn">Stop sharing on (optional)</Label>
            <Input
              id="expiresOn"
              name="expiresOn"
              type="date"
              min={earliestExpiryDate(now)}
              className="w-fit"
            />
            <p className="text-xs text-muted-foreground">
              Leave empty to keep sharing until you stop it.
            </p>
          </div>

          <Button type="submit" className="w-fit">
            Share results
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}
