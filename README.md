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

From your machine you can reach the web app (3000), Keycloak (8080) and Postgres (5432, loopback only). The API is reachable only inside Docker.

## Using the app

Open <http://localhost:3000> and choose **Sign in**. You sign in on the Keycloak page with one of the demo accounts
below (the sign-in page lists them too). They all use the password `demo-password`.

| Account | Role | What they can do |
| --- | --- | --- |
| `emily.carter@demo.example` | Patient | Has 4 lab results. Shares hematology and lipids with Riverside Clinic. |
| `james.wilson@demo.example` | Patient | Has 4 lab results. Shares biochemistry with Meridian Research Institute. |
| `sarah.thompson@demo.example` | Clinician, Northside Clinic | Treats Emily, so sees all her results. |
| `michael.brown@demo.example` | Clinician, Riverside Clinic | Treats James. Sees only what Emily shared with his clinic. |
| `laura.davies@demo.example` | Researcher, Meridian Research Institute | Sees James as an anonymous participant, biochemistry only. |

### A tour

Use a private window for the second person: Keycloak keeps one sign-in per browser, so signing in as someone else in the
same window replaces the first.

1. **As Emily**: the dashboard summarises her results, who can see them and who has opened them. _Lab results_ lists
   them by kind of test; select a test to open it.
2. **Consents** (still as Emily): under _Share your results_, choose _Meridian Research Institute_, tick _Lipids_, give a
   reason, and optionally set a date to stop sharing. Select **Share results**. It appears under _Active_.
3. **As Laura** (private window): _Participants_ now has a second anonymous participant with only _Lipids_. That is
   Emily; the other, _Participant 00000002_, is James. Open her to see just those results. Names are never shown to
   researchers.
4. **As Sarah**: _Patients_ shows Emily with "You treat this patient". Open her: all four categories are there, with
   a notice that the access is recorded.
5. **Back as Emily**: _Access history_ now shows "Dr. Sarah Thompson (Northside Clinic) viewed 4 lab results" and Laura's
   view. Use the _Show_ filter to narrow it by type.
6. **As Michael**: Emily appears as "Shared with your organization". Open her: only _Hematology_ and _Lipids_,
   because that is all she shared with his clinic.
7. **Revoke** (as Emily): on _Consents_, select **Revoke access** on the Meridian consent and confirm. Laura loses access
   at once, and the revoke is in Emily's access history.

`admin@demo.example` signs in but has no patient, clinician or researcher record, so it sees a "no portal access" page.
It exists for managing treatment relationships through the API.

The menu at the top right switches the theme (light, dark or system), opens Settings (empty for now) and logs out.

### Starting over

`docker compose down -v` stops everything and deletes the data. The next `docker compose up --build` re-creates the
database with the demo data and re-imports the Keycloak realm. `docker compose down` alone keeps the data.

## Develop

Postgres and Keycloak run in Docker, and the API and web app run on your machine. Changes show up as you save, and you
can debug from your IDE. One command starts all of it:

```sh
./dev.sh          # start everything, Ctrl+C stops the API and web app
./dev.sh down     # also stop Postgres and Keycloak
./dev.sh reset    # stop everything and delete the database
```

It needs Docker, the .NET SDK and pnpm. On the first run it creates `.env` and `web/.env.local` from the examples,
installs the web dependencies, and applies the migrations. Then open <http://localhost:3000> and sign in as usual.

- The API's local settings are in `api/PatientPortal.Api/appsettings.Development.json` and the web app's are in
  `web/.env.local`. Both hold the same development-only values as `.env.example`, so change them together.
- To debug, start the API from your IDE with the `http` launch profile and run `pnpm dev` under your IDE's Node
  debugger. `./dev.sh` re-runs the migrations each time, so it also picks up a new migration.
- `./dev.sh` stops the containerised web app and API if they are running, because they use the same ports.

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
