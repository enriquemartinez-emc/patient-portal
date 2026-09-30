using Dapper;
using Npgsql;
using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Features.LabResults;

internal static class LabResultsQuery
{
    public static async Task<IReadOnlyList<DbLabResultRow>> InCategoriesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid patientId,
        IReadOnlyList<LabCategory> categories,
        CancellationToken ct
    )
    {
        const string sql = """
            select id as Id, category as Category, test_name as TestName,
                   value_amount as Value, value_unit as Unit, collected_at as CollectedAt
            from lab_results
            where patient_id = @patientId and category = any(@categories)
            order by collected_at desc, id desc
            """;

        var rows = await connection.QueryAsync<DbLabResultRow>(
            new CommandDefinition(
                sql,
                new
                {
                    patientId,
                    categories = categories.Select(LabCategoryNames.ToName).ToArray(),
                },
                transaction,
                cancellationToken: ct
            )
        );
        return [.. rows];
    }
}
