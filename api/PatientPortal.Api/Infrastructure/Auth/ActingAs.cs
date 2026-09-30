using Dapper;
using Microsoft.AspNetCore.Authorization;
using Npgsql;

namespace PatientPortal.Api.Infrastructure.Auth;

public enum ActorKind
{
    Patient,
    Clinician,
    Researcher,
}

// The caller must be the person named in the route (/patients/{patientId}, /clinicians/{clinicianId},
// /researchers/{researcherId}), not just someone with the right role.
public sealed record ActingAsRequirement(ActorKind Kind, string RouteKey)
    : IAuthorizationRequirement;

public sealed class ActingAsHandler(NpgsqlDataSource dataSource)
    : AuthorizationHandler<ActingAsRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActingAsRequirement requirement
    )
    {
        var subject = context.User.FindFirst("sub")?.Value;
        if (
            subject is null
            || context.Resource is not HttpContext http
            || !Guid.TryParse(
                http.GetRouteValue(requirement.RouteKey)?.ToString(),
                out var personId
            )
        )
        {
            return;
        }

        var sql = requirement.Kind switch
        {
            ActorKind.Patient =>
                "select exists (select 1 from patients where id = @personId and external_subject_id = @subject)",
            ActorKind.Clinician =>
                "select exists (select 1 from clinicians where id = @personId and external_subject_id = @subject)",
            ActorKind.Researcher =>
                "select exists (select 1 from researchers where id = @personId and external_subject_id = @subject)",
            _ => throw new InvalidOperationException($"Unsupported actor '{requirement.Kind}'."),
        };

        await using var connection = await dataSource.OpenConnectionAsync(http.RequestAborted);
        var matches = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { personId, subject },
                cancellationToken: http.RequestAborted
            )
        );

        if (matches)
        {
            context.Succeed(requirement);
        }
    }
}
