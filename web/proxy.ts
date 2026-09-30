import { NextResponse } from "next/server"
import type { NextRequest } from "next/server"

import { SESSION_COOKIE } from "@/features/auth/session-cookie"

// Sends visitors without a session cookie to the sign-in page. It only checks the cookie's
// presence; the layout verifies it, and every action authenticates for itself.
export function proxy(request: NextRequest) {
  if (!request.cookies.has(SESSION_COOKIE)) {
    return NextResponse.redirect(new URL("/login", request.url))
  }
  return NextResponse.next()
}

export const config = {
  matcher: ["/patient/:path*", "/clinician/:path*", "/researcher/:path*"],
}
