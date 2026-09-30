## What this is

A GDPR-hardened patient portal. Patients view lab results and
control which clinics/researchers can access their records. Clinicians
view records only for patients they treat or have been granted consent
for. Every access is logged in an append-only audit trail patients can
also see.

## Tech stack

- **Frontend (BFF)**: Next.js (App Router), Functional Core / Imperative
  Shell split (`core/` pure; `features/` and `lib/api` do I/O — see
  "Web layout"), shadcn/ui for components
- **Backend API**: .NET 10 minimal API, FCIS (pure Core class library +
  one use-case file per endpoint holding Request/Response, endpoint, handler
  orchestration, SQL and file-scoped row DTOs — see `emc-fcis-feature-slice`
  skill)
- **Auth**: Keycloak (OIDC), Better-Auth on the
  Next.js side
- **Database**: PostgreSQL 18
- **Orchestration**: Docker Compose

## Architecture principles — do not violate these

- **BFF token isolation**: the browser never receives the Keycloak
  access token. All calls to the .NET API go through Next.js Server
  Components or Server Actions, never a client-side fetch.
- **Resource-based authorization, not role-based**: "is this user a
  clinician" is not sufficient. Every clinician read of a patient's
  data must be checked against an active `TreatmentRelationship` or
  `ConsentGrant` for that _specific_ patient.
- **Append-only audit log**: every `LabResult` read by a clinician and
  every consent action writes an `AuditLogEntry. The audit table has no
`UPDATE`/`DELETE` grants at the database permission level.
- **FCIS on both sides**: pure functions (domain logic, view models,
  permission checks) stay free of I/O; a thin imperative shell
  (Server Actions, .NET Handlers) does all fetching/DB/HTTP.

## Repository structure

```
/web          — Next.js app (BFF)
/api          — .NET 10 minimal API
/keycloak     — realm-export.json (roles, clients, seed users)
docker-compose.yml
```

## Web layout (`/web`, no `src/` folder)

Follows the `emc-fcis-nextjs-feature-slice` skill. The .NET API plays the role
of the database, so `repository.ts` files call it instead of querying SQL.

- `app/` — thin routing layer: renders a feature's components, holds no
  business logic.
- `core/{feature}/` — functional core: `{feature}.types.ts` (discriminated
  unions) and `{feature}.rules.ts` (pure functions). No React, no I/O, no
  third-party dependencies, no imports from `app/`, `features/` or `lib/`
  (ESLint-enforced). Pass `now` in; never read the clock.
- `features/{feature}/` — imperative shell: `repository.ts` (calls to the .NET
  API, no business rules), `actions.ts` (`'use server'`: parse untrusted input
  with Zod, load via the repository, narrow into the Core type, call Core,
  persist), and `components/`.
- `features/auth/` — `session.ts` (`getSession`, `requireSession` for actions,
  `requireSessionPage` for pages) and `actions.ts` (sign in/out). Server
  Actions are public endpoints: every action that changes data calls
  `requireSession()` itself, never relying on the layout or `proxy.ts`.
  `getSession` is wrapped in React `cache()` so one request resolves it once.
- `lib/api/server.ts` — the only module that may call `fetch`; `server-only`;
  every `repository.ts` goes through it. The browser never calls the API.
- `proxy.ts` — only redirects requests without a session cookie to `/login`.
  It is not an authorization check.
- `components/ui/` — shared shadcn primitives.
- Until Keycloak and Better-Auth are added, sign-in uses demo accounts
  (`features/auth/demo-accounts.ts`, enabled by `ENABLE_DEMO_AUTH`).

## Available skills

- `emc-fcis-feature-slice` — use for any .NET endpoint business
  logic: pure Core plus use-case files (no Data/Mapper classes), immutable
  domain records
- `emc-pragmatic-type-driven-domain-modeling` — use when modeling new C# domain
  types, especially status/lifecycle fields (prefer sum types over
  enums/booleans for state)
- `emc-fcis-nextjs-feature-slice` - use when building a feature inside
  the Nextjs application
- `emc-endpoint-style-minimal-api` - use when adding or changing endpoint routes,
  HTTP contracts, validation, OpenAPI, or resource creation responses
- `neon-postgres` - use when working with postgresql
- `vercel-react-best-practices` - use when working with Nextjs
- `shadcn` - use when using shadcn ui components, use the latest haiku model to work with this skill.

## Known gotchas

- Keycloak must be reached at `http://localhost:8080` by the browser
  and `http://keycloak:8080` internally — `KC_HOSTNAME=localhost` keeps
  the issuer consistent regardless of which path a request took.
- `api` and `postgres` have no `ports:` entry in Docker Compose — they
  are unreachable from the host by design. Don't "fix" this by adding one.

## Quality

- Unit-test pure Core behavior. Integration-test API/database behavior with Testcontainers PostgreSQL.
- Use xUnit built-ins. Do not add Moq, FluentAssertions, or Should-style assertion libraries.
- Test behavior, concurrency invariants, and state transitions.
- After C# changes, run `dotnet csharpier format .`.
- After frontend changes, run prettier.
- Use Conventional Commits for completed changes.
- Use playwright cli skill only when approved.
- Never read, modify or do any type of manipulation of confidential information like secret or private keys.
- Format Nextjs .ts/tsx files after every change.

## Git Commit Guidelines

- Never include AI attribution, co-authorship tags, or session links in git commits.
- Keep commit messages concise, descriptive, and strictly attributed to the local git author.
- Use conventional commits always.
