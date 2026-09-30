"use server"

import { redirect } from "next/navigation"
import { z } from "zod"

import { homePathFor } from "@/core/session/session.rules"
import { findDevPersona } from "@/shell/dev-personas"
import {
  clearSession,
  isDevSessionEnabled,
  writeSession,
} from "@/shell/session"

const actAsSchema = z.object({ personaId: z.guid() })

export async function actAs(formData: FormData): Promise<void> {
  if (!isDevSessionEnabled()) {
    redirect("/")
  }

  const parsed = actAsSchema.safeParse({ personaId: formData.get("personaId") })
  const session = parsed.success
    ? findDevPersona(parsed.data.personaId)
    : undefined
  if (!session) {
    redirect("/act-as")
  }

  await writeSession(session)
  redirect(homePathFor(session))
}

export async function endSession(): Promise<void> {
  await clearSession()
  redirect("/act-as")
}
