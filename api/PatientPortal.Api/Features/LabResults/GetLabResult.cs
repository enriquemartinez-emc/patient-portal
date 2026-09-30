using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using PatientPortal.Api.Common;

namespace PatientPortal.Api.Features.LabResults;

public static class GetLabResultEndpoint
{
    public static void MapGetLabResultEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("/{labResultId:guid}", Handle).WithName("GetLabResult");

    private static async Task<Results<Ok<LabResultResponse>, ProblemHttpResult>> Handle(
        Guid patientId,
        Guid labResultId,
        NpgsqlDataSource dataSource,
        CancellationToken ct
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);

        if (!await PatientGuard.ExistsAsync(connection, patientId, ct))
        {
            return PatientGuard.NotFound(patientId);
        }

        // Scoped by patient as well as id, so another patient's result is indistinguishable from a missing one.
        const string sql = """
            select id as Id, category as Category, test_name as TestName,
                   value_amount as Value, value_unit as Unit, collected_at as CollectedAt
            from lab_results
            where id = @labResultId and patient_id = @patientId
            """;

        var row = await connection.QuerySingleOrDefaultAsync<DbLabResultRow>(
            new CommandDefinition(sql, new { labResultId, patientId }, cancellationToken: ct)
        );

        if (row is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Lab result not found.",
                detail: $"Patient '{patientId}' has no lab result '{labResultId}'."
            );
        }

        return TypedResults.Ok(row.ToResponse());
    }
}
