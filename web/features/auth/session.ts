import "server-only"

import { cookies } from "next/headers"
import { redirect } from "next/navigation"
import { cache } from "react"

import { SESSION_COOKIE, homePathFor } from "@/core/auth/auth.rules"
import type { Session, SessionKind } from "@/core/auth/auth.types"
import { findSessionById, sessionId } from "@/features/auth/demo-accounts"

// Demo authentication: the session cookie names one of the demo accounts. This module is the
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
  const id = store.get(SESSION_COOKIE)?.value
  return (id && findSessionById(id)) || null
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
    redirect(homePathFor(session))
  }
  return session
}

export async function writeSession(session: Session): Promise<void> {
  ;(await cookies()).set(SESSION_COOKIE, sessionId(session), {
    httpOnly: true,
    sameSite: "lax",
    path: "/",
  })
}

export async function clearSession(): Promise<void> {
  ;(await cookies()).delete(SESSION_COOKIE)
}
