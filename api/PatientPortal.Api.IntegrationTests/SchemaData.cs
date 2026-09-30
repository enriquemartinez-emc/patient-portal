using Dapper;
using Npgsql;

namespace PatientPortal.Api.IntegrationTests;

// Inserts through the owner connection so tests can arrange state the app role could not create.
internal static class SchemaData
{
    public static async Task<Guid> InsertPatientAsync(NpgsqlConnection owner) =>
        await owner.ExecuteScalarAsync<Guid>(
            "INSERT INTO patients (full_name, date_of_birth) VALUES ('Test Patient', '1990-01-01') RETURNING id"
        );

    public static async Task<Guid> InsertOrganizationAsync(
        NpgsqlConnection owner,
        string kind = "clinic"
    ) =>
        await owner.ExecuteScalarAsync<Guid>(
            "INSERT INTO organizations (name, kind) VALUES ('Test Org', @kind) RETURNING id",
            new { kind }
        );

    public static async Task<Guid> InsertClinicianAsync(
        NpgsqlConnection owner,
        Guid organizationId
    ) =>
        await owner.ExecuteScalarAsync<Guid>(
            "INSERT INTO clinicians (organization_id, full_name) VALUES (@organizationId, 'Test Clinician') RETURNING id",
            new { organizationId }
        );

    public static async Task<Guid> InsertConsentAsync(
        NpgsqlConnection connection,
        Guid patientId,
        Guid granteeId
    ) =>
        await connection.ExecuteScalarAsync<Guid>(
            """
            INSERT INTO consent_grants (patient_id, grantee_organization_id, categories, purpose, granted_at)
            VALUES (@patientId, @granteeId, ARRAY['hematology'], 'Test purpose', now())
            RETURNING id
            """,
            new { patientId, granteeId }
        );

    public static async Task<Guid> InsertAuditEntryAsync(
        NpgsqlConnection connection,
        Guid patientId,
        Guid actorId
    ) =>
        await connection.ExecuteScalarAsync<Guid>(
            """
            INSERT INTO audit_log (actor_kind, actor_id, patient_id, action, lab_result_ids)
            VALUES ('clinician', @actorId, @patientId, 'lab_results_read', ARRAY[gen_random_uuid()])
            RETURNING id
            """,
            new { patientId, actorId }
        );

    public static async Task<Guid> InsertLabResultAsync(
        NpgsqlConnection owner,
        Guid patientId,
        string category,
        string testName,
        DateTimeOffset collectedAt
    ) =>
        await owner.ExecuteScalarAsync<Guid>(
            """
            INSERT INTO lab_results (patient_id, category, test_name, value_amount, value_unit, collected_at)
            VALUES (@patientId, @category, @testName, 4.2, 'mmol/L', @collectedAt)
            RETURNING id
            """,
            new
            {
                patientId,
                category,
                testName,
                collectedAt,
            }
        );

    public static async Task<Guid> InsertNamedOrganizationAsync(
        NpgsqlConnection owner,
        string name
    ) =>
        await owner.ExecuteScalarAsync<Guid>(
            "INSERT INTO organizations (name, kind) VALUES (@name, 'clinic') RETURNING id",
            new { name }
        );

    public static async Task<Guid> InsertNamedClinicianAsync(
        NpgsqlConnection owner,
        Guid organizationId,
        string name
    ) =>
        await owner.ExecuteScalarAsync<Guid>(
            "INSERT INTO clinicians (organization_id, full_name) VALUES (@organizationId, @name) RETURNING id",
            new { organizationId, name }
        );

    public static async Task<Guid> InsertConsentAtAsync(
        NpgsqlConnection owner,
        Guid patientId,
        Guid granteeId,
        DateTimeOffset grantedAt,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? revokedAt = null
    ) =>
        await owner.ExecuteScalarAsync<Guid>(
            """
            INSERT INTO consent_grants (patient_id, grantee_organization_id, categories, purpose, granted_at, expires_at, revoked_at)
            VALUES (@patientId, @granteeId, ARRAY['lipids'], 'Seeded purpose', @grantedAt, @expiresAt, @revokedAt)
            RETURNING id
            """,
            new
            {
                patientId,
                granteeId,
                grantedAt,
                expiresAt,
                revokedAt,
            }
        );

    public static async Task<Guid> InsertAuditEntryAtAsync(
        NpgsqlConnection owner,
        Guid patientId,
        string actorKind,
        Guid actorId,
        DateTimeOffset occurredAt
    ) =>
        await owner.ExecuteScalarAsync<Guid>(
            """
            INSERT INTO audit_log (occurred_at, actor_kind, actor_id, patient_id, action)
            VALUES (@occurredAt, @actorKind, @actorId, @patientId, 'lab_results_read')
            RETURNING id
            """,
            new
            {
                occurredAt,
                actorKind,
                actorId,
                patientId,
            }
        );
}
