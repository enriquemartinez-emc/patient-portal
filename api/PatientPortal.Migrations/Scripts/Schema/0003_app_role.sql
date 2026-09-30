-- The migration user owns the schema. The application connects as patient_portal_app, which is
-- limited to what each table's behavior needs and never gets DELETE anywhere.
--
-- The role is created without a password, so it cannot log in until the migration runner sets one
-- (see MigrationRunner). Keeping the secret out of versioned SQL lets each environment, including
-- Neon, supply its own password.
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'patient_portal_app') THEN
        CREATE ROLE patient_portal_app LOGIN;
    END IF;
END;
$$;

GRANT USAGE ON SCHEMA public TO patient_portal_app;

GRANT SELECT ON organizations, patients, clinicians, researchers, lab_results TO patient_portal_app;

GRANT SELECT, INSERT ON treatment_relationships TO patient_portal_app;
GRANT UPDATE (ended_at) ON treatment_relationships TO patient_portal_app;

GRANT SELECT, INSERT ON consent_grants TO patient_portal_app;
GRANT UPDATE (revoked_at) ON consent_grants TO patient_portal_app;

GRANT SELECT, INSERT ON audit_log TO patient_portal_app;
