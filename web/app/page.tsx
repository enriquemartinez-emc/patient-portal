import { redirect } from "next/navigation"

import { homePathFor } from "@/core/auth/auth.rules"
import { getSession } from "@/features/auth/session"

export default async function Page() {
  const session = await getSession()
  redirect(session ? homePathFor(session) : "/login")
}
