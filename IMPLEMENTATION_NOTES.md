# Implementation Notes

This document summarizes the main changes made for the Accepted C# assessment and how the solution can be built, tested, and run.

## Solution Structure

The solution keeps the existing layered structure:

- `CSharpApp.Api`: Minimal API endpoints, API versioning, middleware, OpenAPI setup, and request/response handling.
- `CSharpApp.Application`: Application services that coordinate calls to the third-party API.
- `CSharpApp.Core`: Shared DTOs, interfaces, and settings models.
- `CSharpApp.Infrastructure`: Dependency injection and HTTP client configuration.
- `tests/CSharpApp.Application.Tests`: Unit tests for application services.
- `tests/CSharpApp.Api.Tests`: Integration tests for the API endpoints.

## Main Changes

### HTTP Client Refactor

The previous HTTP client usage was refactored to use typed `HttpClient` registrations through dependency injection. This keeps HTTP configuration centralized and avoids inefficient client creation patterns.

The configuration includes:

- a shared third-party API base URL
- handler lifetime configuration
- retry count and retry delay configuration
- typed clients for products, categories, and auth services

The `IHttpClientFactory` setup is configured in `CSharpApp.Infrastructure/Configuration/HttpConfiguration.cs` through `services.AddHttpClient(...)`.

The API startup calls this configuration from `CSharpApp.Api/Program.cs`:

```csharp
builder.Services.AddHttpConfiguration(builder.Configuration);
```

Each application service is registered as a typed HTTP client:

```csharp
services.AddHttpClient<IProductsService, ProductsService>(...)
services.AddHttpClient<ICategoriesService, CategoriesService>(...)
services.AddHttpClient<IAuthService, AuthService>(...)
```

This means the .NET `IHttpClientFactory` creates and manages the `HttpClient` instances that are injected into the application services.

The third-party API base URL comes from `RestApiSettings.BaseUrl`. HTTP client behavior is configured from `HttpClientSettings`:

- `LifeTime`: typed HTTP client handler lifetime in minutes
- `RetryCount`: number of retry attempts for transient upstream failures
- `SleepDuration`: base retry delay in milliseconds

Retries are intentionally applied only to idempotent HTTP methods. Non-idempotent operations such as `POST /products` or `POST /categories` are not retried, because retrying them could create duplicate resources.

Retry attempts are logged as structured infrastructure events with `LogType = OutgoingThirdPartyRetry`.

### CQRS Refactor

The API endpoints were refactored to use a CQRS-style application flow with MediatR.

The endpoint flow is now:

```text
HTTP endpoint
  -> command/query sent through ISender
  -> command/query handler
  -> application service
  -> third-party API
```

Queries are used for read operations:

- `GetProductsQuery`
- `GetProductQuery`
- `GetCategoriesQuery`
- `GetCategoryQuery`
- `GetCategoryProductsQuery`
- `GetAuthProfileQuery`

Commands are used for write or action-based operations:

- `CreateProductCommand`
- `CreateCategoryCommand`
- `UpdateCategoryCommand`
- `DeleteCategoryCommand`
- `LoginCommand`
- `RefreshTokenCommand`

The existing application services remain responsible for communicating with the third-party API. The CQRS handlers represent application use cases and delegate the external API work to those services.

This keeps the refactor focused: endpoints no longer call services directly, while the existing external API integration layer remains intact and testable.

### Products API

The products feature was expanded beyond the original `getAll` behavior.

Implemented endpoints:

- `GET /api/v1/products`
- `GET /api/v1/products/{id}`
- `POST /api/v1/products`

The API now returns proper HTTP responses for missing products and invalid third-party responses.

### Categories API

Categories support was added following the same service-oriented structure as products.

Implemented endpoints:

- `GET /api/v1/categories`
- `GET /api/v1/categories/{id}`
- `POST /api/v1/categories`
- `PUT /api/v1/categories/{id}`
- `DELETE /api/v1/categories/{id}`
- `GET /api/v1/categories/{id}/products`

The category endpoints cover the full category flow from the provided API collection.

### Endpoint Organization

The project keeps Minimal APIs, but endpoint mappings are grouped by feature in dedicated extension classes:

- `ProductEndpoints`
- `CategoryEndpoints`
- `AuthEndpoints`

This keeps `Program.cs` focused on application setup, middleware, and dependency registration while preserving the existing Minimal API style.

### Third-Party Auth

JWT-based third-party authentication support was added.

Implemented endpoints:

- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh-token`
- `GET /api/v1/auth/profile`

The login endpoint forwards user credentials to the third-party auth API and returns both access and refresh tokens. The refresh-token endpoint forwards a caller-provided refresh token to the third-party API and returns the renewed token response.

The profile endpoint requires a bearer token in the `Authorization` header. The custom `ThirdPartyBearerAuthenticationHandler` validates the token against the third-party profile endpoint, creates claims from the returned profile, and lets the endpoint return the authenticated profile from those claims.

Swagger/OpenAPI includes a bearer security definition so the protected profile endpoint can be exercised from Swagger UI.

Server-side token caching was not added intentionally. The product and category endpoints used by this application do not require third-party authorization, and the auth endpoints follow the third-party API contract by accepting caller-provided credentials, access tokens, and refresh tokens instead of storing or reusing a configured service token internally.

### Request Logging and Correlation

A custom middleware was added to log one structured API boundary event per incoming request.

The middleware logs:

- HTTP method
- request path
- status code
- elapsed time in milliseconds
- endpoint display name with resolved route values, for example `HTTP: GET /api/v1/categories/42`
- authenticated state and user id when available
- request host
- `CorrelationId`
- ASP.NET Core `RequestId`
- distributed trace id and span id
- `LogType = IncomingRequest`

The middleware supports the `X-Correlation-ID` request header. If the caller provides it, the same value is included in the structured log and echoed back in the response header. If it is not provided, the middleware falls back to the ASP.NET Core request identifier.

The middleware also opens a logging scope with the correlation and request fields, so downstream framework/infrastructure logs can be tied back to the same incoming request.

Expected and handled outcomes such as `400`, `401`, and `404` are logged only as request summary events. Application services do not emit additional logs for those expected outcomes, which avoids duplicate logs for a single request. Unhandled exceptions and `5xx` responses are logged as errors by the request middleware.

The third-party bearer authentication handler is configured to suppress framework-level auth challenge information logs:

```json
"CSharpApp.Api.Auth.ThirdPartyBearerAuthenticationHandler": "Warning"
```

Serilog was configured for structured console logging and rolling daily file logs.

### API Documentation

OpenAPI/Swagger support was added for local development.

When the API runs in the `Development` environment, the OpenAPI document and Swagger UI are available at:

```text
/swagger/v1/swagger.json
/swagger
```

Swagger UI is intended as a development and review aid for exploring the implemented endpoints.

### Health Check

A lightweight health check endpoint was added for local verification, Docker/runtime checks, and operational readiness.

```text
GET /health
```

The endpoint uses the built-in ASP.NET Core health checks infrastructure and returns `200 OK` when the API host is healthy.

### Tests

Unit tests were added for the application services using fake HTTP handlers, covering successful responses and important failure paths.

Integration tests were added for API endpoints using `WebApplicationFactory`. These tests replace the real application services with fake implementations so endpoint behavior can be verified without depending on the external third-party API.

Current test coverage includes:

- products service tests
- categories service tests
- auth service tests
- CQRS command and query handler tests
- products endpoint tests
- categories endpoint tests
- auth endpoint tests

Run all tests:

```powershell
dotnet test src\CSharpApp.sln
```

Dockerized test split:

- Unit/application tests, including CQRS command and query handler tests, run during the main Docker image build as a build gate.
- Integration/API tests run from a dedicated Compose file so they have their own container lifecycle.

Run integration tests with Docker Compose:

```powershell
docker compose -f docker-compose.test.yml run --rm --build integration-tests
```

### Docker Support

Docker support was added with a multi-stage Dockerfile.

The Docker build:

1. restores the full solution
2. runs the application unit tests
3. publishes the API only if unit tests pass
4. creates a clean runtime image containing only the published API output

Build the Docker image:

```powershell
docker build -t csharpapp-api .
```

Run with Docker:

```powershell
docker run --rm -p 8080:8080 csharpapp-api
```

Run with Docker Compose:

```powershell
docker compose up --build
```

Run integration tests with the dedicated test Compose file:

```powershell
docker compose -f docker-compose.test.yml run --rm --build integration-tests
```

The API will be available at:

```text
http://localhost:8080
```

## Verification

The solution was verified with:

```powershell
dotnet test src\CSharpApp.sln
docker build -t csharpapp-api .
docker compose build api
docker compose -f docker-compose.test.yml run --rm --build integration-tests
```

The Docker build executes the application unit test project inside the build container before publishing the final API image. Integration tests are executed separately through `docker-compose.test.yml`.

## Notes

- The implementation keeps the existing architecture and avoids introducing unnecessary abstractions.
- CQRS was introduced in the application layer using MediatR while keeping the service layer as the boundary to the third-party API.
- API endpoints return structured problem responses for expected error cases such as missing resources or rejected authentication.
