import { redirect } from "next/navigation"

import { homePathFor } from "@/core/session/session.rules"
import { getSession } from "@/shell/session"

export default async function Page() {
  const session = await getSession()
  redirect(session ? homePathFor(session) : "/act-as")
}
