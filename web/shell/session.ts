import "server-only"

import { cookies } from "next/headers"
import { redirect } from "next/navigation"

import { homePathFor } from "@/core/session/session.rules"
import type { Session, SessionKind } from "@/core/session/session.types"
import { findDevPersona, personaId } from "@/shell/dev-personas"

// Development "act as" session: a cookie naming one of the seeded people. This module is the
// single seam the rest of the app uses to learn who is acting; real authentication replaces it.
const COOKIE = "dev_actor"

export function isDevSessionEnabled(): boolean {
  return process.env.ENABLE_DEV_SESSION === "true"
}

export async function getSession(): Promise<Session | null> {
  // Read the request's cookies before anything else: it opts every page that asks who is acting
  // into per-request rendering, so a signed-out redirect can never be prerendered at build time.
  const store = await cookies()
  if (!isDevSessionEnabled()) {
    return null
  }
  const id = store.get(COOKIE)?.value
  return (id && findDevPersona(id)) || null
}

// For Server Actions: fails the request when nobody is acting.
export async function requireSession(): Promise<Session> {
  const session = await getSession()
  if (!session) {
    throw new Error("No active session.")
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
    redirect("/act-as")
  }
  if (kind && session.kind !== kind) {
    redirect(homePathFor(session))
  }
  return session
}

export async function writeSession(session: Session): Promise<void> {
  ;(await cookies()).set(COOKIE, personaId(session), {
    httpOnly: true,
    sameSite: "lax",
    path: "/",
  })
}

export async function clearSession(): Promise<void> {
  ;(await cookies()).delete(COOKIE)
}
