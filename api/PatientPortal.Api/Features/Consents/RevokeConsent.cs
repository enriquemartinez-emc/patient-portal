using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using PatientPortal.Api.Common;
using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Features.Consents;

public static class RevokeConsentEndpoint
{
    public static void MapRevokeConsentEndpoint(this IEndpointRouteBuilder app) =>
        app.MapDelete("/{consentId:guid}", Handle).WithName("RevokeConsent");

    // Idempotent: revoking a consent that is already revoked succeeds without a second audit entry.
    private static async Task<Results<NoContent, ProblemHttpResult>> Handle(
        Guid patientId,
        Guid consentId,
        NpgsqlDataSource dataSource,
        TimeProvider time,
        CancellationToken ct
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        // The row lock serializes concurrent revokes of the same consent (and, later, reads that
        // rely on it), and is transaction-scoped so it is safe behind a transaction-mode pooler.
        const string selectSql =
            DbConsentRow.SelectSql
            + """

                where c.id = @consentId and c.patient_id = @patientId
                for update of c
                """;

        var row = await connection.QuerySingleOrDefaultAsync<DbConsentRow>(
            new CommandDefinition(
                selectSql,
                new { consentId, patientId },
                transaction,
                cancellationToken: ct
            )
        );

        if (row is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Consent not found.",
                detail: $"Patient '{patientId}' has no consent '{consentId}'."
            );
        }

        var now = time.GetUtcNow();

        switch (row.ToConsent(now))
        {
            case RevokedConsent:
                return TypedResults.NoContent();

            case ExpiredConsent:
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Consent has already expired.",
                    detail: "An expired consent no longer grants access, so there is nothing to revoke."
                );

            case ActiveConsent active:
                var revoked = ConsentTransitions.Revoke(active, now);
                var audit = AuditEntries.ForConsentRevoked(
                    new AuditLogEntryId(Guid.CreateVersion7()),
                    now,
                    revoked
                );

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        "update consent_grants set revoked_at = @RevokedAt where id = @Id and revoked_at is null",
                        new { RevokedAt = revoked.RevokedAt, Id = revoked.Id.Value },
                        transaction,
                        cancellationToken: ct
                    )
                );
                await AuditLogWriter.InsertAsync(connection, transaction, audit, ct);

                await transaction.CommitAsync(ct);
                return TypedResults.NoContent();

            default:
                throw new InvalidOperationException("Unsupported consent state.");
        }
    }
}
