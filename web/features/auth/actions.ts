"use server"

import { headers } from "next/headers"
import { redirect } from "next/navigation"

import { getAuth } from "@/lib/auth"

// Signing in and out are the two actions that run without a session: one creates it, the other ends it.
export async function signInAction(): Promise<void> {
  const { url } = await getAuth().api.signInSocial({
    body: { provider: "keycloak", callbackURL: "/", disableRedirect: true },
    headers: await headers(),
  })
  if (!url) {
    throw new Error("Keycloak did not return a sign-in address.")
  }
  redirect(url)
}

export async function signOutAction(): Promise<void> {
  // Ends the portal session, then sends the browser to Keycloak to end its session too, so the
  // next sign-in asks for credentials again.
  const result = await getAuth().api.signOut({
    body: { callbackURL: "/login", disableRedirect: true },
    headers: await headers(),
  })
  redirect(result.url ?? "/login")
}
