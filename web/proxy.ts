import { getSessionCookie } from "better-auth/cookies"
import { NextResponse } from "next/server"
import type { NextRequest } from "next/server"

// Sends visitors without a session cookie to the sign-in page. It only checks that the cookie
// exists; the layout validates the session, and every action authenticates for itself.
export function proxy(request: NextRequest) {
  if (!getSessionCookie(request)) {
    return NextResponse.redirect(new URL("/login", request.url))
  }
  return NextResponse.next()
}

export const config = {
  matcher: [
    "/patient/:path*",
    "/clinician/:path*",
    "/researcher/:path*",
    "/settings/:path*",
  ],
}
