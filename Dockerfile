# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
HEALTHCHECK --interval=30s --timeout=1s --start-period=15s --start-interval=3s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-backend
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ImageShare/ImageShare.csproj", "ImageShare/"]
RUN dotnet restore "./ImageShare/ImageShare.csproj"
COPY . .
WORKDIR "/src/ImageShare"
RUN dotnet build "./ImageShare.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage builds the Vite/React frontend (../frontend) into /src/frontend/dist.
# The @hey-api/vite-plugin generates the typed API client at Vite's configResolved
# hook, so a Vite build must run before `tsc -b` can resolve the generated imports.
# The compiled assets are later served from the "Client" folder by SpaExtensions.
FROM node:26-trixie-slim AS build-frontend
ENV COREPACK_ENABLE_DOWNLOAD_PROMPT=0
WORKDIR /src/frontend
RUN npm install -g corepack && corepack enable
# Install dependencies first so the layer is cached across source/API changes.
COPY frontend/package.json frontend/pnpm-lock.yaml frontend/pnpm-workspace.yaml ./
RUN pnpm install --frozen-lockfile
# Use the OpenAPI document produced by the backend build so the generated client
# matches the API being shipped.
COPY --from=build-backend /src/ImageShare/openapi.json /src/ImageShare/openapi.json
COPY frontend/ ./
RUN pnpm run build

# This stage is used to publish the service project to be copied to the final stage
FROM build-backend AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ImageShare.csproj" -c $BUILD_CONFIGURATION -o /app/publish
# Host the SPA from the "Client" folder expected by SpaExtensions.AddSpaStaticFiles.
COPY --from=build-frontend /src/frontend/dist /app/publish/Client

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ImageShare.dll"]
