using Dapper;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using PatientPortal.Api.Common;
using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Features.Treatment;

public sealed record StartTreatmentRequest(Guid PatientId, Guid ClinicianId);

public sealed class StartTreatmentValidator : AbstractValidator<StartTreatmentRequest>
{
    public StartTreatmentValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.ClinicianId).NotEmpty();
    }
}

public static class StartTreatmentEndpoint
{
    public static void MapStartTreatmentEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/", Handle).WithValidation<StartTreatmentRequest>().WithName("StartTreatment");

    private static async Task<
        Results<CreatedAtRoute<TreatmentRelationshipResponse>, ProblemHttpResult>
    > Handle(
        StartTreatmentRequest request,
        NpgsqlDataSource dataSource,
        TimeProvider time,
        CancellationToken ct
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);

        var patientExists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                "select exists (select 1 from patients where id = @id)",
                new { id = request.PatientId },
                cancellationToken: ct
            )
        );
        if (!patientExists)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Patient not found.",
                detail: $"Patient '{request.PatientId}' does not exist."
            );
        }

        var clinicianExists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                "select exists (select 1 from clinicians where id = @id)",
                new { id = request.ClinicianId },
                cancellationToken: ct
            )
        );
        if (!clinicianExists)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Clinician not found.",
                detail: $"Clinician '{request.ClinicianId}' does not exist."
            );
        }

        var treatment = TreatmentTransitions.Start(
            new TreatmentRelationshipId(Guid.CreateVersion7()),
            new PatientId(request.PatientId),
            new ClinicianId(request.ClinicianId),
            time.GetUtcNow()
        );

        const string sql = """
            insert into treatment_relationships (id, patient_id, clinician_id, started_at)
            values (@Id, @PatientId, @ClinicianId, @StartedAt)
            on conflict (patient_id, clinician_id) where ended_at is null do nothing
            """;

        var inserted = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    Id = treatment.Id.Value,
                    PatientId = treatment.Patient.Value,
                    ClinicianId = treatment.Clinician.Value,
                    treatment.StartedAt,
                },
                cancellationToken: ct
            )
        );

        if (inserted == 0)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Treatment relationship already active.",
                detail: "This clinician already has an active treatment relationship with this patient."
            );
        }

        var response = TreatmentRelationshipResponse.From(treatment);
        return TypedResults.CreatedAtRoute(
            response,
            "GetTreatment",
            new { treatmentId = response.Id }
        );
    }
}
