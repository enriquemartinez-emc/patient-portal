using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using PatientPortal.Api.Common;
using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Auth;

public static class AccessDenial
{
    // Ends the read (releasing its locks), records the refusal in the patient's audit trail, and
    // answers 403. The refusal is recorded on its own, so it is kept even though the read is not.
    public static async Task<ProblemHttpResult> RefuseAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        AuditActor actor,
        Guid patientId,
        TimeProvider time,
        CancellationToken ct
    )
    {
        await transaction.RollbackAsync(ct);

        await AuditLogWriter.InsertAsync(
            connection,
            transaction: null,
            AuditEntries.ForAccessDenied(
                new AuditLogEntryId(Guid.CreateVersion7()),
                time.GetUtcNow(),
                actor,
                new PatientId(patientId)
            ),
            ct
        );

        return TypedResults.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Access denied.",
            detail: "You are not allowed to view this patient's records."
        );
    }
}
