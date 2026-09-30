-- The migration user owns the schema. The application connects as pp_app, which is
-- limited to what each table's behavior needs and never gets DELETE anywhere.
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'pp_app') THEN
        CREATE ROLE pp_app LOGIN PASSWORD '$AppPassword$';
    ELSE
        ALTER ROLE pp_app LOGIN PASSWORD '$AppPassword$';
    END IF;
END;
$$;

GRANT USAGE ON SCHEMA public TO pp_app;

GRANT SELECT ON organizations, patients, clinicians, researchers, lab_results TO pp_app;

GRANT SELECT, INSERT ON treatment_relationships TO pp_app;
GRANT UPDATE (ended_at) ON treatment_relationships TO pp_app;

GRANT SELECT, INSERT ON consent_grants TO pp_app;
GRANT UPDATE (revoked_at) ON consent_grants TO pp_app;

GRANT SELECT, INSERT ON audit_log TO pp_app;
