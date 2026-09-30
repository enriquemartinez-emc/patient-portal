using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace PatientPortal.Api.Features.Organizations;

public sealed record OrganizationResponse(Guid Id, string Name, string Kind);

public sealed record ListOrganizationsResponse(IReadOnlyList<OrganizationResponse> Items);

public static class ListOrganizationsEndpoint
{
    public static void MapListOrganizationsEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle).WithName("ListOrganizations");

    private static async Task<Ok<ListOrganizationsResponse>> Handle(
        NpgsqlDataSource dataSource,
        CancellationToken ct
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);

        const string sql = """
            select id as Id, name as Name, kind as Kind
            from organizations
            order by name, id
            """;

        var rows = await connection.QueryAsync<OrganizationResponse>(
            new CommandDefinition(sql, cancellationToken: ct)
        );

        return TypedResults.Ok(new ListOrganizationsResponse([.. rows]));
    }
}
