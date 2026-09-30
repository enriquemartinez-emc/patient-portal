"use server"

import { redirect } from "next/navigation"
import { z } from "zod"

import { authenticateDemoAccount } from "@/features/auth/demo-accounts"
import { homePathFor } from "@/features/auth/navigation"
import {
  clearSession,
  isDemoAuthEnabled,
  writeSession,
} from "@/features/auth/session"

const signInSchema = z.object({
  email: z.email(),
  password: z.string().min(1),
})

// Signing in is the one action that runs without a session: it is how one is created.
export async function signInAction(formData: FormData): Promise<void> {
  if (!isDemoAuthEnabled()) {
    redirect("/")
  }

  const parsed = signInSchema.safeParse({
    email: formData.get("email"),
    password: formData.get("password"),
  })
  const session = parsed.success
    ? authenticateDemoAccount(parsed.data.email, parsed.data.password)
    : undefined
  if (!session) {
    redirect("/login?error=invalid")
  }

  await writeSession(session)
  redirect(homePathFor(session))
}

export async function signOutAction(): Promise<void> {
  await clearSession()
  redirect("/login")
}
