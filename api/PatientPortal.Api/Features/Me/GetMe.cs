using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace PatientPortal.Api.Features.Me;

public sealed record MeResponse(string Kind, Guid Id, string Name);

public static class GetMeEndpoint
{
    public static void MapGetMeEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle).WithName("GetMe");

    // Who the signed-in user is in this system: the patient, clinician or researcher record linked to
    // their login. Only the tables for roles the token holds are searched.
    private static async Task<Results<Ok<MeResponse>, ProblemHttpResult>> Handle(
        ClaimsPrincipal user,
        NpgsqlDataSource dataSource,
        CancellationToken ct
    )
    {
        var subject = user.FindFirstValue("sub");
        if (subject is null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized);
        }

        await using var connection = await dataSource.OpenConnectionAsync(ct);
        var matches = new List<MeResponse>();
        foreach (
            var (role, sql) in new[]
            {
                (
                    "patient",
                    "select id as Id, full_name as Name from patients where external_subject_id = @subject"
                ),
                (
                    "clinician",
                    "select id as Id, full_name as Name from clinicians where external_subject_id = @subject"
                ),
                (
                    "researcher",
                    "select id as Id, full_name as Name from researchers where external_subject_id = @subject"
                ),
            }
        )
        {
            if (!user.IsInRole(role))
            {
                continue;
            }

            var person = await connection.QuerySingleOrDefaultAsync<DbPersonRow>(
                new CommandDefinition(sql, new { subject }, cancellationToken: ct)
            );
            if (person is not null)
            {
                matches.Add(new MeResponse(role, person.Id, person.Name));
            }
        }

        return matches switch
        {
            [var only] => TypedResults.Ok(only),
            [] => TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "No portal profile.",
                detail: "This login is not linked to a patient, clinician or researcher record."
            ),
            _ => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "More than one portal profile.",
                detail: "This login is linked to more than one kind of record."
            ),
        };
    }
}

file sealed record DbPersonRow(Guid Id, string Name);
