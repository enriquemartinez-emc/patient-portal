using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace PatientPortal.Api.Features.Consents;

public static class GetConsentEndpoint
{
    public static void MapGetConsentEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("/{consentId:guid}", Handle).WithName("GetConsent");

    private static async Task<Results<Ok<ConsentResponse>, ProblemHttpResult>> Handle(
        Guid patientId,
        Guid consentId,
        NpgsqlDataSource dataSource,
        TimeProvider time,
        CancellationToken ct
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);

        const string sql =
            DbConsentRow.SelectSql
            + """

                where c.id = @consentId and c.patient_id = @patientId
                """;

        var row = await connection.QuerySingleOrDefaultAsync<DbConsentRow>(
            new CommandDefinition(sql, new { consentId, patientId }, cancellationToken: ct)
        );

        if (row is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Consent not found.",
                detail: $"Patient '{patientId}' has no consent '{consentId}'."
            );
        }

        return TypedResults.Ok(row.ToResponse(time.GetUtcNow()));
    }
}
