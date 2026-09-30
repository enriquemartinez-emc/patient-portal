-- Links a person to the identity provider account they sign in with (the token's `sub`).
-- A person without a login has no subject. A subject belongs to at most one person of each kind.
ALTER TABLE patients ADD COLUMN external_subject_id text;
ALTER TABLE clinicians ADD COLUMN external_subject_id text;
ALTER TABLE researchers ADD COLUMN external_subject_id text;

CREATE UNIQUE INDEX patients_external_subject_id_idx
    ON patients (external_subject_id) WHERE external_subject_id IS NOT NULL;
CREATE UNIQUE INDEX clinicians_external_subject_id_idx
    ON clinicians (external_subject_id) WHERE external_subject_id IS NOT NULL;
CREATE UNIQUE INDEX researchers_external_subject_id_idx
    ON researchers (external_subject_id) WHERE external_subject_id IS NOT NULL;
