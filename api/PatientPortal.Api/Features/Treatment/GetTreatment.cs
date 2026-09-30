using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace PatientPortal.Api.Features.Treatment;

public static class GetTreatmentEndpoint
{
    public static void MapGetTreatmentEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("/{treatmentId:guid}", Handle).WithName("GetTreatment");

    private static async Task<Results<Ok<TreatmentRelationshipResponse>, ProblemHttpResult>> Handle(
        Guid treatmentId,
        NpgsqlDataSource dataSource,
        CancellationToken ct
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);

        var row = await connection.QuerySingleOrDefaultAsync<DbTreatmentRow>(
            new CommandDefinition(
                DbTreatmentRow.SelectSql + " where id = @treatmentId",
                new { treatmentId },
                cancellationToken: ct
            )
        );

        if (row is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Treatment relationship not found.",
                detail: $"Treatment relationship '{treatmentId}' does not exist."
            );
        }

        return TypedResults.Ok(TreatmentRelationshipResponse.From(row.ToTreatment()));
    }
}
