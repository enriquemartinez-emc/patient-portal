import { redirect } from "next/navigation"

import { getSession } from "@/features/auth/session"

export default async function Page() {
  const session = await getSession()
  redirect(session ? `/${session.kind}` : "/login")
}
