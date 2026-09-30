-- actor_id is polymorphic (patient, clinician or researcher, named by actor_kind), so it
-- carries no foreign key; the audit trail must never be blocked by a change to a person row.
CREATE TABLE audit_log (
    id               uuid PRIMARY KEY DEFAULT uuidv7(),
    occurred_at      timestamptz NOT NULL DEFAULT now(),
    actor_kind       text NOT NULL CHECK (actor_kind IN ('patient', 'clinician', 'researcher')),
    actor_id         uuid NOT NULL,
    patient_id       uuid NOT NULL REFERENCES patients (id),
    action           text NOT NULL CHECK (action IN ('lab_results_read', 'consent_granted', 'consent_revoked')),
    lab_result_ids   uuid[] NOT NULL DEFAULT '{}',
    consent_grant_id uuid REFERENCES consent_grants (id),
    CHECK (
        (action = 'lab_results_read' AND consent_grant_id IS NULL)
        OR (action IN ('consent_granted', 'consent_revoked')
            AND consent_grant_id IS NOT NULL
            AND cardinality(lab_result_ids) = 0)
    )
);
CREATE INDEX audit_log_patient_occurred_idx ON audit_log (patient_id, occurred_at DESC, id DESC);
CREATE INDEX audit_log_consent_grant_id_idx ON audit_log (consent_grant_id) WHERE consent_grant_id IS NOT NULL;

-- Defense in depth: the application role has no UPDATE/DELETE/TRUNCATE privilege (see 0003),
-- and these triggers reject the operations for every other role as well.
CREATE FUNCTION audit_log_reject_mutation() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'audit_log is append-only: % is not permitted', TG_OP
        USING ERRCODE = 'insufficient_privilege';
END;
$$;

CREATE TRIGGER audit_log_no_update_delete
    BEFORE UPDATE OR DELETE ON audit_log
    FOR EACH ROW EXECUTE FUNCTION audit_log_reject_mutation();

CREATE TRIGGER audit_log_no_truncate
    BEFORE TRUNCATE ON audit_log
    FOR EACH STATEMENT EXECUTE FUNCTION audit_log_reject_mutation();
