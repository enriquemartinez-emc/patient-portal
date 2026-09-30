## What this is

A GDPR-hardened patient portal. Patients view lab results and
control which clinics/researchers can access their records. Clinicians
view records only for patients they treat or have been granted consent
for. Every access is logged in an append-only audit trail patients can
also see.

## Tech stack

- **Frontend (BFF)**: Next.js (App Router), Functional Core / Imperative
  Shell split (see `emc-fcis-nextjs-feature-slice` skill), shadcn/ui for
  components
- **Backend API**: .NET 10 minimal API, FCIS (pure Core class library +
  use-case files, no Data/Mapper classes — see `emc-fcis-feature-slice` skill)
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
- **FCIS on both sides**: pure functions (domain logic, view models)
  stay free of I/O; a thin imperative shell (Server Actions, .NET
  Handlers) does all fetching/DB/HTTP.

## Repository structure

```
/web          — Next.js app (BFF)
/api          — .NET 10 minimal API
/keycloak     — realm-export.json (roles, clients, seed users)
docker-compose.yml
```

## Available skills

The `emc-` skills are authoritative: always follow them for code structure, FCIS and endpoints.

- `emc-fcis-feature-slice` — use for any .NET endpoint business
  logic: pure Core plus use-case files, immutable domain records
- `emc-pragmatic-type-driven-domain-modeling` — use when modeling new C# domain
  types, especially status/lifecycle fields (prefer sum types over
  enums/booleans for state)
- `emc-fcis-nextjs-feature-slice` - use when building a feature inside
  the Nextjs application
- `emc-endpoint-style-minimal-api` - use when adding or changing endpoint routes,
  HTTP contracts, validation, OpenAPI, or resource creation responses
- `neon-postgres` - use when working with postgresql
- `vercel-react-best-practices` - use when working with Nextjs
- `shadcn` - use when using shadcn ui components, use Sonnet 5.5 at low effort to work with this skill.

## Known gotchas

- Keycloak must be reached at `http://localhost:8080` by the browser
  and `http://keycloak:8080` internally — `KC_HOSTNAME=localhost` keeps
  the issuer consistent regardless of which path a request took.

## Quality

- Unit-test pure Core behavior. Integration-test API/database behavior with Testcontainers PostgreSQL.
- Use xUnit built-ins. Do not add Moq, FluentAssertions, or Should-style assertion libraries.
- Test behavior, concurrency invariants, and state transitions.
- After C# changes, run `dotnet csharpier format .`.
- After frontend changes, run prettier.
- Use Conventional Commits for completed changes.
- Use playwright cli skill only when approved.
- Run e2e (Playwright) tests only when the user commands it.
- For frontend changes, edit components and make all needed changes without asking. Ask for approval to verify with Playwright (tests or screenshots) only right before committing, not earlier.
- Never read, modify or do any type of manipulation of confidential information like secret or private keys.
- Format Nextjs .ts/tsx files after every change.

## Git Commit Guidelines

- Never include AI attribution, co-authorship tags, or session links in git commits.
- Keep commit messages concise, descriptive, and strictly attributed to the local git author.
