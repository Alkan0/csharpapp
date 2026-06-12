# Implementation Notes

This document summarizes the implementation work completed for the Accepted C# assessment, the main technical decisions, and the commands used to build, test, and run the solution.

## Solution Structure

The solution keeps the existing layered structure and avoids a large architectural rewrite:

- `CSharpApp.Api`: Minimal API endpoints, API versioning, authentication/authorization, middleware, Swagger/OpenAPI, and HTTP response handling.
- `CSharpApp.Application`: Application services that coordinate calls to the third-party API.
- `CSharpApp.Core`: DTOs, interfaces, and settings models shared across the solution.
- `CSharpApp.Infrastructure`: Dependency injection and outbound HTTP client configuration.
- `tests/CSharpApp.Application.Tests`: Unit tests for application services and third-party service wrappers.
- `tests/CSharpApp.Api.Tests`: Integration tests for API endpoints and middleware behavior.

The API remains implemented with Minimal APIs. Endpoint mappings are grouped by feature to keep `Program.cs` focused on application startup and pipeline configuration.

## HTTP Client Configuration

Outbound calls to the third-party API are centralized through typed `HttpClient` registrations using `IHttpClientFactory`.

Registered typed clients:

- `IProductsService` / `ProductsService`
- `ICategoriesService` / `CategoriesService`
- `IAuthService` / `AuthService`

Configuration is handled in:

```text
src/CSharpApp.Infrastructure/Configuration/HttpConfiguration.cs
```

The setup uses:

- `RestApiSettings.BaseUrl` for the third-party API base address
- `HttpClientSettings.LifeTime` for handler lifetime
- `HttpClientSettings.RetryCount` for retry attempts
- `HttpClientSettings.SleepDuration` for the base retry delay

Retries are implemented with Polly and are applied only to idempotent HTTP methods. This avoids retrying non-idempotent calls such as `POST /products`, `POST /categories`, `auth/login`, and `auth/refresh-token`, where automatic retries could create duplicate writes or misleading auth behavior.

The retry policy handles:

- transient HTTP failures
- `408 Request Timeout`
- `5xx` responses
- `429 Too Many Requests`

Retry delays use exponential backoff based on `SleepDuration`. Retry attempts are logged through the same `ILogger`/Serilog pipeline used by the rest of the application with `LogType = OutgoingThirdPartyRetry`.

## Products API

The products feature was expanded beyond the original `getAll` behavior.

Implemented endpoints:

- `GET /api/v1/products`
- `GET /api/v1/products/{id}`
- `POST /api/v1/products`

The API returns structured responses for missing products and validation failures.

## Categories API

Categories support was added following the same service-oriented structure as products.

Implemented endpoints:

- `GET /api/v1/categories`
- `GET /api/v1/categories/{id}`
- `POST /api/v1/categories`
- `PUT /api/v1/categories/{id}`
- `DELETE /api/v1/categories/{id}`
- `GET /api/v1/categories/{id}/products`

These endpoints cover the category flows included in the provided API collections.

## Endpoint Organization

The Minimal API endpoints are grouped into feature-specific extension classes:

- `ProductEndpoints`
- `CategoryEndpoints`
- `AuthEndpoints`

`Program.cs` wires these groups together:

```csharp
app.MapProductEndpoints()
   .MapCategoryEndpoints()
   .MapAuthEndpoints();
```

This keeps the existing Minimal API style while avoiding a large, hard-to-read startup file.

## Authentication and Authorization

Third-party JWT support was implemented through the API layer using ASP.NET Core authentication and authorization.

Public auth endpoints:

- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh-token`

Protected auth endpoint:

- `GET /api/v1/auth/profile`

Protected resource endpoints:

- products endpoints
- categories endpoints

The solution uses a custom authentication scheme:

```text
ThirdPartyBearer
```

The custom authentication handler:

1. Reads `Authorization: Bearer <access_token>`.
2. Sends the token to the third-party `/auth/profile` endpoint through `IAuthService.GetProfile(...)`.
3. Treats a successful profile response as token validation.
4. Creates a `ClaimsPrincipal` from the returned profile data.
5. Rejects missing or invalid tokens with `401 Unauthorized`.

The project does not perform local JWT signature validation because the third-party signing key/JWKS is not available. Using `/auth/profile` as the validation source is the correct approach for this API contract.

Minimal APIs are protected with:

```csharp
.RequireAuthorization()
```

This is the Minimal API equivalent of applying `[Authorize]` metadata while preserving the existing endpoint style.

The refresh-token endpoint intentionally remains public. It must be callable when the access token has expired or is no longer accepted.

## Swagger/OpenAPI

Swagger is enabled in the `Development` environment.

Development URLs:

```text
/swagger/v1/swagger.json
/swagger
```

Swagger also includes Bearer authentication support. In Swagger UI:

1. Call `POST /api/v1/auth/login`.
2. Copy the returned `access_token`.
3. Click `Authorize`.
4. Paste the token without the `Bearer` prefix.
5. Call protected endpoints.

## Request Logging and Correlation

A custom middleware logs one structured API boundary event per incoming request.

Logged fields include:

- HTTP method
- request path
- response status code
- elapsed time in milliseconds
- concrete endpoint name with resolved route values
- authentication state
- user id when available
- request host
- `CorrelationId`
- ASP.NET Core `RequestId`
- distributed trace id and span id
- `LogType = IncomingRequest`

Endpoint names replace route tokens with concrete values, so logs show values such as:

```text
HTTP: GET /api/v1/categories/42
```

instead of:

```text
HTTP: GET /api/v{version:apiVersion}/categories/{id:int}
```

The middleware supports the `X-Correlation-ID` request header. If a caller provides it, the same value is included in logs and returned in the response header. If it is not provided, the middleware falls back to the ASP.NET Core request identifier.

The middleware opens a logging scope with request and correlation fields, so framework and infrastructure logs can be tied back to the same incoming request.

Expected and handled outcomes such as `400`, `401`, and `404` are logged only as request summary events. Application services do not emit additional logs for those expected outcomes, which avoids duplicate logs for a single request.

Unhandled exceptions and `5xx` responses are logged as errors by the request middleware and then rethrown so the global exception handler can return the correct response.

The third-party bearer authentication handler is configured to suppress framework-level auth challenge information logs:

```json
"CSharpApp.Api.Auth.ThirdPartyBearerAuthenticationHandler": "Warning"
```

## Global Exception Handling

Global exception handling was added through:

```text
src/CSharpApp.Api/Middleware/ExceptionHandlingExtensions.cs
```

The API now returns consistent `ProblemDetails` responses for unhandled exceptions.

Current mappings:

- `HttpRequestException` -> `503 Service Unavailable`
- all other unhandled exceptions -> `500 Internal Server Error`

This keeps upstream third-party failures from leaking raw exception details to API clients.

## Health Check

A lightweight health endpoint is available:

```text
GET /health
```

It uses ASP.NET Core health checks and returns `200 OK` when the API host is healthy.

## Tests

Tests cover application services, endpoint behavior, middleware behavior, Swagger metadata, and important failure paths.

Current API test coverage includes:

- products endpoints
- categories endpoints
- auth endpoints
- health checks
- Swagger/OpenAPI output
- HTTP configuration validation
- request performance middleware
- global exception handling

Current application test coverage includes:

- products service behavior
- categories service behavior
- auth service behavior

Run API tests:

```powershell
dotnet test tests\CSharpApp.Api.Tests\CSharpApp.Api.Tests.csproj
```

Run application tests:

```powershell
dotnet test tests\CSharpApp.Application.Tests\CSharpApp.Application.Tests.csproj
```

Run the full solution tests:

```powershell
dotnet test src\CSharpApp.sln
```

If the API is running locally and locks build outputs, stop the running process before executing tests.

## Docker Support

Docker support was added with a multi-stage Dockerfile.

The Docker build:

1. restores the solution
2. runs the application unit tests as a build gate
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

Run integration/API tests with the dedicated Compose file:

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
dotnet test tests\CSharpApp.Api.Tests\CSharpApp.Api.Tests.csproj
dotnet test tests\CSharpApp.Application.Tests\CSharpApp.Application.Tests.csproj
```

Docker verification commands:

```powershell
docker build -t csharpapp-api .
docker compose build api
docker compose -f docker-compose.test.yml run --rm --build integration-tests
```

## Notes and Trade-Offs

- The existing Minimal API architecture was preserved.
- `.RequireAuthorization()` is used instead of controller `[Authorize]` attributes because the project is not controller-based.
- Token validation is performed through the third-party `/auth/profile` endpoint because local JWT signing metadata is not available.
- Token/profile validation is performed on each protected request. A short-lived token validation cache could improve performance in a production system, but it was intentionally not added to avoid changing token revocation semantics.
- Retries are limited to idempotent outbound HTTP methods to avoid duplicate writes.
- CQRS was considered, but the current service-based structure remains simpler and more readable for this solution size.
