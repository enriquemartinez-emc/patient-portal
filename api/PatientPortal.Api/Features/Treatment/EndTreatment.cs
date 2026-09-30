using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Features.Treatment;

public static class EndTreatmentEndpoint
{
    public static void MapEndTreatmentEndpoint(this IEndpointRouteBuilder app) =>
        app.MapDelete("/{treatmentId:guid}", Handle).WithName("EndTreatment");

    // Idempotent: ending a relationship that has already ended succeeds and changes nothing.
    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(
        Guid treatmentId,
        NpgsqlDataSource dataSource,
        TimeProvider time,
        CancellationToken ct
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        // FOR UPDATE serializes with reads that rely on this relationship (they take FOR SHARE).
        const string selectSql = DbTreatmentRow.SelectSql + " where id = @treatmentId for update";

        var row = await connection.QuerySingleOrDefaultAsync<DbTreatmentRow>(
            new CommandDefinition(
                selectSql,
                new { treatmentId },
                transaction,
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

        if (row.ToTreatment() is not ActiveTreatment active)
        {
            return TypedResults.NoContent();
        }

        var ended = TreatmentTransitions.End(active, time.GetUtcNow());

        await connection.ExecuteAsync(
            new CommandDefinition(
                "update treatment_relationships set ended_at = @EndedAt where id = @Id and ended_at is null",
                new { ended.EndedAt, Id = ended.Id.Value },
                transaction,
                cancellationToken: ct
            )
        );

        await transaction.CommitAsync(ct);
        return TypedResults.NoContent();
    }
}
