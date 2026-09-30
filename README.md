# Patient Portal

Patients see their lab results and decide which clinics and researchers can see them. Clinicians see only patients they
treat or who have shared with their organization. Every read, refusal and consent change goes into an append-only audit
trail that patients can read.

## Run it

Requires Docker.

```sh
cp .env.example .env      # development-only values; change nothing to try it out
docker compose up --build
```

- Web app: <http://localhost:3000> (the sign-in page lists the demo accounts and their shared password)
- Keycloak admin console: <http://localhost:8080> (`KEYCLOAK_ADMIN_USER` / `KEYCLOAK_ADMIN_PASSWORD` from `.env`)

The API and Postgres publish no host ports on purpose; only the web app and Keycloak are reachable from the host.

## How it fits together

```
browser ──► web (Next.js BFF) ──► api (.NET minimal API) ──► postgres
   │             │  ▲ bearer token
   └─► keycloak ◄┘  └─ tokens stay on the server; the browser holds an opaque session cookie
```

- **`web/`**: server components and server actions do all fetching; the browser never calls the API or sees a token.
- **`api/`**: a pure Core class library, one file per endpoint, Dapper on Postgres, JWT validation and .NET authorization.
  `PatientPortal.Migrations` owns the schema (dbup) and the dev seed.
- **`keycloak/`**: the imported realm, with roles and demo users.

## Checks

```sh
cd api && dotnet csharpier check . && dotnet test PatientPortal.slnx   # integration tests start Postgres with Testcontainers
cd web && pnpm lint && pnpm typecheck && pnpm format:check && pnpm test && pnpm build
cd web && pnpm exec playwright install chromium && pnpm test:e2e   # browser journeys against a running stack
```

## Decisions

- [0001 Core architecture decisions](docs/adr/0001-architecture-decisions.md)
- [0002 Schema and database roles](docs/adr/0002-schema-and-database-roles.md)
- [0003 Authentication and authorization](docs/adr/0003-authentication-and-authorization.md)
