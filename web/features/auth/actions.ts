"use server"

import { headers } from "next/headers"
import { redirect } from "next/navigation"

import { getSessionState } from "@/features/auth/session"
import { getAuth } from "@/lib/auth"

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
  // Read before signing out, which removes the session. Keycloak's own session can end first, when
  // it sits idle. Asking Keycloak to log out a session it no longer has makes it show an error
  // page instead of returning, and there is nothing left to end there.
  const { status } = await getSessionState()

  // Ends the portal session, then sends the browser to Keycloak to end its session too, so the
  // next sign-in asks for credentials again.
  const result = await getAuth().api.signOut({
    body: { callbackURL: "/login", disableRedirect: true },
    headers: await headers(),
  })
  redirect(status === "expired" ? "/login" : (result.url ?? "/login"))
}
