import "server-only"

import { headers } from "next/headers"
import { redirect } from "next/navigation"
import { cache } from "react"

import { getMe } from "@/features/auth/repository"
import type { Session, SessionKind, SessionState } from "@/features/auth/types"
import { getAuth } from "@/lib/auth"

export const getAuthSession = cache(async () => {
  // Reading the request's headers opts every page that asks who is signed in into per-request
  // rendering, so a signed-out redirect can never be prerendered at build time.
  const requestHeaders = await headers()
  return getAuth().api.getSession({ headers: requestHeaders })
})

export const getSessionState = cache(async (): Promise<SessionState> => {
  const authSession = await getAuthSession()
  return authSession ? getMe() : { status: "signed-out" }
})

export const getSession = cache(async (): Promise<Session | null> => {
  const state = await getSessionState()
  return state.status === "active" ? state.session : null
})

// For Server Actions: sends the visitor to sign in when there is no usable session. Every action
// that changes data calls this itself, because actions are public endpoints and can be invoked
// directly. The sign-in page says why, e.g. that the session expired.
export async function requireSession(): Promise<Session> {
  const session = await getSession()
  if (!session) {
    redirect("/login")
  }
  return session
}

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
