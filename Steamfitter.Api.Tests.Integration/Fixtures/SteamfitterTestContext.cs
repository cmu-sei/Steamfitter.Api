// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Security.Claims;
using Crucible.Common.EntityEvents.Extensions;
using FakeItEasy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Steamfitter.Api.Data;
using Steamfitter.Api.Services;
using Testcontainers.PostgreSql;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace Steamfitter.Api.Tests.Integration.Fixtures;

/// <summary>
/// WebApplicationFactory-based test context for Steamfitter API integration tests.
/// Uses Testcontainers to spin up a real PostgreSQL instance per test class.
/// </summary>
public class SteamfitterTestContext : WebApplicationFactory<Program>, IAsyncInitializer, IAsyncDisposable
{
    private PostgreSqlContainer? _container;

    /// <summary>
    /// Default test user ID used for authentication in integration tests.
    /// </summary>
    public static readonly Guid TestUserId = Guid.Parse("9fd3c38e-58b0-4af1-80d1-1895af91f1f9");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .UseEnvironment("Test")
            .UseSetting("Authorization:Authority", "https://localhost")
            .UseSetting("Authorization:AuthorizationScope", "steamfitter-api")
            .UseSetting("Authorization:ClientId", "steamfitter-api-test")
            .UseSetting("ClientSettings:urls:playerApi", "https://localhost")
            .UseSetting("ClientSettings:urls:vmApi", "https://localhost")
            .UseSetting("Database:Provider", "PostgreSQL")
            .ConfigureServices(services =>
            {
                if (_container is null)
                {
                    throw new InvalidOperationException(
                        "Cannot initialize test context: database container has not been started.");
                }

                var connectionString = _container.GetConnectionString();

                // Remove the existing DbContext registrations
                services.RemoveAll<DbContextOptions<SteamfitterContext>>();
                services.RemoveAll<SteamfitterContext>();
                services.RemoveAll<IDbContextFactory<SteamfitterContext>>();

                // Register with Testcontainers PostgreSQL
                services.AddEventPublishingDbContextFactory<SteamfitterContext>((sp, optionsBuilder) =>
                {
                    optionsBuilder.UseNpgsql(connectionString);
                });

                // Replace hosted services that require external dependencies
                services.RemoveAll<IHostedService>();

                // Replace StackStorm service with a fake
                var fakeStackStorm = A.Fake<IStackStormService>();
                services.RemoveAll<IStackStormService>();
                services.AddSingleton(fakeStackStorm);

                // Replace task execution queue with a fake
                var fakeTaskQueue = A.Fake<ITaskExecutionQueue>();
                services.RemoveAll<ITaskExecutionQueue>();
                services.AddSingleton(fakeTaskQueue);

                // Bypass authentication - use a test scheme that always succeeds
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });

                // Bypass authorization - allow all requests through
                services.RemoveAll<IAuthorizationService>();
                services.AddSingleton<IAuthorizationService, TestAuthorizationService>();
            });
    }

    public SteamfitterContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<SteamfitterContext>();
    }

    public async System.Threading.Tasks.Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithHostname("localhost")
            .WithUsername("steamfitter")
            .WithPassword("steamfitter")
            .WithImage("postgres:latest")
            .WithAutoRemove(true)
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
    }

    public new async ValueTask DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
        await base.DisposeAsync();
    }
}

/// <summary>
/// Authentication handler that always succeeds for integration tests.
/// </summary>
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override System.Threading.Tasks.Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim("sub", SteamfitterTestContext.TestUserId.ToString()),
            new Claim("name", "Integration Test User"),
            new Claim(ClaimTypes.NameIdentifier, SteamfitterTestContext.TestUserId.ToString()),
            new Claim("scope", "steamfitter-api")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return System.Threading.Tasks.Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Authorization service that always permits access for integration tests.
/// </summary>
public class TestAuthorizationService : IAuthorizationService
{
    public System.Threading.Tasks.Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
    {
        return System.Threading.Tasks.Task.FromResult(AuthorizationResult.Success());
    }

    public System.Threading.Tasks.Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
    {
        return System.Threading.Tasks.Task.FromResult(AuthorizationResult.Success());
    }
}
