import "server-only"

import type { z } from "zod"

import { getAccessToken } from "@/lib/auth"

// The only module that talks to the .NET API. It runs on the server (Server Components and
// Server Actions) and adds the signed-in user's token, so the browser never calls the API and
// never sees a credential.

export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly title: string,
    readonly detail?: string,
    readonly errors?: Record<string, string[]>
  ) {
    super(detail ? `${title} ${detail}` : title)
    this.name = "ApiError"
  }
}

function baseUrl(): string {
  const url = process.env.API_BASE_URL
  if (!url) {
    throw new Error("API_BASE_URL is not configured.")
  }
  return url.replace(/\/$/, "")
}

async function toApiError(response: Response): Promise<ApiError> {
  try {
    const problem = (await response.json()) as {
      title?: string
      detail?: string
      errors?: Record<string, string[]>
    }
    return new ApiError(
      response.status,
      problem.title ?? response.statusText,
      problem.detail,
      problem.errors
    )
  } catch {
    return new ApiError(response.status, response.statusText)
  }
}

async function send(
  method: "GET" | "POST" | "DELETE",
  path: string,
  body?: unknown
): Promise<Response> {
  const token = await getAccessToken()
  const response = await fetch(`${baseUrl()}${path}`, {
    method,
    headers: {
      Accept: "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(body === undefined ? {} : { "Content-Type": "application/json" }),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
    // Patient data must never be served from a shared cache.
    cache: "no-store",
  })

  if (!response.ok) {
    throw await toApiError(response)
  }
  return response
}

export async function apiGet<T>(
  path: string,
  schema: z.ZodType<T>
): Promise<T> {
  const response = await send("GET", path)
  return schema.parse(await response.json())
}

export async function apiPost<T>(
  path: string,
  body: unknown,
  schema: z.ZodType<T>
): Promise<T> {
  const response = await send("POST", path, body)
  return schema.parse(await response.json())
}

export async function apiDelete(path: string): Promise<void> {
  await send("DELETE", path)
}

export function isNotFound(error: unknown): boolean {
  return error instanceof ApiError && error.status === 404
}

export function isForbidden(error: unknown): boolean {
  return error instanceof ApiError && error.status === 403
}
