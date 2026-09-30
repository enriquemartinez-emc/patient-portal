# ADR 0002: Schema and database roles

Status: accepted

## Decisions

- **Two database identities.** The migration user (the bootstrap user in local compose) owns the schema and runs
  dbup. The application connects as `pp_app`, created by `0003_app_role.sql` with a password supplied through
  `PP_APP_PASSWORD`. There is no separate `pp_migrator` role.
- **Least privilege for `pp_app`.** It has no `DELETE` anywhere. Reference data (organizations, people, lab results)
  is `SELECT` only. `treatment_relationships` and `consent_grants` allow `SELECT` and `INSERT`, plus `UPDATE` on
  `ended_at` and `revoked_at` respectively. `audit_log` allows `SELECT` and `INSERT` only.
- **Append-only audit log, twice.** Missing privileges stop `pp_app`; a `BEFORE UPDATE OR DELETE` row trigger and a
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
