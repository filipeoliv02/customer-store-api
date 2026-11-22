# Customer Store API

[![CI](https://github.com/filipeoliv02/customer-store-api/actions/workflows/ci.yml/badge.svg)](https://github.com/filipeoliv02/customer-store-api/actions/workflows/ci.yml)

A small REST API for managing the customers of an e-commerce application, built with ASP.NET Core (.NET 10 LTS). It uses a layered architecture inspired by [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) and gets geolocation data for customer addresses from the third-party [PositionStack](https://positionstack.com/) API.

> Data is stored in an in-memory database only, so it is lost when the application stops.

## Features

| User story | Capability |
|---|---|
| US01 | Create a customer: name and email are required, the email must be unique (case-insensitive), and the VAT number must have 9 digits. |
| US02 | List customers, optionally filtered by `name` and/or `email` (case-insensitive exact match). |
| US03 | Get all the details of a single customer. |
| US04 | Delete a customer. |
| US05 | Get geolocation items (latitude, longitude, type, locality, region, country) for a customer's address from PositionStack. |

Other features:

- Errors are returned as [RFC 7807](https://www.rfc-editor.org/rfc/rfc7807) `ProblemDetails`, and validation errors as `ValidationProblemDetails`.
- An OpenAPI document and Swagger UI are available in the Development environment.
- A health check endpoint is available at `/health`.

## Tech stack

- **.NET 10 (LTS)**, ASP.NET Core MVC controllers, minimal hosting
- **Entity Framework Core 10** with the in-memory provider
- **NSwag** for OpenAPI and Swagger UI
- **IHttpClientFactory** with a typed client for PositionStack
- **xUnit v3** on Microsoft.Testing.Platform, **AwesomeAssertions**, `WebApplicationFactory` for integration tests
- **Analyzers:** .NET analyzers, StyleCop, VS Threading and AsyncFixer, configured in `.editorconfig`
- **GitHub Actions** for CI and **Dependabot** for NuGet, GitHub Actions and SDK updates

## Techniques & patterns demonstrated

| Technique / pattern | What it is | Why it is used | Where in the code |
|---|---|---|---|
| **Layered / Clean Architecture** | Separate layers for HTTP (controllers), application logic (managers), infrastructure (storage and external services) and domain entities. Dependencies point inward through interfaces. | Each layer can be changed and tested on its own. For example, the geolocation provider or the database can be replaced without touching the controller. | `WebApi/Controllers` → `WebApi/Managers` → `WebApi/Services`, `WebApi/Storage` → `WebApi/Entities` |
| **Result pattern** | Operations return `Result<T>`, which holds either a value or an error code and description, instead of throwing exceptions for expected failures. | Makes business failures (not found, duplicate email, and so on) explicit and type-safe. Nullability attributes (`MemberNotNullWhen`) let the compiler check that `Value` or `ErrorCode` is set. | `Managers/Result.cs`, `Managers/ErrorCodes.cs` |
| **Manager / service abstraction** | `ICustomersManager` holds the use cases, and `IGeoLocationService` hides the third-party provider. | Keeps controllers thin and keeps provider details (URLs, keys, JSON format) out of the business logic. Both interfaces can be replaced with fakes in tests. | `Managers/ICustomersManager.cs`, `Managers/CustomersManager.cs`, `Services/IGeoLocationService.cs` |
| **Dependency injection & composition root** | All services are registered in one place, and constructor injection is used everywhere. | Allows loose coupling, makes the code testable, and lets the DI container manage object lifetimes (scoped `DbContext` and manager, pooled HTTP handlers). | `ApplicationSetup.cs`, `Program.cs` |
| **Mapping layer (entity ↔ DTO)** | Persistence entities are never exposed directly. Explicit mapping extension methods convert entities to request and response DTOs. | Decouples the API contract from the storage model and avoids over-posting. Explicit mappings are checked at compile time and have no reflection cost. | `Managers/CustomerMappings.cs`, `Models/*` |
| **DTOs with records** | Immutable `record` types for responses. | Value equality and concise, immutable contracts. | `Models/CustomerDto.cs`, `Models/CustomerDetailsDto.cs` |
| **Declarative validation** | Data annotations (`[Required]`, `[EmailAddress]`, `[StringLength]`, `[RegularExpression]`) plus `[ApiController]` automatic 400 responses. | Requests are validated before they reach the business logic, and the length limits match the database limits. | `Models/CreateCustomerRequest.cs`, `Storage/ApplicationDbContext.cs` |
| **Typed HttpClient (IHttpClientFactory)** | The PositionStack client receives a pooled `HttpClient` that has its base address and timeout already set. | Avoids socket exhaustion and stale DNS from creating `HttpClient` by hand, and makes the client easy to test. | `ApplicationSetup.cs` (`AddPositionStackGeolocation`), `Services/PositionStackGeolocationService.cs` |
| **Options pattern** | Strongly typed `PositionStackOptions` read from configuration and environment variables, and validated at startup (HTTPS base address, timeout range). | Centralises configuration, catches invalid settings when the app starts, and keeps secrets out of source control. | `Services/PositionStackOptions.cs`, `appsettings.json` |
| **Defensive integration of an external API** | Handles timeouts, transport errors, non-success status codes, error payloads and malformed JSON. Returns `502 Bad Gateway` when the provider fails, and propagates caller cancellation. | Third-party APIs fail in many ways, so this API degrades gracefully instead of returning `500`. | `Services/PositionStackGeolocationService.cs`, `Controllers/CustomersController.cs` |
| **High-performance structured logging** | Source-generated `[LoggerMessage]` methods that log identifiers only. | Avoids allocations and string formatting, and never writes personal data (names, emails, addresses) or the API key to the logs. | `Managers/CustomersManager.cs`, `Services/PositionStackGeolocationService.cs` |
| **Async all the way + cancellation** | Every I/O path is `async` and accepts a `CancellationToken` that comes from the HTTP request. | Improves scalability, and stops work when the client disconnects. | Controllers, managers and services |
| **ProblemDetails error handling** | `AddProblemDetails` with `UseExceptionHandler` and `UseStatusCodePages`. | Consistent, standards-based error responses, and no stack traces outside Development. | `ApplicationSetup.cs`, `Controllers/CustomersController.cs` (`ToProblem`) |
| **Testing pyramid** | Unit tests for the manager (in-memory EF Core with a fake geolocation service) and the PositionStack client (stub `HttpMessageHandler`), plus integration tests for the full HTTP pipeline (`WebApplicationFactory`). The database is isolated per test. | Fast, deterministic tests that never call the real third-party API. | `Tests/WebApi.Tests/{Managers,Services,Controllers}`, `Tests/WebApi.Tests/Fakes` |
| **Quality gates** | Nullable reference types, strict analyzers, central package management, NuGet vulnerability audit, and warnings treated as errors in CI. | Problems are caught at build time, not in production. | `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `.github/workflows/ci.yml` |

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (the version is pinned in `global.json`), **or** Docker
- Optional: a free [PositionStack API key](https://positionstack.com/signup/free) for the geolocation endpoint

### Configure the geolocation API key

The key is read only from configuration and environment variables. **Never commit it.**

```bash
# Either of these works (the first one is kept for backwards compatibility)
export PositionStackApiKey="<your-key>"
export PositionStack__ApiKey="<your-key>"

# Or, for local development, use .NET user secrets
dotnet user-secrets init --project WebApi
dotnet user-secrets set "PositionStack:ApiKey" "<your-key>" --project WebApi
```

Without a key, every endpoint still works except `GET /api/customers/{id}/geolocation`, which returns `502 Bad Gateway`.

### Run

```bash
dotnet run --project WebApi
```

The launch profile starts the API on `https://localhost:20001` and `http://localhost:20002` and opens Swagger UI at `/swagger`.

With Docker only (no local SDK):

```bash
docker run --rm -it -p 8080:8080 -v "$PWD":/src -w /src \
  -e ASPNETCORE_ENVIRONMENT=Development -e PositionStackApiKey \
  mcr.microsoft.com/dotnet/sdk:10.0 dotnet run --project WebApi --no-launch-profile --urls http://0.0.0.0:8080
```

## Testing

```bash
dotnet test
```

The tests use fakes for PositionStack, so they need no API key or network access. CI runs the same command on every push and pull request to `master`.

Vulnerability and update checks:

```bash
dotnet list package --vulnerable --include-transitive
dotnet list package --outdated
```

## API endpoints

| Method | Route | Description | Responses |
|---|---|---|---|
| `POST` | `/api/customers` | Create a customer. Body: `{ "name", "emailAddress", "address"?, "vatNumber"? }` | `201` with the id and a `Location` header, `400` validation error, `409` email already in use |
| `GET` | `/api/customers?name=&email=` | List customers, with optional filters | `200` with `[{ "id", "name", "email" }]` |
| `GET` | `/api/customers/{id}` | Get all of a customer's details | `200` with `{ "id", "name", "emailAddress", "address", "vatNumber" }`, `404` |
| `DELETE` | `/api/customers/{id}` | Delete a customer | `204`, `404` |
| `GET` | `/api/customers/{id}/geolocation` | Geolocation items for the customer's address | `200` with `{ "data": [ { "address", "latitude", "longitude", "type", "locality", "region", "country" } ] }`, `404` (unknown customer or no address), `502` (provider unavailable or not configured) |
| `GET` | `/health` | Health check | `200 Healthy` |
| `GET` | `/swagger` | Swagger UI (Development only) | — |

## Project structure

```
├── WebApi/                    # ASP.NET Core Web API (assembly: CustomerStoreApi)
│   ├── Controllers/           # HTTP layer
│   ├── Managers/              # Use cases, Result pattern, mappings
│   ├── Services/              # External services (PositionStack) + options
│   ├── Storage/               # EF Core DbContext
│   ├── Entities/              # Persistence model
│   ├── Models/                # Request/response DTOs
│   ├── ApplicationSetup.cs    # Composition root (DI + HTTP pipeline)
│   └── Program.cs
├── Tests/WebApi.Tests/        # Unit + integration tests
├── Directory.Build.props      # Shared build settings (nullable, analyzers, NuGet audit)
├── Directory.Packages.props   # Central package versions
└── global.json                # Pinned .NET SDK + test runner
```
