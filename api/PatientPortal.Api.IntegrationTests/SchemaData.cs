using Dapper;
using Npgsql;

namespace PatientPortal.Api.IntegrationTests;

internal static class SchemaData
{
    // In tests a person's login subject is their own id, so a token for that id acts as them.
    public static async Task<Guid> InsertPatientAsync(NpgsqlConnection owner) =>
        await InsertNamedPatientAsync(owner, "Test Patient");

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
    ) => await InsertNamedClinicianAsync(owner, organizationId, "Test Clinician");

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
    )
    {
        var id = Guid.CreateVersion7();
        await owner.ExecuteAsync(
            "INSERT INTO clinicians (id, organization_id, full_name, external_subject_id) VALUES (@id, @organizationId, @name, @subject)",
            new
            {
                id,
                organizationId,
                name,
                subject = id.ToString(),
            }
        );
        return id;
    }

    public static async Task<Guid> InsertConsentAtAsync(
        NpgsqlConnection owner,
        Guid patientId,
        Guid granteeId,
        DateTimeOffset grantedAt,
        DateTimeOffset? expiresAt = null,
        DateTimeOffset? revokedAt = null,
        string[]? categories = null
    ) =>
        await owner.ExecuteScalarAsync<Guid>(
            """
            INSERT INTO consent_grants (patient_id, grantee_organization_id, categories, purpose, granted_at, expires_at, revoked_at)
            VALUES (@patientId, @granteeId, @categories, 'Seeded purpose', @grantedAt, @expiresAt, @revokedAt)
            RETURNING id
            """,
            new
            {
                patientId,
                granteeId,
                grantedAt,
                expiresAt,
                revokedAt,
                categories = categories ?? ["lipids"],
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

    public static async Task<Guid> InsertNamedPatientAsync(NpgsqlConnection owner, string name)
    {
        var id = Guid.CreateVersion7();
        await owner.ExecuteAsync(
            "INSERT INTO patients (id, full_name, date_of_birth, external_subject_id) VALUES (@id, @name, '1990-01-01', @subject)",
            new
            {
                id,
                name,
                subject = id.ToString(),
            }
        );
        return id;
    }

    public static async Task<Guid> InsertResearcherAsync(
        NpgsqlConnection owner,
        Guid organizationId
    )
    {
        var id = Guid.CreateVersion7();
        await owner.ExecuteAsync(
            "INSERT INTO researchers (id, organization_id, full_name, external_subject_id) VALUES (@id, @organizationId, 'Test Researcher', @subject)",
            new
            {
                id,
                organizationId,
                subject = id.ToString(),
            }
        );
        return id;
    }

    public static async Task<Guid> InsertTreatmentAsync(
        NpgsqlConnection owner,
        Guid patientId,
        Guid clinicianId,
        DateTimeOffset startedAt,
        DateTimeOffset? endedAt = null
    ) =>
        await owner.ExecuteScalarAsync<Guid>(
            """
            INSERT INTO treatment_relationships (patient_id, clinician_id, started_at, ended_at)
            VALUES (@patientId, @clinicianId, @startedAt, @endedAt)
            RETURNING id
            """,
            new
            {
                patientId,
                clinicianId,
                startedAt,
                endedAt,
            }
        );

    public static async Task<List<AuditReadRow>> AuditReadsAsync(
        NpgsqlConnection owner,
        Guid patientId
    ) =>
        (
            await owner.QueryAsync<AuditReadRow>(
                """
                SELECT actor_kind AS ActorKind, actor_id AS ActorId, action AS Action,
                       lab_result_ids AS LabResultIds, occurred_at AS OccurredAt
                FROM audit_log WHERE patient_id = @patientId ORDER BY occurred_at, id
                """,
                new { patientId }
            )
        ).ToList();
}

public sealed class AuditReadRow
{
    public string ActorKind { get; set; } = "";
    public Guid ActorId { get; set; }
    public string Action { get; set; } = "";
    public Guid[] LabResultIds { get; set; } = [];
    public DateTimeOffset OccurredAt { get; set; }
}
