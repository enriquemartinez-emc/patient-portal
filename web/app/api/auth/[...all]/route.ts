import { toNextJsHandler } from "better-auth/next-js"

import { getAuth } from "@/lib/auth"

// Better-Auth's own endpoints: the OAuth callback Keycloak redirects the browser to, and session
// management. Built per request because the auth instance needs runtime configuration.
export function GET(request: Request) {
  return toNextJsHandler(getAuth()).GET(request)
}

export function POST(request: Request) {
  return toNextJsHandler(getAuth()).POST(request)
}
