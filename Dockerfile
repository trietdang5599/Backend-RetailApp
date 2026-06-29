FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY ProductManagement.sln .
COPY src/Domain/ProductManagement.Domain.csproj src/Domain/
COPY src/Application/ProductManagement.Application.csproj src/Application/
COPY src/Infrastructure/ProductManagement.Infrastructure.csproj src/Infrastructure/
COPY src/API/ProductManagement.API.csproj src/API/
RUN dotnet restore

COPY . .
RUN dotnet publish src/API/ProductManagement.API.csproj -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /publish .
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5000
ENTRYPOINT ["dotnet", "ProductManagement.API.dll"]
