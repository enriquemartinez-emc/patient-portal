# ADR 0003: Authentication and authorization

Status: accepted

## Decisions

- **Keycloak is the identity provider.** The browser signs in on Keycloak's page; nothing in the portal handles passwords.
- **The API is a stateless resource server.** It validates the bearer token with the first-party JwtBearer handler
  (issuer, audience `portal-api`, signature) and reads Keycloak's realm roles as ordinary roles. Keycloak is reached at
  `keycloak:8080` inside the network and `localhost:8080` from the browser; `KC_HOSTNAME` fixes the issuer and
  `KC_HOSTNAME_BACKCHANNEL_DYNAMIC` lets the signing-key URL follow the caller.
- **Authorization is .NET policy, not Core.** Two layers:
  - Route groups require a role and that the caller is the person named in the URL (`/patients/{id}`,
    `/clinicians/{id}`, `/researchers/{id}`), matched through `external_subject_id`. An id that is not the caller's is
    403 whether or not it exists.
  - A resource-based handler decides whether a clinician or researcher may read a specific patient's results and which
    categories they see: treatment shows everything, consent shows what it covers. It runs inside the read's
    transaction and locks the rows it relies on (`FOR SHARE`), so a revoke cannot land between the decision and the read.
- **Refusals are audited.** A refused read is recorded in the patient's trail as `access_denied`, on its own
  transaction so it survives the rollback of the read.
- **`GET /me` links a login to its record.** API routes are keyed by patient, clinician or researcher id, which the web
  app learns from `/me` after sign-in. A login with no record (an admin, for instance) has no portal access.
- **The web app is a BFF.** Better-Auth is the OIDC client. Sessions and Keycloak's tokens live in Postgres (`web_*`
  tables, written as `patient_portal_web`, a role that can reach nothing else) and the browser holds only an opaque
  session cookie. Access and refresh tokens are encrypted at rest; the ID token is not, and the API rejects it because its
  audience is the web client. The web server refreshes the access token when it expires.
- **`proxy.ts` only redirects** requests without a session cookie. Layouts validate the session, and every server action
  authenticates for itself.
- **The demo accounts on the sign-in page are development data.** Keycloak holds the real accounts.
