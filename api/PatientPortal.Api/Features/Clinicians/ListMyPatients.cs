using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using PatientPortal.Api.Common;

namespace PatientPortal.Api.Features.Clinicians;

public sealed record ListMyPatientsRequest(int Page = 1, int PageSize = PagingRules.DefaultPageSize)
    : IPagedRequest;

public sealed class ListMyPatientsValidator : PagedRequestValidator<ListMyPatientsRequest>;

public sealed record PatientSummaryResponse(
    Guid Id,
    string FullName,
    DateOnly DateOfBirth,
    IReadOnlyList<string> AccessBasis
);

public static class ListMyPatientsEndpoint
{
    public static void MapListMyPatientsEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("/patients", Handle)
            .WithValidation<ListMyPatientsRequest>()
            .WithName("ListMyPatients");

    // The expiry predicate mirrors ConsentRules.ApplyExpiry.
    private static async Task<Ok<PagedResponse<PatientSummaryResponse>>> Handle(
        Guid clinicianId,
        [AsParameters] ListMyPatientsRequest request,
        NpgsqlDataSource dataSource,
        TimeProvider time,
        CancellationToken ct
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);

        var organizationId = await connection.QuerySingleAsync<Guid>(
            new CommandDefinition(
                "select organization_id from clinicians where id = @clinicianId",
                new { clinicianId },
                cancellationToken: ct
            )
        );

        const string sql = """
            with candidates as (
                select patient_id from treatment_relationships
                where clinician_id = @clinicianId and ended_at is null
                union
                select patient_id from consent_grants
                where grantee_organization_id = @organizationId
                  and revoked_at is null
                  and (expires_at is null or expires_at > @now)
            )
            select p.id as Id, p.full_name as FullName, p.date_of_birth as DateOfBirth,
                   exists (
                       select 1 from treatment_relationships t
                       where t.patient_id = p.id and t.clinician_id = @clinicianId and t.ended_at is null
                   ) as HasTreatment,
                   exists (
                       select 1 from consent_grants g
                       where g.patient_id = p.id
                         and g.grantee_organization_id = @organizationId
                         and g.revoked_at is null
                         and (g.expires_at is null or g.expires_at > @now)
                   ) as HasConsent
            from patients p
            join candidates c on c.patient_id = p.id
            order by p.full_name, p.id
            limit @limit offset @offset
            """;

        var rows = await connection.QueryAsync<DbPatientRow>(
            new CommandDefinition(
                sql,
                new
                {
                    clinicianId,
                    organizationId,
                    now = time.GetUtcNow(),
                    limit = Paging.FetchLimit(request),
                    offset = Paging.Offset(request),
                },
                cancellationToken: ct
            )
        );

        var patients = rows.Select(row => row.ToResponse()).ToList();
        return TypedResults.Ok(Paging.ToPage<PatientSummaryResponse>(patients, request));
    }
}
