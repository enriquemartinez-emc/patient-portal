import "server-only"

import { betterAuth } from "better-auth"
import { nextCookies } from "better-auth/next-js"
import { genericOAuth, keycloak } from "better-auth/plugins/generic-oauth"
import { headers } from "next/headers"
import { Pool } from "pg"
import { cache } from "react"

function required(name: string): string {
  const value = process.env[name]
  if (!value) {
    throw new Error(`${name} is not configured.`)
  }
  return value
}

// Created on first use, not at import: the environment is not available while `next build` runs.
function createAuth() {
  return betterAuth({
    baseURL: required("BETTER_AUTH_URL"),
    secret: required("BETTER_AUTH_SECRET"),
    database: new Pool({ connectionString: required("WEB_DATABASE_URL") }),
    user: {
      modelName: "web_users",
      fields: {
        emailVerified: "email_verified",
        createdAt: "created_at",
        updatedAt: "updated_at",
      },
    },
    session: {
      modelName: "web_sessions",
      fields: {
        expiresAt: "expires_at",
        createdAt: "created_at",
        updatedAt: "updated_at",
        ipAddress: "ip_address",
        userAgent: "user_agent",
        userId: "user_id",
      },
    },
    account: {
      modelName: "web_accounts",
      // Keycloak's tokens live only in this table, encrypted, and never reach the browser.
      encryptOAuthTokens: true,
      fields: {
        accountId: "account_id",
        providerId: "provider_id",
        userId: "user_id",
        accessToken: "access_token",
        refreshToken: "refresh_token",
        idToken: "id_token",
        accessTokenExpiresAt: "access_token_expires_at",
        refreshTokenExpiresAt: "refresh_token_expires_at",
        createdAt: "created_at",
        updatedAt: "updated_at",
      },
    },
    verification: {
      modelName: "web_verifications",
      fields: {
        expiresAt: "expires_at",
        createdAt: "created_at",
        updatedAt: "updated_at",
      },
    },
    plugins: [
      genericOAuth({
        config: [
          keycloak({
            clientId: "portal-web",
            clientSecret: required("KEYCLOAK_CLIENT_SECRET"),
            // The server reaches Keycloak at this internal address. The discovery document it
            // returns names the public address for the browser and for the token issuer.
            issuer: required("KEYCLOAK_ISSUER"),
            postLogoutRedirectURI: "/login",
          }),
        ],
      }),
      nextCookies(),
    ],
  })
}

let instance: ReturnType<typeof createAuth> | undefined

export function getAuth() {
  return (instance ??= createAuth())
}

export const getAccessToken = cache(async (): Promise<string | null> => {
  try {
    const requestHeaders = await headers()
    const accounts = await getAuth().api.listUserAccounts({
      headers: requestHeaders,
    })
    const account = accounts.find(
      (candidate) => candidate.providerId === "keycloak"
    )
    if (!account) {
      return null
    }

    const { accessToken } = await getAuth().api.getAccessToken({
      body: { accountId: account.id },
      headers: requestHeaders,
    })
    return accessToken ?? null
  } catch {
    return null
  }
})
