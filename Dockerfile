# ---------- Stage 1: build (SDK image, big, has compilers) ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, so this layer is cached while only code changes
COPY uconia-api.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /out --no-restore

# ---------- Stage 2: run (runtime image only, small, no SDK) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .

# Commit SHA from the pipeline (shown by /api/info)
ARG APP_VERSION=local
ENV APP_VERSION=$APP_VERSION

# .NET images listen on 8080 by default and ship a non-root user
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "uconia-api.dll"]
