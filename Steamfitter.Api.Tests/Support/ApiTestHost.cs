// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// Steamfitter: the services the hub and the two background services (TaskExecutionService,
// TaskMaintenanceService) resolve, as Startup.ConfigureServices registers them, with the collaborators
// that leave the process replaced by recorders this host owns. The context is scoped and built from the
// test's session (rather than the template's single Db instance), because both background services and
// ScoringService create scopes of their own and dispose the context they resolve.

using System;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steamfitter.Api.Data;
using Steamfitter.Api.Hubs;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Infrastructure.HealthChecks;
using Steamfitter.Api.Infrastructure.Identity;
using Steamfitter.Api.Infrastructure.Options;
using Steamfitter.Api.Services;

namespace Steamfitter.Api.Tests.Support;

/// <summary>The application's services without the web host, over one test's database, as one user.</summary>
public sealed class ApiTestHost : IDisposable
{
    private readonly ServiceProvider _services;

    private ApiTestHost(ServiceProvider services, HubRecorder<EngineHub> hub, TaskActionRecorder executors, StubHttpMessageHandler outboundHttp)
    {
        _services = services;
        Hub = hub;
        Executors = executors;
        OutboundHttp = outboundHttp;
    }

    /// <summary>What this host's services broadcast on <c>EngineHub</c>.</summary>
    public HubRecorder<EngineHub> Hub { get; }

    /// <summary>What this host's task executors were asked to do.</summary>
    public TaskActionRecorder Executors { get; }

    /// <summary>Answers this host's outbound HTTP.</summary>
    public StubHttpMessageHandler OutboundHttp { get; }

    public T Resolve<T>() where T : notnull => _services.GetRequiredService<T>();

    public static ApiTestHost Create(ITestDatabaseSession<SteamfitterContext> session, ClaimsPrincipal user, Action<ApiTestHostOptions> configure = null)
    {
        var options = new ApiTestHostOptions();
        configure?.Invoke(options);

        var hub = new HubRecorder<EngineHub>();
        var executors = new TaskActionRecorder();
        var outboundHttp = new StubHttpMessageHandler();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();

        // Scoped, from the session: the services under test open scopes of their own and dispose what
        // they resolve, as they do in production.
        services.AddScoped(_ => session.CreateContext());
        services.AddSingleton(TestMapper.Mapper);

        services.AddSingleton<IHttpContextAccessor>(
            new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = user } });
        services.AddScoped<IPrincipal>(p => p.GetRequiredService<IHttpContextAccessor>().HttpContext.User);
        services.AddScoped<IIdentityResolver, IdentityResolver>();

        services.AddAuthorization();
        services.AddSingleton<IAuthorizationHandler, SystemPermissionHandler>();
        services.AddSingleton<IAuthorizationHandler, ScenarioPermissionHandler>();
        services.AddSingleton<IAuthorizationHandler, ScenarioTemplatePermissionHandler>();
        services.AddScoped<ISteamfitterAuthorizationService, AuthorizationService>();

        AddOptions(services, options.VmTaskProcessing);
        AddOptions(services, options.HttpTask);
        AddOptions(services, options.Client);
        AddOptions(services, options.ResourceOwnerAuthorization);

        services.AddSingleton<IHubContext<EngineHub>>(hub);
        services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(outboundHttp));
        services.AddSingleton<IVmOperationsService>(executors);
        services.AddSingleton<ISshService>(executors);
        services.AddSingleton<IEmailService>(executors);

        services.AddSingleton<ITaskExecutionQueue, TaskExecutionQueue>();
        services.AddScoped<IScoringService, ScoringService>();
        services.AddSingleton<StartupHealthCheck>();
        services.AddSingleton<TaskMaintenanceServiceHealthCheck>();
        services.AddSingleton<TaskExecutionService>();
        services.AddSingleton<TaskMaintenanceService>();
        services.AddScoped<EngineHub>();

        return new ApiTestHost(services.BuildServiceProvider(), hub, executors, outboundHttp);
    }

    /// <summary>The three shapes Startup's options take: the object itself, IOptions and IOptionsMonitor.</summary>
    private static void AddOptions<T>(IServiceCollection services, T value) where T : class
    {
        services.AddSingleton(value);
        services.AddSingleton(Options.Create(value));
        services.AddSingleton<IOptionsMonitor<T>>(new FixedOptionsMonitor<T>(value));
    }

    public void Dispose() => _services.Dispose();

    private sealed class FixedOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;

        public T Get(string name) => value;

        public IDisposable OnChange(Action<T, string> listener) => null;
    }
}

/// <summary>The configuration-backed options an <see cref="ApiTestHost"/> exposes, defaulted so every service runs.</summary>
public sealed class ApiTestHostOptions
{
    /// <summary>
    /// appsettings.json's values, except the two loop periods: one second, so a background service's
    /// next pass comes within a test's wait.
    /// </summary>
    public VmTaskProcessingOptions VmTaskProcessing { get; } = new()
    {
        HealthCheckSeconds = 1,
        HealthCheckTimeoutSeconds = 90,
        TaskProcessIntervalMilliseconds = 5000,
        TaskProcessMaxWaitSeconds = 120,
        ExpirationCheckSeconds = 1,
        HttpTimeoutSeconds = 90,
        HttpHeaderReplacements = [],
        ApiParameters = []
    };

    /// <summary>No allow-list, as shipped.</summary>
    public HttpTaskOptions HttpTask { get; } = new();

    public ClientOptions Client { get; } = new()
    {
        urls = new ApiUrlSettings { playerApi = "http://player.test/", vmApi = "http://vm.test/" }
    };

    public ResourceOwnerAuthorizationOptions ResourceOwnerAuthorization { get; } = new()
    {
        Authority = "http://identity.test",
        ClientId = "steamfitter.api",
        Scope = "player-vm"
    };
}
