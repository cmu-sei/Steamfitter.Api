# Steamfitter.Api.Tests.Integration

Copyright 2026 Carnegie Mellon University. All Rights Reserved.
Released under a MIT (SEI)-style license.

## Purpose

Integration tests for Steamfitter API HTTP endpoints. Uses `WebApplicationFactory<Program>` with Testcontainers to spin up a real PostgreSQL database and test the full application stack.

## Files

### Test Infrastructure

- **`Fixtures/SteamfitterTestContext.cs`** - WebApplicationFactory-based test context
  - Implements `IAsyncLifetime` for container lifecycle management
  - Spins up a PostgreSQL container using Testcontainers
  - Configures test authentication and authorization bypass
  - Provides `GetDbContext()` helper for database seeding
  - Includes `TestAuthenticationHandler` that always succeeds with a test user
  - Includes `TestAuthorizationService` that always permits access

### Controller Tests

- **`Tests/Controllers/HealthCheckTests.cs`** - Health endpoint tests
- **`Tests/Controllers/UserControllerTests.cs`** - User API endpoint tests
  - GET /api/users (list all users)
  - GET /api/users/{id} (get user by ID)
  - POST /api/users (create user)
  - DELETE /api/users/{id} (delete user)

## Key Patterns

### Test Class Structure

```csharp
public class UserControllerTests(SteamfitterTestContext context)
{
    private readonly HttpClient _client = context.CreateClient();
    private readonly SteamfitterTestContext _context = context;

    [Test]
    public async Task GetUsers_ReturnsOkAndList()
    {
        // Arrange - seed database
        using var dbContext = _context.GetDbContext();
        var userEntity = new UserEntity { Id = Guid.NewGuid(), Name = "Test User" };
        dbContext.Users.Add(userEntity);
        await dbContext.SaveChangesAsync();

        // Act - make HTTP request
        var response = await _client.GetAsync("/api/users");

        // Assert - verify response
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<UserViewModel>>();
        await Assert.That(users).IsNotNull();
        await Assert.That(users).Contains(u => u.Id == userEntity.Id);
    }
}
```

### Test Context Configuration

The `SteamfitterTestContext` configures:

1. **Database:** Real PostgreSQL via Testcontainers (latest image)
2. **Authentication:** Test scheme that always succeeds with a predefined test user ID
3. **Authorization:** Bypass service that always permits access
4. **External Services:** Replaces StackStorm and TaskExecutionQueue with FakeItEasy mocks
5. **Hosted Services:** Removed to avoid background job interference
6. **Environment:** Sets `Test` environment with OIDC settings

Implements `IAsyncInitializer` and `IAsyncDisposable` for container lifecycle management.

## Dependencies

- **Testing Frameworks:**
  - TUnit 1.19.22
  - Microsoft.AspNetCore.Mvc.Testing 10.0.1
  - coverlet.collector 6.0.2 (code coverage)

- **Testcontainers:**
  - Testcontainers.PostgreSql 4.0.0

- **Mocking:**
  - FakeItEasy 8.3.0
  - AutoFixture 4.18.1
  - AutoFixture.AutoFakeItEasy 4.18.1

- **Database:**
  - Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0

- **Project References:**
  - Steamfitter.Api
  - Steamfitter.Api.Data
  - Steamfitter.Api.Tests.Shared
  - Crucible.Common.Testing

## Running Tests

```bash
# Run all integration tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~UserControllerTests"

# Run specific test
dotnet test --filter "FullyQualifiedName~UserControllerTests.GetUsers_ReturnsOkAndList"
```

## Notes

- Each test session gets a fresh PostgreSQL container (via `[ClassDataSource<SteamfitterTestContext>(Shared = SharedType.PerTestSession)]`)
- Container automatically starts in `InitializeAsync()` and stops in `DisposeAsync()`
- Tests use `_context.CreateClient()` to get an authenticated `HttpClient`
- Database seeding done per test via `_context.GetDbContext()`
- All tests run against the full API stack with EF Core migrations applied
- Test user ID: `9fd3c38e-58b0-4af1-80d1-1895af91f1f9`

## Container Lifecycle

```
Test Class Start → InitializeAsync() → Container Starts → PostgreSQL Ready
                                      ↓
                            Tests Execute (CreateClient, GetDbContext)
                                      ↓
Test Class End   → DisposeAsync()   → Container Stops → Cleanup
```

The PostgreSQL container uses:
- Username: `steamfitter`
- Password: `steamfitter`
- Image: `postgres:latest`
- Auto-remove and cleanup enabled
