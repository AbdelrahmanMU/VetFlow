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

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "VetFlow.Api.dll"]
