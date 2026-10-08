Steamfitter.Api has an automated test suite in the `Steamfitter.Api.Tests` project. This document details how the suite is built, how to run it, and what is specific to Steamfitter. The parts every Crucible API shares (the harness files, the pinned packages, the conventions tests follow, the defect documents) are described once, in `agent-docs/api-testing/` of the Crucible workspace: `README.md` for the harness and `CONVENTIONS.md` for how tests are written. This document points there rather than repeating them.

# Testing

The suite is built on xUnit v3 and NSubstitute, and runs against a real PostgreSQL instance started in a container. It is on the Crucible API test standard: the files under `Steamfitter.Api.Tests/Support/Shared/`, the test project's `Directory.Build.props`, `xunit.runner.json`, `coverlet.runsettings` and `.github/workflows/build-and-test.yml` are copied from the standard by its `sync.sh` and are not edited here. A typical test sends an HTTP request to the application hosted in process, through the real routes, MVC filters, claims transformer, authorization handlers, AutoMapper profiles and a real database with the real migrations, then asserts on the response and on what changed in the database. Only the collaborators that leave the process are replaced.

# Running the tests

```bash
dotnet test Steamfitter.Api.Tests
```

Docker must be running. The suite starts and disposes its own PostgreSQL container (`postgres:16-alpine`) through Testcontainers. The container starts when the first test asks for a database, so the tests that take none (the mapping, filter and handler tests) run without Docker.

A single class or test:

```bash
dotnet test Steamfitter.Api.Tests --filter "FullyQualifiedName~.TaskControllerTests"
dotnet test Steamfitter.Api.Tests --filter "FullyQualifiedName~.TaskControllerTests.Execute_is_forbidden_for_a_member_holding_only_EditScenario"
```

The leading dot keeps `ScenarioControllerTests` from also matching `ScenarioTemplateControllerTests`.

To see which database the run used, pass the diagnostic flag on the command line (never in `xunit.runner.json`, where it makes the run hang after the last test):

```bash
dotnet test Steamfitter.Api.Tests -- xUnit.DiagnosticMessages=true
```

The run then prints `[Steamfitter.Api.Tests] database provider: PostgreSQL (postgres:16-alpine, real migrations)` once.

# Coverage

```bash
dotnet test Steamfitter.Api.Tests --collect:"XPlat Code Coverage"
```

`coverlet.runsettings` is applied automatically (the test project names it in `RunSettingsFilePath`), with the collector disabled by default, so a plain run collects nothing and a coverage run cannot lose its exclusions: `Steamfitter.Api.Migrations.PostgreSQL`, any `**/Migrations/**` source, the `Crucible.Common.EntityEvents` sources that compile into `Steamfitter.Api.Data`, generated code and auto-properties. coverlet's cobertura output lists every class twice; read `lines-covered` and `lines-valid` on the root element rather than summing classes. Coverage is a local diagnostic; CI neither collects nor gates on it.

# Build settings

`Steamfitter.Api.Tests/Directory.Build.props` (shared) turns on `TreatWarningsAsErrors` for the test project only, which makes the xUnit analyzers fail the build: xUnit1051 (an awaited call without the test's cancellation token), xUnit1026, xUnit2000, xUnit2012. The root `.editorconfig` raises xUnit1004, so a `[Fact(Skip = ...)]` fails the build too. Restore warnings stay warnings (`WarningsNotAsErrors`: the NuGet audit NU1901-NU1904, AutoMapper 13 and MediatR 12 being pinned for licensing, NU1510, and TinCan's NU1701). The application projects are unchanged.

The repository has no central package management, so the test project carries its package versions itself; they are exactly the standard's `test-packages.props`, which `sync.sh --check` verifies, including its optional `Microsoft.AspNetCore.SignalR.Client` 10.0.1 for `EngineHub`'s real-connection tests. One more reference is Steamfitter's own: `Newtonsoft.Json` 13.0.3. `Steamfitter.Api` compiles against 13.0.3, which reaches it only through `Microsoft.EntityFrameworkCore.Design` (`PrivateAssets=all`), so a project referencing the API would otherwise get IdentityModel's 11.0.2 and MSBuild reports the conflict (MSB3277).

The test project keeps `ImplicitUsings` off, like the Steamfitter projects, so every file lists its usings. Two names clash and are aliased where a file needs both: `TaskStatus` (`Steamfitter.Api.Data` and `System.Threading.Tasks`) and `Task` (the view model, imported as `SAVM.Task` through `using SAVM = Steamfitter.Api.ViewModels;`).

The suite runs in VSTest mode of `dotnet test`, as every Crucible API does; the standard's README ("Microsoft guidance") says why Microsoft.Testing.Platform mode is not used yet.

# How the harness works

The shared base classes and what each provides (`Db`, `NewContext()`, `Mediator`, `Ct`, `Seed`, `WaitUntil`; `Root`, `RootClient`, `Actor()`, `Client(actor)`, `Client()`, `ReadAsync`, `AssertStatus`, `AssertProblem`) are described in the standard's README. Steamfitter's own pieces:

- `DatabaseFixture` wraps the shared `PostgresTestDatabase<SteamfitterContext>`: database prefix `steamfitter`, migrations from `Steamfitter.Api.Migrations.PostgreSQL`. It is static because the host takes a database of its own (below).
- `SteamfitterAppFactory` hosts the application once for the run (an xUnit v3 assembly fixture in `AssemblyFixtures.cs`).
- `DatabaseTestBase`, `ApiTestBase` and `ServiceTestBase` are the three base classes. Errors are asserted with the shared `AssertJsonError(status, response)`, because `JsonExceptionFilter` answers a thrown exception with a `ProblemDetails` body under `application/json` (a `JsonResult`), where `AssertProblem` expects `application/problem+json`. A handled exception's message is the error's `Title`; a 500's is its `Detail`. `AssertProblem` is still right for model-binding failures, which `[ApiController]` answers with `application/problem+json`.
- `TestActor` / `TestActorBuilder` seed the rows `UserClaimsService` reads: `WithAllSystemPermissions()` (the seeded Administrator role), `WithSystemPermissions(...)` and `WithRole(...)` for the user's system role; `OnScenario(id, roleId | permissions)` and `OnScenarioTemplate(...)` for memberships (one of the two is required, because the column default role, `Member`, grants view and edit); `OnNewScenario(...)` and `OnNewScenarioTemplate(...)`, which mint a resource of their own, for the near miss that holds the right permission on another resource; and `InGroup(groupId)`, whose group memberships then grant.
- `TestData` holds the object mothers and the ids of the seeded rows: the system roles (`Administrator`, `ContentDeveloper`, `Observer`), the scenario roles (`Manager`, `Observer`, `Member`, `Facilitator`) and the template roles (`Manager`, `Observer`, `Member`), all from `HasData` in `Steamfitter.Api.Data/Models`.
- `ClaimsPrincipalBuilder` writes the claims the transformer produces (`Permission`, and the JSON `ScenarioPermission` / `ScenarioTemplatePermission` claims), for the authorization handler tests and the service tests only; an HTTP test's caller is always an actor.
- `AuthorizationHarness` wires the three permission handlers as `AuthorizationPolicyExtensions` does, and builds Steamfitter's `AuthorizationService` over a principal and a context.
- `TestMapper` is the real AutoMapper configuration, including a private copy of Startup's internal `IgnoreNullSourceValues` convention.
- `ApiTestHost` (with `ServiceTestBase`) builds, per principal, the services the hub and the two background services resolve, with recorders of its own: `Hub` (`EngineHub`), `Executors` and `OutboundHttp`. Its context is scoped and comes from the test's session rather than being the test's single `Db`, because `TaskExecutionService`, `TaskMaintenanceService` and `ScoringService` open scopes of their own and dispose the context they resolve. `ApiTestHostTests` constructs each registered service, so a missing registration fails one test by name.

## What is real, and what is not

`SteamfitterAppFactory` boots the real `Startup` through `Program.Main`, in the `Production` environment, so `JsonExceptionFilter` answers as deployed. What is not the application's own:

- **Token validation.** The shared `TestAuthHandler` mints the identity from `X-Test-User` and `X-Test-Name`, with the scopes of `Authorization:AuthorizationScope` (which both the MVC `AuthorizeFilter` and the default policy require; a test sends `X-Test-Scope` to drop one, `Infrastructure/Extensions/AuthorizationPolicyExtensionTests`). It is registered twice, under `Test` (the default scheme, which the controllers' `[Authorize]` uses) and under `Bearer`, the scheme `EngineHub` names (`[Authorize(AuthenticationSchemes = "Bearer")]`): the standard's Bearer recipe, which first removes every `IConfigureOptions<AuthenticationOptions>` because Startup's `AddJwtBearer` has claimed the name. Every client also carries `Authorization: Bearer steamfitter-tests` (`SteamfitterAppFactory.BearerToken`, set in `ConfigureClient`), because the request-scoped Player and Player VM clients copy the caller's `Authorization` header onto their own requests and throw while being constructed when there is none, which would fail every controller that reaches `TaskService`. The handler ignores the header.
- **The context registration.** A request resolves the database of the test that sent it by its `X-Test-Session` header (the shared `TestDatabaseScope.ReplaceRegistration`), in its two-argument form. `Program.Main` has no switch that skips `InitializeDatabase`, which resolves a context from a scope of its own while the host starts, outside any request, where the shared registration throws ("resolved outside a request"). So the host gets a throwaway clone of the template (the standard's step 1B), set as host settings (`Database:Provider`, `ConnectionStrings:PostgreSQL`) because `Main` reads them first, and until `CreateHost` returns a resolution with no request gets that database, over the host session's own services, so the seed's entity events reach no handler or recorder. After start-up such a resolution throws again, so a stray one still fails loudly. The factory also builds its one host under a lock in `CreateHost`: `WebApplicationFactory` starts its server without a lock, so tests asking for their first client at once could each build a host, and the second host's `InitializeDatabase` would then throw. Every way into the host (`CreateClient`, `Services`, `Server`) passes through `CreateHost`, so the lock covers them all.
- **The collaborators that leave the process**, all recorders fixed at construction (no `Substitute.For` in the run-wide factory):
  - `IHubContext<EngineHub>` is `Factory.Hub<EngineHub>()`: most of Steamfitter's SignalR handlers send through `Clients.Groups(...)`, and the shared recorder records such a send once per group, so `ToGroup(id)` reads it.
  - `IHttpClientFactory` over `Factory.OutboundHttp`, which answers the generated Player (`http://localhost:4300/`) and Player VM (`http://localhost:4302/`) clients; arrange a url of your own (the VM url carries the view id).
  - `IVmOperationsService`, `ISshService` and `IEmailService` over `Factory.Executors` (`TaskActionRecorder`). Nothing in the host calls them while the task runner is removed; they are there so nothing can reach a network if that changes.
  - The hosted services (`TaskExecutionService`, `TaskMaintenanceService`, `XApiBackgroundService`) are removed. Task execution endpoints therefore only reset the task and queue it on the in-process `TaskExecutionQueue`; `Services/TaskExecutionServiceTests` and `Services/TaskMaintenanceServiceTests` drive the two services directly over an `ApiTestHost`, waiting for their effects with `WaitUntil` (their work starts in `StartAsync` and completes nowhere a test can await). Their loops never end; each blocks on its own host's queue, or fails quietly once its host is disposed. With the startup task never run, `api/health/ready` answers 503 in the harness.

`TestConfiguration` overrides only `Database:Provider` (appsettings.json ships Sqlite, which is never migrated) and `ClaimsTransformation:EnableCaching` (off, because the cache is keyed on user id and the host serves the whole run). The shipped `UseRolesFromIdP` and `UseGroupsFromIdP` stay on and are inert here: `TestAuthHandler` mints no role or group claims, so the rows a test seeds are what decide. `UserClaimsServiceTests` covers the token paths and the cache.

## The hub

`Hubs/EngineHubTests` invokes `EngineHub`'s methods directly over a `HubHarness` (which groups a connection joins and leaves), the caller a `ClaimsPrincipalBuilder` principal read by the real `AuthorizationService` built in an `ApiTestHost`. `Hubs/EngineHubConnectionTests` connects to `/hubs/engine` over a real SignalR connection: WebSockets with `SkipNegotiation`, through `Factory.Server.CreateWebSocketClient()` carrying the actor's `X-Test-User`, `X-Test-Name` and `X-Test-Session` headers, so the hub's context is the test's database (under long polling an invocation runs outside any request and `TestDatabaseScope` could not pick one). It proves the hub's own `[Authorize]`: an actor connects and joins a scenario's group (a send to that group through the host's `HubLifetimeManager<EngineHub>` reaches it), a negotiate request with no identity is a 401, and one whose token lacks any of the three scopes is a 403.

## Isolation

Each test gets its own database, cloned from the migrated template; the host is shared by the run and the tests run in parallel. So assert on keys a test owns: a scenario's or template's own group on `Factory.Hub<EngineHub>()` (or the administrator groups filtered by the test's id), a url with the test's own id on `Factory.OutboundHttp`, a marker in the task's parameters on `Executors`. Steamfitter's own singletons (`TaskExecutionQueue`, `TaskMaintenanceServiceHealthCheck`, `StartupHealthCheck`) are shared too; no test reads the queue, because reading it takes items off it.

# Adding a test

Follow `agent-docs/api-testing/CONVENTIONS.md` section 2. In short: put the file where the code lives (`Controllers/`, `Services/`, `Hubs/`, `Infrastructure/...`), derive from `ApiTestBase` for a request, `ServiceTestBase` for a background service or the hub, `DatabaseTestBase` for the database alone, or nothing; forward the fixtures (`XTests(DatabaseFixture fixture, SteamfitterAppFactory factory) : ApiTestBase(fixture, factory)`); seed with `TestData` and `Actor()`; name the test as a sentence; pass `Ct`; re-read through `NewContext()`.

Every gate gets an allowed test with exactly the permission the gate names and denied tests that are near misses, named for what the caller holds: `..._is_forbidden_for_a_caller_holding_only_ViewScenarios`, `..._is_forbidden_for_a_member_holding_only_EditScenario`, `..._is_forbidden_for_a_caller_holding_ViewScenario_only_on_another_scenario` (seeded with `OnNewScenario`). Each controller has one `An_unauthenticated_request_is_unauthorized`.

A defect is characterized by a passing test of the current behaviour, with at most a one-line neutral `/// <summary>`, and described in `agent-docs/api-test-bugs/steamfitter.api.md` (CONVENTIONS.md section 3); never in a test comment.

# Layout

```
Steamfitter.Api.Tests/
  Controllers/       one class per controller, over HTTP: authorization, persistence, broadcasts, error shapes
  Services/          TaskExecutionService, TaskMaintenanceService, UserClaimsService, driven directly
  Hubs/              EngineHub over HubHarness, and over a real connection (EngineHubConnectionTests)
  Infrastructure/
    Authorization/   the permission handlers and AuthorizationService
    Extensions/      the scopes the default policy and the MVC AuthorizeFilter require
    Filters/         JsonExceptionFilter, malformed requests and enum values
    Mappings/        the AutoMapper configuration
  Support/           Steamfitter's harness files, its extras and the harness self-tests
    Shared/          the standard's shared files, copied by sync.sh and never edited here
```

Steamfitter's extras in `Support/`: `TaskActionRecorder`, `ApiTestHost` / `ServiceTestBase` and `ApiTestHostTests`. The harness self-tests are `DatabaseHarnessTests` (isolation probed on the uniquely indexed `system_roles.name`), `HttpHarnessTests` and `TestActorTests`.

# Continuous integration

`.github/workflows/build-and-test.yml` is the standard's workflow with this repository's test project filled in (`sync.sh` keeps it identical): restore, build, `dotnet test Steamfitter.Api.Tests -- xUnit.DiagnosticMessages=true`, a grep for the `[Steamfitter.Api.Tests] database provider: PostgreSQL` banner, and the TRX of the run as the `test-results` artifact, uploaded even when the job fails. It runs on pull requests and on pushes to `main`, with the SDK from `global.json`, and has no `services: postgres:` block because Testcontainers manages the container. The repository had no test workflow before.
