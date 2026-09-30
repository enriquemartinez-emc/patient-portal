using Dapper;
using Npgsql;
using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Features.Consents;

internal static class ConsentCoverage
{
    // Loads the consents a patient has given an organization, as they stand right now. The rows are
    // locked FOR SHARE for the rest of the transaction, so a concurrent revoke (which takes FOR
    // UPDATE) either finishes first and is seen here, or waits until the read has committed. The
    // lock is transaction-scoped, so it is pooler-safe.
    public static async Task<IReadOnlyList<ConsentGrant>> LoadGrantsAsync(
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

        return [.. rows.Select(row => row.ToConsent(now))];
    }
}
