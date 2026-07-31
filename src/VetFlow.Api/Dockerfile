FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY Directory.Build.props .editorconfig ./
COPY src/VetFlow.Domain/VetFlow.Domain.csproj src/VetFlow.Domain/
COPY src/VetFlow.Application/VetFlow.Application.csproj src/VetFlow.Application/
COPY src/VetFlow.Infrastructure/VetFlow.Infrastructure.csproj src/VetFlow.Infrastructure/
COPY src/VetFlow.Api/VetFlow.Api.csproj src/VetFlow.Api/
RUN dotnet restore src/VetFlow.Api/VetFlow.Api.csproj
COPY src/ src/
RUN dotnet publish src/VetFlow.Api/VetFlow.Api.csproj -c Release -o /app --no-restore

# The Angular production bundle, built inside the image so `docker compose build`
# needs nothing from the host but the repository (PRS WS1 — the pilot deployment
# is one documented command, not a remembered sequence).
FROM node:24-alpine AS web
WORKDIR /web
COPY web/package.json web/package-lock.json ./
RUN npm ci
COPY web/ ./
RUN npx ng build

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
# Served same-origin by the API when present (Program.cs pilot hosting block).
COPY --from=web /web/dist/vetflow/browser ./wwwroot
EXPOSE 8080
ENTRYPOINT ["dotnet", "VetFlow.Api.dll"]
