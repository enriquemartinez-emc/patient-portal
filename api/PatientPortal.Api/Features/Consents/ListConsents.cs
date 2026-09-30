using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace PatientPortal.Api.Features.Consents;

public sealed record ListConsentsResponse(IReadOnlyList<ConsentResponse> Items);

public static class ListConsentsEndpoint
{
    public static void MapListConsentsEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle).WithName("ListConsents");

    private static async Task<Ok<ListConsentsResponse>> Handle(
        Guid patientId,
        NpgsqlDataSource dataSource,
        TimeProvider time,
        CancellationToken ct
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);

        const string sql =
            DbConsentRow.SelectSql
            + """

                where c.patient_id = @patientId
                order by c.granted_at desc, c.id desc
                """;

        var rows = await connection.QueryAsync<DbConsentRow>(
            new CommandDefinition(sql, new { patientId }, cancellationToken: ct)
        );

        var now = time.GetUtcNow();
        return TypedResults.Ok(
            new ListConsentsResponse([.. rows.Select(row => row.ToResponse(now))])
        );
    }
}
