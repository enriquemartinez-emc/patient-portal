import "server-only"

import { cookies } from "next/headers"
import { redirect } from "next/navigation"
import { cache } from "react"

import type { Session, SessionKind } from "@/features/auth/types"
import { findDemoAccount } from "@/features/auth/demo-accounts"
import { SESSION_COOKIE } from "@/features/auth/session-cookie"

// Demo authentication: the session cookie holds the email of a demo account. This module is the
// single place the rest of the app learns who is signed in; real authentication replaces it.
export function isDemoAuthEnabled(): boolean {
  return process.env.ENABLE_DEMO_AUTH === "true"
}

// Deduplicated per request, so the layout, page and actions can all ask without repeating work.
export const getSession = cache(async (): Promise<Session | null> => {
  // Read the request's cookies before anything else: it opts every page that asks who is signed
  // in into per-request rendering, so a signed-out redirect can never be prerendered at build time.
  const store = await cookies()
  if (!isDemoAuthEnabled()) {
    return null
  }
  const email = store.get(SESSION_COOKIE)?.value
  return (email && findDemoAccount(email)?.session) || null
})

// For Server Actions: fails the request when nobody is signed in. Every action that changes
// data calls this itself, because actions are public endpoints and can be invoked directly.
export async function requireSession(): Promise<Session> {
  const session = await getSession()
  if (!session) {
    throw new Error("Not signed in.")
  }
  return session
}

// For pages and layouts: sends the visitor to sign in, or to their own home when the page
// belongs to a different kind of user.
export async function requireSessionPage(): Promise<Session>
export async function requireSessionPage<K extends SessionKind>(
  kind: K
): Promise<Extract<Session, { kind: K }>>
export async function requireSessionPage(kind?: SessionKind): Promise<Session> {
  const session = await getSession()
  if (!session) {
    redirect("/login")
  }
  if (kind && session.kind !== kind) {
    redirect(`/${session.kind}`)
  }
  return session
}

export async function writeSession(email: string): Promise<void> {
  ;(await cookies()).set(SESSION_COOKIE, email, {
    httpOnly: true,
    sameSite: "lax",
    path: "/",
  })
}

export async function clearSession(): Promise<void> {
  ;(await cookies()).delete(SESSION_COOKIE)
}
