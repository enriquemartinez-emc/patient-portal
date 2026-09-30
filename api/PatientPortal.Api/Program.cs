using FluentValidation;
using Npgsql;
using PatientPortal.Api.Auth;
using PatientPortal.Api.Common;
using PatientPortal.Api.Features.Audit;
using PatientPortal.Api.Features.Clinicians;
using PatientPortal.Api.Features.Consents;
using PatientPortal.Api.Features.LabResults;
using PatientPortal.Api.Features.Me;
using PatientPortal.Api.Features.Organizations;
using PatientPortal.Api.Features.Researchers;
using PatientPortal.Api.Features.Treatment;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

DapperConfiguration.Register();

builder.Services.AddPortalAuthentication(builder.Configuration).AddPortalAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.MapHealthChecks("/health").AllowAnonymous();
app.MapLabResultsEndpoints();
app.MapConsentsEndpoints();
app.MapAuditEndpoints();
app.MapMeEndpoints();
app.MapOrganizationsEndpoints();
app.MapTreatmentEndpoints();
app.MapCliniciansEndpoints();
app.MapResearchersEndpoints();

app.Run();

public partial class Program;
