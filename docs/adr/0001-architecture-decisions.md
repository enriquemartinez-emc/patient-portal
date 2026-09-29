# ADR 0001: Core architecture decisions

Status: accepted

## Decisions

- **Core holds business rules only.** Authorization is infrastructure and uses the .NET authorization stack
  (JwtBearer, policies, resource-based handlers), not Core functions.
- **Explicit actor and subject ids in routes** (`/patients/{patientId}`, `/clinicians/{clinicianId}`,
  `/researchers/{researcherId}`). Once authentication exists, a resource handler checks the route id against the caller.
- **Audit atomicity.** Reads and their `AuditLogEntry` insert share one transaction.
- **Append-only audit log** enforced by Postgres permissions (`pp_app` has `INSERT, SELECT` only) plus a trigger.
- **Lifecycle state as sum types**, not enums or booleans.
- **BFF token isolation.** The browser never receives the Keycloak access token.
- **`api` and `postgres` publish no host ports.**
- **Build order:** application first, authentication and authorization afterwards.
