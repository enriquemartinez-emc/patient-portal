-- A refused attempt to read a patient's records is recorded too. It names the actor and the
-- patient, and carries no lab results and no consent.
ALTER TABLE audit_log DROP CONSTRAINT audit_log_action_check;
ALTER TABLE audit_log ADD CONSTRAINT audit_log_action_check
    CHECK (action IN ('lab_results_read', 'consent_granted', 'consent_revoked', 'access_denied'));

ALTER TABLE audit_log DROP CONSTRAINT audit_log_check;
ALTER TABLE audit_log ADD CONSTRAINT audit_log_action_shape_check CHECK (
    (action = 'lab_results_read' AND consent_grant_id IS NULL)
    OR (action = 'access_denied'
        AND consent_grant_id IS NULL
        AND cardinality(lab_result_ids) = 0)
    OR (action IN ('consent_granted', 'consent_revoked')
        AND consent_grant_id IS NOT NULL
        AND cardinality(lab_result_ids) = 0)
);
