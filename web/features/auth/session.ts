import "server-only"

import { headers } from "next/headers"
import { redirect } from "next/navigation"
import { cache } from "react"

import { getMe } from "@/features/auth/repository"
import type { Session, SessionKind } from "@/features/auth/types"
import { getAuth } from "@/lib/auth"

export const getAuthSession = cache(async () => {
  // Reading the request's headers opts every page that asks who is signed in into per-request
  // rendering, so a signed-out redirect can never be prerendered at build time.
  const requestHeaders = await headers()
  return getAuth().api.getSession({ headers: requestHeaders })
})

export const getSession = cache(async (): Promise<Session | null> => {
  const authSession = await getAuthSession()
  return authSession ? getMe() : null
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
