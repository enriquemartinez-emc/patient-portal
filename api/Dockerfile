# ---- build: restore and publish both deployables -------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first, so the restore layer is cached until a dependency changes.
COPY PatientPortal.Core/PatientPortal.Core.csproj PatientPortal.Core/
COPY PatientPortal.Api/PatientPortal.Api.csproj PatientPortal.Api/
COPY PatientPortal.Migrations/PatientPortal.Migrations.csproj PatientPortal.Migrations/

RUN dotnet restore PatientPortal.Api/PatientPortal.Api.csproj \
    && dotnet restore PatientPortal.Migrations/PatientPortal.Migrations.csproj

COPY PatientPortal.Core/ PatientPortal.Core/
COPY PatientPortal.Api/ PatientPortal.Api/
COPY PatientPortal.Migrations/ PatientPortal.Migrations/

RUN dotnet publish PatientPortal.Api/PatientPortal.Api.csproj \
        -c Release -o /out/api --no-restore -p:UseAppHost=false \
    && dotnet publish PatientPortal.Migrations/PatientPortal.Migrations.csproj \
        -c Release -o /out/migrations --no-restore -p:UseAppHost=false


# ---- migrations: one-shot job that applies the database schema ------------------------------------
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS migrations
WORKDIR /app
COPY --from=build /out/migrations ./

USER $APP_UID
ENTRYPOINT ["dotnet", "PatientPortal.Migrations.dll"]


# ---- api: the HTTP service (last stage, so it is the default build target) ------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api

# curl is used by the compose healthcheck.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /out/api ./

# The aspnet image listens on 8080 by default.
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "PatientPortal.Api.dll"]
