CREATE TABLE organizations (
    id   uuid PRIMARY KEY DEFAULT uuidv7(),
    name text NOT NULL CHECK (length(btrim(name)) > 0),
    kind text NOT NULL CHECK (kind IN ('clinic', 'research_institution'))
);

CREATE TABLE patients (
    id            uuid PRIMARY KEY DEFAULT uuidv7(),
    full_name     text NOT NULL CHECK (length(btrim(full_name)) > 0),
    date_of_birth date NOT NULL
);

CREATE TABLE clinicians (
    id              uuid PRIMARY KEY DEFAULT uuidv7(),
    organization_id uuid NOT NULL REFERENCES organizations (id),
    full_name       text NOT NULL CHECK (length(btrim(full_name)) > 0)
);
CREATE INDEX clinicians_organization_id_idx ON clinicians (organization_id);

CREATE TABLE researchers (
    id              uuid PRIMARY KEY DEFAULT uuidv7(),
    organization_id uuid NOT NULL REFERENCES organizations (id),
    full_name       text NOT NULL CHECK (length(btrim(full_name)) > 0)
);
CREATE INDEX researchers_organization_id_idx ON researchers (organization_id);

CREATE TABLE treatment_relationships (
    id           uuid PRIMARY KEY DEFAULT uuidv7(),
    patient_id   uuid NOT NULL REFERENCES patients (id),
    clinician_id uuid NOT NULL REFERENCES clinicians (id),
    started_at   timestamptz NOT NULL,
    ended_at     timestamptz,
    CHECK (ended_at IS NULL OR ended_at >= started_at)
);
CREATE INDEX treatment_relationships_patient_id_idx ON treatment_relationships (patient_id);
-- A clinician has at most one active relationship with a given patient.
CREATE UNIQUE INDEX treatment_relationships_one_active_idx
    ON treatment_relationships (patient_id, clinician_id)
    WHERE ended_at IS NULL;
-- "My patients" lookup for a clinician.
CREATE INDEX treatment_relationships_active_by_clinician_idx
    ON treatment_relationships (clinician_id)
    WHERE ended_at IS NULL;

-- A patient may hold several grants to the same organization (for example different
-- scopes); access is the union of the grants currently in effect. Expiry is derived
-- from expires_at and the clock, so only revocation is a stored state change.
CREATE TABLE consent_grants (
    id                     uuid PRIMARY KEY DEFAULT uuidv7(),
    patient_id             uuid NOT NULL REFERENCES patients (id),
    grantee_organization_id uuid NOT NULL REFERENCES organizations (id),
    categories             text[] NOT NULL CHECK (
                               cardinality(categories) > 0
                               AND categories <@ ARRAY['hematology', 'biochemistry', 'lipids',
                                   'endocrinology', 'immunology', 'microbiology', 'urinalysis']
                           ),
    purpose                text NOT NULL CHECK (length(btrim(purpose)) > 0),
    granted_at             timestamptz NOT NULL,
    expires_at             timestamptz,
    revoked_at             timestamptz,
    CHECK (expires_at IS NULL OR expires_at > granted_at),
    CHECK (revoked_at IS NULL OR revoked_at >= granted_at)
);
CREATE INDEX consent_grants_patient_id_idx ON consent_grants (patient_id);
CREATE INDEX consent_grants_unrevoked_by_grantee_idx
    ON consent_grants (grantee_organization_id)
    WHERE revoked_at IS NULL;

CREATE TABLE lab_results (
    id           uuid PRIMARY KEY DEFAULT uuidv7(),
    patient_id   uuid NOT NULL REFERENCES patients (id),
    category     text NOT NULL CHECK (category IN ('hematology', 'biochemistry', 'lipids',
                     'endocrinology', 'immunology', 'microbiology', 'urinalysis')),
    test_name    text NOT NULL CHECK (length(btrim(test_name)) > 0),
    value_amount numeric NOT NULL,
    value_unit   text NOT NULL CHECK (length(btrim(value_unit)) > 0),
    collected_at timestamptz NOT NULL
);
CREATE INDEX lab_results_patient_collected_idx ON lab_results (patient_id, collected_at DESC);
