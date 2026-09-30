using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace PatientPortal.Api.Common;

public static class PatientGuard
{
    public static async Task<bool> ExistsAsync(
        NpgsqlConnection connection,
        Guid patientId,
        CancellationToken ct
    ) =>
        await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                "select exists (select 1 from patients where id = @patientId)",
                new { patientId },
                cancellationToken: ct
            )
        );

    public static ProblemHttpResult NotFound(Guid patientId) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Patient not found.",
            detail: $"Patient '{patientId}' does not exist."
        );
}
