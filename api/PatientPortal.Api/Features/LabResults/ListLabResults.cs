using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace PatientPortal.Api.Features.LabResults;

public sealed record LabResultResponse(
    Guid Id,
    string Category,
    string TestName,
    decimal Value,
    string Unit,
    DateTimeOffset CollectedAt
);

public sealed record ListLabResultsResponse(IReadOnlyList<LabResultResponse> Items);

public static class ListLabResultsEndpoint
{
    public static void MapListLabResultsEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle).WithName("ListLabResults");

    private static async Task<Ok<ListLabResultsResponse>> Handle(
        Guid patientId,
        NpgsqlDataSource dataSource,
        CancellationToken ct
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);

        const string sql = """
            select id as Id, category as Category, test_name as TestName,
                   value_amount as Value, value_unit as Unit, collected_at as CollectedAt
            from lab_results
            where patient_id = @patientId
            order by collected_at desc, id desc
            """;

        var rows = await connection.QueryAsync<DbLabResultRow>(
            new CommandDefinition(sql, new { patientId }, cancellationToken: ct)
        );

        return TypedResults.Ok(
            new ListLabResultsResponse([.. rows.Select(row => row.ToResponse())])
        );
    }
}
