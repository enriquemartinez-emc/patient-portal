using Dapper;
using Npgsql;
using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Features.Consents;

internal static class ConsentCoverage
{
    // Rows are locked FOR SHARE until the transaction ends, so a concurrent revoke (FOR UPDATE) is
    // either seen here or waits for the read to commit. The lock is transaction-scoped, so it is
    // pooler-safe.
    public static async Task<IReadOnlyList<LabCategory>> CoveredCategoriesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid patientId,
        Guid granteeOrganizationId,
        DateTimeOffset now,
        CancellationToken ct
    )
    {
        const string sql =
            DbConsentRow.SelectSql
            + """

                where c.patient_id = @patientId and c.grantee_organization_id = @granteeOrganizationId
                for share of c
                """;

        var rows = await connection.QueryAsync<DbConsentRow>(
            new CommandDefinition(
                sql,
                new { patientId, granteeOrganizationId },
                transaction,
                cancellationToken: ct
            )
        );

        return ConsentRules.CoveredCategories([.. rows.Select(row => row.ToConsent(now))]);
    }
}
