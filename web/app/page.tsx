import { redirect } from "next/navigation"

import { homePathFor } from "@/features/auth/navigation"
import { getSession } from "@/features/auth/session"

export default async function Page() {
  const session = await getSession()
  redirect(session ? homePathFor(session) : "/login")
}
