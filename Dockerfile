FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj files for layer caching — only production projects, not tests
COPY src/Domain/ProductManagement.Domain.csproj src/Domain/
COPY src/Application/ProductManagement.Application.csproj src/Application/
COPY src/Infrastructure/ProductManagement.Infrastructure.csproj src/Infrastructure/
COPY src/API/ProductManagement.API.csproj src/API/

# Restore only the API project (pulls all transitive deps, skips tests)
RUN dotnet restore src/API/ProductManagement.API.csproj

COPY src/ src/
RUN dotnet publish src/API/ProductManagement.API.csproj -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /publish .
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5000
ENTRYPOINT ["dotnet", "ProductManagement.API.dll"]
