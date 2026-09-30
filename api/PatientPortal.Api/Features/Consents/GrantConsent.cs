using Dapper;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using PatientPortal.Api.Common;
using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Features.Consents;

public sealed record GrantConsentRequest(
    Guid GranteeOrganizationId,
    IReadOnlyList<string> Categories,
    string Purpose,
    DateTimeOffset? ExpiresAt
);

public sealed class GrantConsentValidator : AbstractValidator<GrantConsentRequest>
{
    public GrantConsentValidator(TimeProvider time)
    {
        RuleFor(x => x.GranteeOrganizationId).NotEmpty();
        RuleFor(x => x.Categories).NotEmpty();
        RuleForEach(x => x.Categories)
            .Must(name => LabCategoryNames.TryParse(name, out _))
            .WithMessage("'{PropertyValue}' is not a known lab category.");
        RuleFor(x => x.Purpose).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ExpiresAt)
            .Must(expiresAt => expiresAt > time.GetUtcNow())
            .When(x => x.ExpiresAt is not null)
            .WithMessage("The expiry must be in the future.");
    }
}

public static class GrantConsentEndpoint
{
    public static void MapGrantConsentEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/", Handle).WithValidation<GrantConsentRequest>().WithName("GrantConsent");

    private static async Task<Results<CreatedAtRoute<ConsentResponse>, ProblemHttpResult>> Handle(
        Guid patientId,
        GrantConsentRequest request,
        NpgsqlDataSource dataSource,
        TimeProvider time,
        CancellationToken ct
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);

        var granteeName = await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(
                "select name from organizations where id = @id",
                new { id = request.GranteeOrganizationId },
                cancellationToken: ct
            )
        );
        if (granteeName is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Grantee organization not found.",
                detail: $"Organization '{request.GranteeOrganizationId}' does not exist."
            );
        }

        var now = time.GetUtcNow();
        var categories = request
            .Categories.Select(name =>
            {
                LabCategoryNames.TryParse(name, out var category);
                return category;
            })
            .Distinct()
            .ToArray();
        var expiry = request.ExpiresAt is { } expiresAt
            ? (ConsentExpiry)new ExpiresAt(expiresAt.ToUniversalTime())
            : new NeverExpires();

        var consent = ConsentTransitions.Grant(
            new ConsentGrantId(Guid.CreateVersion7()),
            new PatientId(patientId),
            new OrganizationId(request.GranteeOrganizationId),
            new ConsentScope(categories[0], categories[1..]),
            new ConsentPurpose(request.Purpose.Trim()),
            expiry,
            now
        );
        var audit = AuditEntries.ForConsentGranted(
            new AuditLogEntryId(Guid.CreateVersion7()),
            now,
            consent
        );

        const string insertSql = """
            insert into consent_grants
                (id, patient_id, grantee_organization_id, categories, purpose, granted_at, expires_at)
            values
                (@Id, @PatientId, @GranteeOrganizationId, @Categories, @Purpose, @GrantedAt, @ExpiresAt)
            """;

        await using var transaction = await connection.BeginTransactionAsync(ct);

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertSql,
                new
                {
                    Id = consent.Id.Value,
                    PatientId = consent.Patient.Value,
                    GranteeOrganizationId = consent.Grantee.Value,
                    Categories = categories.Select(LabCategoryNames.ToName).ToArray(),
                    Purpose = consent.Purpose.Value,
                    GrantedAt = consent.GrantedAt,
                    ExpiresAt = (expiry as ExpiresAt)?.Instant,
                },
                transaction,
                cancellationToken: ct
            )
        );
        await AuditLogWriter.InsertAsync(connection, transaction, audit, ct);

        await transaction.CommitAsync(ct);

        var response = ConsentResponse.From(consent, granteeName, (expiry as ExpiresAt)?.Instant);
        return TypedResults.CreatedAtRoute(
            response,
            "GetConsent",
            new { patientId, consentId = response.Id }
        );
    }
}
