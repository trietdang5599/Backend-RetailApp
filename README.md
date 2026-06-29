# Product Management API

.NET 9 Clean Architecture backend for retail product management.

## Live Demo

**Production URL:** https://backend-retailapp.onrender.com

> Hosted on Render (free tier — cold start ~30s after inactivity)

- Swagger UI: https://backend-retailapp.onrender.com/swagger
- Base API: https://backend-retailapp.onrender.com/api

## Tech Stack

- **ASP.NET Core 9** — REST API
- **PostgreSQL** — primary database (EF Core + Npgsql)
- **MongoDB** — product attributes (flexible schema)
- **Redis** *(optional)* — cache layer, falls back to InMemory if not configured
- **MediatR** — CQRS pattern
- **FluentValidation** — request validation
- **Serilog** — structured logging

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)
- PostgreSQL running on `localhost:5432`
- MongoDB running on `localhost:27017`
- (Optional) Redis

---

## Run the project

### 1. Configure the database

Edit `src/API/appsettings.json` to match your local PostgreSQL credentials:

```json
"ConnectionStrings": {
  "PostgreSQL": "Host=localhost;Database=product_management;Username=YOUR_USER;Trust Server Certificate=true"
}
```

### 2. Apply migrations

```bash
dotnet ef database update --project src/Infrastructure --startup-project src/API
```

### 3. Start the API

```bash
dotnet run --project src/API
```

API will be available at:
- Swagger UI: `http://localhost:5000/swagger`
- Base URL: `http://localhost:5000/api`

### Hot-reload (recommended for development)

```bash
dotnet watch --project src/API
```

---

## Run tests

### All tests

```bash
dotnet test tests/ProductManagement.Tests.csproj
```

### Specific test group

```bash
# Unit tests only (fast, no DB)
dotnet test --filter "FullyQualifiedName~Domain|FullyQualifiedName~Application"

# Rate limiting integration tests
dotnet test --filter "FullyQualifiedName~RateLimiting"
```

### With output

```bash
dotnet test --logger "console;verbosity=normal"
```

> Rate limiting tests spin up the full API in-memory using `WebApplicationFactory` with a limit of 5 req/min. No PostgreSQL or MongoDB required — they use InMemory database automatically.

---

## Project structure

```
src/
├── API/                  # Controllers, middleware, program entry
│   ├── Controllers/
│   ├── Middleware/       # ExceptionHandling, SecurityHeaders, RequestTiming
│   └── Extensions/
├── Application/          # CQRS handlers, validators, DTOs
├── Domain/               # Entities, interfaces, business rules
└── Infrastructure/       # EF Core, MongoDB, Redis, repositories

tests/
├── Domain/               # Entity unit tests
├── Application/          # Handler unit tests (Moq)
└── API/                  # Integration tests (WebApplicationFactory)
```

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products` | List products (paged, filtered, sorted) |
| GET | `/api/products/{id}` | Get product by ID |
| POST | `/api/products` | Create product |
| PUT | `/api/products/{id}` | Update product |
| DELETE | `/api/products/{id}` | Soft delete product |
| PATCH | `/api/products/{id}/status` | Update product status |
| GET | `/api/categories` | List categories (tree or flat) |
| GET | `/api/categories?leafOnly=true` | Leaf categories only (for product picker) |
| POST | `/api/categories` | Create sub-category |

## Rate limiting

- **100 requests/minute** per window (global)
- Exceeded requests return `429 Too Many Requests`
- Configurable via `appsettings.json`:

```json
"RateLimiting": {
  "PermitLimit": 100,
  "WindowSeconds": 60
}
```
