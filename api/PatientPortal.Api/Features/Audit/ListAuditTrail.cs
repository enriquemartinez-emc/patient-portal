using Dapper;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using PatientPortal.Api.Common;

namespace PatientPortal.Api.Features.Audit;

public sealed record ListAuditTrailRequest(
    int Page = 1,
    int PageSize = PagingRules.DefaultPageSize,
    string? Action = null
) : IPagedRequest;

public sealed class ListAuditTrailValidator : PagedRequestValidator<ListAuditTrailRequest>
{
    private static readonly string[] Actions =
    [
        "lab_results_read",
        "consent_granted",
        "consent_revoked",
    ];

    public ListAuditTrailValidator()
    {
        RuleFor(x => x.Action)
            .Must(action => Actions.Contains(action))
            .When(x => x.Action is not null)
            .WithMessage("The action filter must be one of: " + string.Join(", ", Actions) + ".");
    }
}

public sealed record AuditEntryResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    string ActorKind,
    string ActorName,
    string? ActorOrganization,
    string Action,
    IReadOnlyList<Guid> LabResultIds,
    Guid? ConsentGrantId
);

public static class ListAuditTrailEndpoint
{
    public static void MapListAuditTrailEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle).WithValidation<ListAuditTrailRequest>().WithName("ListAuditTrail");

    private static async Task<Ok<PagedResponse<AuditEntryResponse>>> Handle(
        Guid patientId,
        [AsParameters] ListAuditTrailRequest request,
        NpgsqlDataSource dataSource,
        CancellationToken ct
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);

        // Ordered by (occurred_at desc, id desc) to match audit_log_patient_occurred_idx; the id is the tiebreaker.
        const string sql = """
            select a.id as Id, a.occurred_at as OccurredAt, a.actor_kind as ActorKind,
                   coalesce(p.full_name, c.full_name, r.full_name, 'Unknown') as ActorName,
                   coalesce(co.name, ro.name) as ActorOrganization,
                   a.action as Action, a.lab_result_ids as LabResultIds,
                   a.consent_grant_id as ConsentGrantId
            from audit_log a
            left join patients p on a.actor_kind = 'patient' and p.id = a.actor_id
            left join clinicians c on a.actor_kind = 'clinician' and c.id = a.actor_id
            left join organizations co on co.id = c.organization_id
            left join researchers r on a.actor_kind = 'researcher' and r.id = a.actor_id
            left join organizations ro on ro.id = r.organization_id
            where a.patient_id = @patientId
              and (@action::text is null or a.action = @action)
            order by a.occurred_at desc, a.id desc
            limit @limit offset @offset
            """;

        var rows = await connection.QueryAsync<DbAuditEntryRow>(
            new CommandDefinition(
                sql,
                new
                {
                    patientId,
                    action = request.Action,
                    limit = Paging.FetchLimit(request),
                    offset = Paging.Offset(request),
                },
                cancellationToken: ct
            )
        );

        var entries = rows.Select(row => row.ToResponse()).ToList();
        return TypedResults.Ok(Paging.ToPage<AuditEntryResponse>(entries, request));
    }
}

file sealed record DbAuditEntryRow(
    Guid Id,
    DateTimeOffset OccurredAt,
    string ActorKind,
    string ActorName,
    string? ActorOrganization,
    string Action,
    Guid[] LabResultIds,
    Guid? ConsentGrantId
)
{
    public AuditEntryResponse ToResponse() =>
        new(
            Id,
            OccurredAt,
            ActorKind,
            ActorName,
            ActorOrganization,
            Action,
            LabResultIds,
            ConsentGrantId
        );
}
