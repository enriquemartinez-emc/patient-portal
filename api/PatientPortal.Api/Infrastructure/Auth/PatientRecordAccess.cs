using Dapper;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using PatientPortal.Api.Features.Consents;
using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Infrastructure.Auth;

public sealed class PatientRecordAccessRequirement : IAuthorizationRequirement
{
    public static readonly PatientRecordAccessRequirement Instance = new();
}

// The resource carries the caller's open transaction: the checks lock the rows they rely on until
// the read commits, so a revoke cannot slip in between the decision and the read.
public sealed class PatientRecordResource(
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

    public IReadOnlyList<LabCategory> VisibleCategories { get; set; } = [];
}

public sealed class PatientRecordAccessHandler(TimeProvider time)
    : AuthorizationHandler<PatientRecordAccessRequirement, PatientRecordResource>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PatientRecordAccessRequirement requirement,
        PatientRecordResource resource
    )
    {
        var categories =
            resource.Actor == ActorKind.Clinician && await IsTreatingAsync(resource)
                ? LabCategoryNames.All
                : await ConsentCoverage.CoveredCategoriesAsync(
                    resource.Connection,
                    resource.Transaction,
                    resource.PatientId,
                    resource.OrganizationId,
                    time.GetUtcNow(),
                    resource.CancellationToken
                );

        if (categories.Count > 0)
        {
            resource.VisibleCategories = categories;
            context.Succeed(requirement);
        }
    }

    // FOR SHARE keeps a concurrent "end treatment" from committing mid-read.
    private static async Task<bool> IsTreatingAsync(PatientRecordResource resource) =>
        await resource.Connection.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(
                """
                select id from treatment_relationships
                where patient_id = @PatientId and clinician_id = @ActorId and ended_at is null
                for share
                """,
                new { resource.PatientId, resource.ActorId },
                resource.Transaction,
                cancellationToken: resource.CancellationToken
            )
        )
            is not null;
}
