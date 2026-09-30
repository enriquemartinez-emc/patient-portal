# ADR 0002: Schema and database roles

Status: accepted

## Decisions

- **Two database identities.** The migration user (the bootstrap user in local compose, `neondb_owner` on Neon) owns
  the schema and runs dbup. The API connects as `patient_portal_app`, which cannot drop tables or touch `audit_log`.
  There is no separate migrator role.
- **The app role's password is not in versioned SQL.** `0003_app_role.sql` creates the role without a password, so it
  cannot log in. `MigrationRunner` then sets the password from `APP_ROLE_PASSWORD` (optional; when unset the existing
  password is kept). The value travels as a session setting and is quoted server-side, so it never lands in a script
  and can contain quotes. Re-running with a new value rotates the password.
- **Least privilege for `patient_portal_app`.** It has no `DELETE` anywhere. Reference data (organizations, people, lab results)
  is `SELECT` only. `treatment_relationships` and `consent_grants` allow `SELECT` and `INSERT`, plus `UPDATE` on
  `ended_at` and `revoked_at` respectively. `audit_log` allows `SELECT` and `INSERT` only.
- **Append-only audit log, twice.** Missing privileges stop `patient_portal_app`; a `BEFORE UPDATE OR DELETE` row trigger and a
  `BEFORE TRUNCATE` statement trigger stop every other role, including the owner and roles that were granted the
  privileges. The owner can still drop the triggers, so they are defense in depth, not the primary control.
- **Expiry is derived, revocation is stored.** `consent_grants` stores `expires_at` and `revoked_at`. Core classifies a
  loaded grant as active, revoked or expired against the clock (`ConsentRules.ApplyExpiry`), so no job has to flip
  a status, and a consent expires exactly at its expiry instant.
- **Overlapping grants are allowed.** A patient may hold several grants to one organization. A partial unique index
  cannot say "no other grant in effect right now" because expiry is derived, so access is the union of grants
  currently in effect instead.
- **One audit row per read request.** `lab_results_read` stores the ids of every result returned in `lab_result_ids`,
  and an empty read is still recorded. `actor_id` is polymorphic (`actor_kind` names the table) and has no foreign key.
- **Text plus `CHECK` instead of Postgres enum types** for `kind`, `category`, `action` and `actor_kind`, so a new
  value is a normal migration. Primary keys default to `uuidv7()` (Postgres 18).
- **Dev seed is separate.** Scripts under `Scripts/DevSeed` only run when `MIGRATIONS_INCLUDE_DEV_SEED=true`, after
  all schema scripts (dbup run groups).

## Deploying to Neon

- **Create the app role with SQL, never through the Neon console, CLI or API.** Roles made there join
  `neon_superuser` (`CREATEDB`, `CREATEROLE`, `BYPASSRLS`, `REPLICATION`); roles created with SQL start with basic
  privileges only. `0003_app_role.sql` already does this.
- **Two connection strings.** Migrations use the direct (non-pooled) endpoint as the owner role. The API uses the pooled
  (`-pooler`) endpoint as `patient_portal_app`. Both need TLS (`SSL Mode=Require` for Npgsql).
- **PgBouncer runs in transaction mode**, so API code must not depend on session state: no `SET`, no session-level
  advisory locks, no temp tables, no `LISTEN`/`NOTIFY`. Use transaction-scoped locks (`SELECT ... FOR UPDATE`,
  `pg_advisory_xact_lock`) and keep Npgsql auto-prepare off.
- **Neon password rules**: minimum 60 bits of entropy, so the dev placeholder in `.env.example` is local-only.
- Postgres 18 is supported on Neon, so `uuidv7()` defaults apply. Not yet tried against a real Neon project.
- Row-level security is not used: authorization is decided in the API (see ADR 0001), and the API uses one shared
  database role rather than one per user.
