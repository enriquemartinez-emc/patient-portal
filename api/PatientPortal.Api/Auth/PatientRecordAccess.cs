using Dapper;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using PatientPortal.Api.Features.Consents;
using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Auth;

// May this clinician or researcher read this specific patient's lab results, and which categories?
public sealed class PatientRecordAccessRequirement : IAuthorizationRequirement
{
    public static readonly PatientRecordAccessRequirement Instance = new();
}

// The request carries the caller's open transaction: the checks lock the rows they rely on until
// the read commits, so a revoke cannot slip in between the decision and the read.
public sealed class PatientRecordRequest(
    ActorKind actor,
    Guid organizationId,
    Guid actorId,
    Guid patientId,
    NpgsqlConnection connection,
    NpgsqlTransaction transaction,
    CancellationToken cancellationToken
)
{
    public ActorKind Actor { get; } = actor;
    public Guid OrganizationId { get; } = organizationId;
    public Guid ActorId { get; } = actorId;
    public Guid PatientId { get; } = patientId;
    public NpgsqlConnection Connection { get; } = connection;
    public NpgsqlTransaction Transaction { get; } = transaction;
    public CancellationToken CancellationToken { get; } = cancellationToken;

    // Set when access is granted: the categories the caller may see.
    public IReadOnlyList<LabCategory> VisibleCategories { get; set; } = [];
}

public sealed class PatientRecordAccessHandler(TimeProvider time)
    : AuthorizationHandler<PatientRecordAccessRequirement, PatientRecordRequest>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PatientRecordAccessRequirement requirement,
        PatientRecordRequest request
    )
    {
        // A treating clinician sees every category. Everyone else sees what the patient's
        // consents to their organization cover; no treatment and no consent means no access.
        var categories =
            request.Actor == ActorKind.Clinician && await IsTreatingAsync(request)
                ? LabCategoryNames.All
                : await ConsentCoverage.CoveredCategoriesAsync(
                    request.Connection,
                    request.Transaction,
                    request.PatientId,
                    request.OrganizationId,
                    time.GetUtcNow(),
                    request.CancellationToken
                );

        if (categories.Count > 0)
        {
            request.VisibleCategories = categories;
            context.Succeed(requirement);
        }
    }

    // FOR SHARE keeps a concurrent "end treatment" from committing mid-read.
    private static async Task<bool> IsTreatingAsync(PatientRecordRequest request) =>
        await request.Connection.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(
                """
                select id from treatment_relationships
                where patient_id = @PatientId and clinician_id = @ActorId and ended_at is null
                for share
                """,
                new { request.PatientId, request.ActorId },
                request.Transaction,
                cancellationToken: request.CancellationToken
            )
        )
            is not null;
}
