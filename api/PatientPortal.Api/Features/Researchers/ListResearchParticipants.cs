using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using PatientPortal.Api.Common;

namespace PatientPortal.Api.Features.Researchers;

public sealed record ListResearchParticipantsRequest(
    int Page = 1,
    int PageSize = PagingRules.DefaultPageSize
) : IPagedRequest;

public sealed class ListResearchParticipantsValidator
    : PagedRequestValidator<ListResearchParticipantsRequest>;

// Deliberately carries no name or date of birth: researchers see only what they may analyse.
public sealed record ResearchParticipantResponse(Guid PatientId, IReadOnlyList<string> Categories);

public static class ListResearchParticipantsEndpoint
{
    public static void MapListResearchParticipantsEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("/patients", Handle)
            .WithValidation<ListResearchParticipantsRequest>()
            .WithName("ListResearchParticipants");

    // Participants are the patients with an in-effect consent naming the researcher's organization,
    // with the union of categories those consents cover. The consent predicate mirrors
    // ConsentRules.ApplyExpiry: a consent stops being in effect at its expiry instant.
    private static async Task<Ok<PagedResponse<ResearchParticipantResponse>>> Handle(
        Guid researcherId,
        [AsParameters] ListResearchParticipantsRequest request,
        NpgsqlDataSource dataSource,
        TimeProvider time,
        CancellationToken ct
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);

        var organizationId = await connection.QuerySingleAsync<Guid>(
            new CommandDefinition(
                "select organization_id from researchers where id = @researcherId",
                new { researcherId },
                cancellationToken: ct
            )
        );

        const string sql = """
            select g.patient_id as PatientId,
                   array_agg(distinct c.category order by c.category) as Categories
            from consent_grants g
            cross join lateral unnest(g.categories) as c(category)
            where g.grantee_organization_id = @organizationId
              and g.revoked_at is null
              and (g.expires_at is null or g.expires_at > @now)
            group by g.patient_id
            order by g.patient_id
            limit @limit offset @offset
            """;

        var rows = await connection.QueryAsync<DbParticipantRow>(
            new CommandDefinition(
                sql,
                new
                {
                    organizationId,
                    now = time.GetUtcNow(),
                    limit = Paging.FetchLimit(request),
                    offset = Paging.Offset(request),
                },
                cancellationToken: ct
            )
        );

        var participants = rows.Select(row => new ResearchParticipantResponse(
                row.PatientId,
                row.Categories
            ))
            .ToList();
        return TypedResults.Ok(Paging.ToPage<ResearchParticipantResponse>(participants, request));
    }
}

file sealed record DbParticipantRow(Guid PatientId, string[] Categories);
