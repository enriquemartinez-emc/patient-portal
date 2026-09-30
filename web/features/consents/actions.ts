"use server"

import { revalidatePath } from "next/cache"
import { redirect } from "next/navigation"
import { z } from "zod"

import {
  expiryInstant,
  isValidExpiryDate,
} from "@/core/consents/consents.rules"
import { LAB_CATEGORIES } from "@/core/lab-results/lab-results.types"
import { requireSession } from "@/features/auth/session"
import { grantConsent, revokeConsent } from "@/features/consents/repository"
import type { ConsentFormError } from "@/features/consents/view"
import { ApiError } from "@/lib/api/server"

const CONSENTS_PATH = "/patient/consents"

const grantSchema = z.object({
  organizationId: z.guid(),
  categories: z.array(z.enum(LAB_CATEGORIES)).min(1),
  purpose: z.string().trim().min(1).max(500),
  expiresOn: z.union([z.literal(""), z.iso.date()]),
})

const issueToError: Record<string, ConsentFormError> = {
  organizationId: "organization",
  categories: "categories",
  purpose: "purpose",
  expiresOn: "expiry",
}

// Actions are public endpoints, so each one confirms a patient is signed in before doing anything.
async function requirePatient() {
  const session = await requireSession()
  if (session.kind !== "patient") {
    throw new Error("Only patients manage their own consents.")
  }
  return session
}

export async function grantConsentAction(formData: FormData): Promise<void> {
  const session = await requirePatient()

  const parsed = grantSchema.safeParse({
    organizationId: formData.get("organizationId"),
    categories: formData.getAll("categories"),
    purpose: formData.get("purpose"),
    expiresOn: formData.get("expiresOn") ?? "",
  })
  if (!parsed.success) {
    const field = String(parsed.error.issues[0]?.path[0] ?? "")
    redirect(`${CONSENTS_PATH}?error=${issueToError[field] ?? "failed"}`)
  }

  const { organizationId, categories, purpose, expiresOn } = parsed.data
  if (expiresOn !== "" && !isValidExpiryDate(expiresOn, new Date())) {
    redirect(`${CONSENTS_PATH}?error=expiry`)
  }

  let saved = true
  try {
    await grantConsent(session.patientId, {
      granteeOrganizationId: organizationId,
      categories,
      purpose,
      expiresAt: expiresOn === "" ? undefined : expiryInstant(expiresOn),
    })
  } catch (error) {
    if (!(error instanceof ApiError) || error.status >= 500) {
      throw error
    }
    saved = false
  }

  if (!saved) {
    redirect(`${CONSENTS_PATH}?error=failed`)
  }
  revalidatePath(CONSENTS_PATH)
  revalidatePath("/patient/access-history")
  redirect(`${CONSENTS_PATH}?granted=1`)
}

export async function revokeConsentAction(formData: FormData): Promise<void> {
  const session = await requirePatient()

  const consentId = z.guid().safeParse(formData.get("consentId"))
  if (!consentId.success) {
    redirect(CONSENTS_PATH)
  }

  try {
    await revokeConsent(session.patientId, consentId.data)
  } catch (error) {
    // Already gone or already expired: nothing left to revoke, so just show the current state.
    if (!(error instanceof ApiError) || ![404, 409].includes(error.status)) {
      throw error
    }
  }

  revalidatePath(CONSENTS_PATH)
  revalidatePath("/patient/access-history")
  redirect(`${CONSENTS_PATH}?revoked=1`)
}
