using Dapper;
using Npgsql;

namespace PatientPortal.Api.Common;

// The organization a clinician or researcher works for. The caller has already been checked to be
// that person, so the record exists.
internal static class OrganizationLookup
{
    public static Task<Guid> OfClinicianAsync(
        NpgsqlConnection connection,
        Guid clinicianId,
        CancellationToken ct
    ) =>
        connection.QuerySingleAsync<Guid>(
            new CommandDefinition(
                "select organization_id from clinicians where id = @clinicianId",
                new { clinicianId },
                cancellationToken: ct
            )
        );

    public static Task<Guid> OfResearcherAsync(
        NpgsqlConnection connection,
        Guid researcherId,
        CancellationToken ct
    ) =>
        connection.QuerySingleAsync<Guid>(
            new CommandDefinition(
                "select organization_id from researchers where id = @researcherId",
                new { researcherId },
                cancellationToken: ct
            )
        );
}
