# Steamfitter.Api.Tests.Unit

Copyright 2026 Carnegie Mellon University. All Rights Reserved.
Released under a MIT (SEI)-style license.

## Purpose

Unit tests for Steamfitter API services and AutoMapper configuration. Uses in-memory database and mocked dependencies to test business logic in isolation.

## Files

### Mapping Tests

- **`MappingConfigurationTests.cs`** - AutoMapper profile validation
  - Registers all 13 AutoMapper profiles individually:
    - ScenarioProfile, ScenarioTemplateProfile, TaskProfile, ResultProfile
    - UserProfile, VmCredentialProfile
    - GroupProfile, GroupMembershipProfile
    - ScenarioMembershipProfile, ScenarioTemplateMembershipProfile
    - ScenarioRoleProfile, ScenarioTemplateRoleProfile, SystemRoleProfile
  - Tests individual entity-to-viewmodel mappings for Scenario, User, and Task entities

### Service Tests

- **`Services/ScenarioServiceTests.cs`** - ScenarioService CRUD operation tests
- **`Services/TaskServiceTests.cs`** - TaskService CRUD operation tests
  - Uses `TaskEntity` to reference Steamfitter's task entity (avoiding `System.Threading.Tasks.Task` conflict)
  - Uses `SystemTask = System.Threading.Tasks.Task` alias for async method return types
- **`Services/UserServiceTests.cs`** - UserService CRUD operation tests

## Key Patterns

### Service Test Structure

```csharp
public class TaskServiceTests
{
    private readonly IFixture _fixture;
    private readonly IMapper _mapper;

    public TaskServiceTests()
    {
        _fixture = new Fixture().Customize(new SteamfitterCustomization())
                                .Customize(new AutoFakeItEasyCustomization());

        var config = new MapperConfiguration(cfg => cfg.AddProfile<TaskProfile>());
        _mapper = config.CreateMapper();
    }

    private (TaskService service, SteamfitterContext context) CreateService(
        Guid? userId = null,
        List<TaskEntity>? tasks = null)
    {
        var context = TestDbContextFactory.Create<SteamfitterContext>();
        // ... mock dependencies with FakeItEasy
        // ... seed test data if provided
        return (service, context);
    }

    [Test]
    public async SystemTask GetAsync_ReturnsAllTasks()
    {
        // Arrange, Act, Assert
    }
}
```

### Namespace Conflict Handling

The tests carefully handle the namespace conflict between `Steamfitter.Api.ViewModels.Task` and `System.Threading.Tasks.Task`:

```csharp
using SystemTask = System.Threading.Tasks.Task;

[Fact]
public async SystemTask GetAsync_ReturnsAllTasks() { ... }

var entity = new TaskEntity { ... }; // Steamfitter's task entity
```

## Dependencies

- **Testing Frameworks:**
  - TUnit 1.19.22
  - coverlet.collector 6.0.2 (code coverage)

- **Mocking & Fixtures:**
  - FakeItEasy 8.3.0
  - AutoFixture 4.18.1
  - AutoFixture.AutoFakeItEasy 4.18.1
  - MockQueryable.FakeItEasy 7.0.3

- **Entity Framework:**
  - Microsoft.EntityFrameworkCore.InMemory 10.0.1

- **Project References:**
  - Steamfitter.Api
  - Steamfitter.Api.Data
  - Steamfitter.Api.Tests.Shared
  - Crucible.Common.Testing

## Running Tests

```bash
# Run all unit tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~MappingConfigurationTests"

# Run tests in a specific file
dotnet test --filter "FullyQualifiedName~TaskServiceTests"
```

## Notes

- Tests use `TestDbContextFactory.Create<SteamfitterContext>()` for in-memory database
- All external dependencies (StackStorm, Player services, etc.) are mocked with FakeItEasy
- ClaimsPrincipal is manually constructed for authorization context
- Tests follow Arrange-Act-Assert pattern with TUnit assertions
