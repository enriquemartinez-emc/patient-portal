using FluentValidation;
using Npgsql;
using PatientPortal.Api.Common;
using PatientPortal.Api.Features.Audit;
using PatientPortal.Api.Features.Consents;
using PatientPortal.Api.Features.LabResults;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

DapperConfiguration.Register();

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthChecks("/health");
app.MapLabResultsEndpoints();
app.MapConsentsEndpoints();
app.MapAuditEndpoints();

app.Run();

public partial class Program;
