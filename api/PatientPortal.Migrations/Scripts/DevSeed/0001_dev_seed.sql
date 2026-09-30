-- Development-only sample data with fixed ids so tests and the UI can refer to it. The subject ids
-- match the users in keycloak/realm-export.json.
INSERT INTO organizations (id, name, kind) VALUES
    ('a0000000-0000-0000-0000-000000000001', 'Northside Clinic', 'clinic'),
    ('a0000000-0000-0000-0000-000000000002', 'Riverside Clinic', 'clinic'),
    ('a0000000-0000-0000-0000-000000000003', 'Meridian Research Institute', 'research_institution');

INSERT INTO patients (id, full_name, date_of_birth, external_subject_id) VALUES
    ('b0000000-0000-0000-0000-000000000001', 'Emily Carter', '1984-03-12', '90000000-0000-0000-0000-000000000001'),
    ('b0000000-0000-0000-0000-000000000002', 'James Wilson', '1971-11-02', '90000000-0000-0000-0000-000000000002');

INSERT INTO clinicians (id, organization_id, full_name, external_subject_id) VALUES
    ('c0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000001', 'Dr. Sarah Thompson', '90000000-0000-0000-0000-000000000003'),
    ('c0000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000002', 'Dr. Michael Brown', '90000000-0000-0000-0000-000000000004');

INSERT INTO researchers (id, organization_id, full_name, external_subject_id) VALUES
    ('d0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000003', 'Dr. Laura Davies', '90000000-0000-0000-0000-000000000005');

-- Sarah Thompson treats Emily; Michael Brown treats James; Sarah used to treat James.
INSERT INTO treatment_relationships (id, patient_id, clinician_id, started_at, ended_at) VALUES
    ('f0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', '2025-01-10T09:00:00Z', NULL),
    ('f0000000-0000-0000-0000-000000000002', 'b0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000002', '2025-03-05T09:00:00Z', NULL),
    ('f0000000-0000-0000-0000-000000000003', 'b0000000-0000-0000-0000-000000000002', 'c0000000-0000-0000-0000-000000000001', '2023-06-01T09:00:00Z', '2024-12-01T09:00:00Z');

-- Emily shares hematology and lipids with Riverside Clinic; James shares biochemistry with the research institute.
INSERT INTO consent_grants (id, patient_id, grantee_organization_id, categories, purpose, granted_at, expires_at, revoked_at) VALUES
    ('e0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000002', ARRAY['hematology', 'lipids'], 'Second opinion on blood work', '2026-01-15T10:00:00Z', NULL, NULL),
    ('e0000000-0000-0000-0000-000000000002', 'b0000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000003', ARRAY['biochemistry'], 'Metabolic health study', '2026-02-01T10:00:00Z', '2030-02-01T10:00:00Z', NULL);

INSERT INTO lab_results (id, patient_id, category, test_name, value_amount, value_unit, collected_at) VALUES
    ('10000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000001', 'hematology', 'Hemoglobin', 13.4, 'g/dL', '2026-05-02T08:30:00Z'),
    ('10000000-0000-0000-0000-000000000002', 'b0000000-0000-0000-0000-000000000001', 'lipids', 'LDL cholesterol', 118, 'mg/dL', '2026-05-02T08:30:00Z'),
    ('10000000-0000-0000-0000-000000000003', 'b0000000-0000-0000-0000-000000000001', 'endocrinology', 'TSH', 2.1, 'mIU/L', '2026-05-02T08:30:00Z'),
    ('10000000-0000-0000-0000-000000000004', 'b0000000-0000-0000-0000-000000000001', 'biochemistry', 'Creatinine', 0.9, 'mg/dL', '2026-08-14T08:00:00Z'),
    ('10000000-0000-0000-0000-000000000005', 'b0000000-0000-0000-0000-000000000002', 'biochemistry', 'HbA1c', 5.8, '%', '2026-06-20T07:45:00Z'),
    ('10000000-0000-0000-0000-000000000006', 'b0000000-0000-0000-0000-000000000002', 'biochemistry', 'Fasting glucose', 102, 'mg/dL', '2026-06-20T07:45:00Z'),
    ('10000000-0000-0000-0000-000000000007', 'b0000000-0000-0000-0000-000000000002', 'hematology', 'Hemoglobin', 14.8, 'g/dL', '2026-06-20T07:45:00Z'),
    ('10000000-0000-0000-0000-000000000008', 'b0000000-0000-0000-0000-000000000002', 'urinalysis', 'Urine protein', 0, 'mg/dL', '2026-06-20T07:45:00Z');
