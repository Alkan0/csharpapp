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
- timeout configuration
- typed clients for products, categories, and auth services

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

Delete was not added because it was not part of the README requirements or the provided API collection flow.

### Third-Party Auth

JWT-based third-party authentication support was added.

Implemented endpoints:

- `POST /api/v1/auth/login`
- `GET /api/v1/auth/profile`

The login endpoint forwards user credentials to the third-party auth API. The profile endpoint requires a bearer token in the `Authorization` header and forwards that token to the third-party profile endpoint.

### Request Performance Logging

A custom middleware was added to measure and log API request performance.

The middleware logs:

- HTTP method
- request path
- status code
- elapsed time in milliseconds

Serilog was configured for structured console logging and rolling daily file logs.

### Tests

Unit tests were added for the application services using fake HTTP handlers, covering successful responses and important failure paths.

Integration tests were added for API endpoints using `WebApplicationFactory`. These tests replace the real application services with fake implementations so endpoint behavior can be verified without depending on the external third-party API.

Current test coverage includes:

- products service tests
- categories service tests
- auth service tests
- products endpoint tests
- categories endpoint tests
- auth endpoint tests

Run all tests:

```powershell
dotnet test src\CSharpApp.sln
```

### Docker Support

Docker support was added with a multi-stage Dockerfile.

The Docker build:

1. restores the full solution
2. runs unit and integration tests
3. publishes the API only if tests pass
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

The API will be available at:

```text
http://localhost:8080
```

## Verification

The solution was verified with:

```powershell
dotnet test src\CSharpApp.sln
docker build -t csharpapp-api .
```

The Docker build also executes the test suite inside the build container before publishing the final API image.

## Notes

- The implementation keeps the existing architecture and avoids introducing unnecessary abstractions.
- CQRS was considered, but it was not included in the main implementation because the current service-based structure remains simple and readable for the project size.
- API endpoints return structured problem responses for expected error cases such as missing resources or rejected authentication.
