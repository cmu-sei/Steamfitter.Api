// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// Steamfitter: the run-wide factory with step 1B (Program.Main runs InitializeDatabase with no switch to
// skip it), one hub (EngineHub, recorded by the shared HubRecorder, which records a Clients.Groups(list)
// send for each group), the Player and Player VM clients over OutboundHttp, and the task executors over
// TaskActionRecorder.

using System.Collections.Concurrent;
using System.Net.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Steamfitter.Api.Data;
using Steamfitter.Api.Hubs;
using Steamfitter.Api.Services;

namespace Steamfitter.Api.Tests.Support;

/// <summary>
/// Hosts <c>Steamfitter.Api</c> in process over <c>TestServer</c>, so that tests drive the real
/// application: the real <c>Startup</c>, the real MVC filters, the real authorization stack and the real
/// claims transformer.
/// </summary>
/// <remarks>
/// <para>
/// One instance serves the whole run, declared in <c>AssemblyFixtures.cs</c>. Everything the application
/// registers as a singleton is therefore shared by every test, which is what
/// <see cref="TestConfiguration"/>'s claims-caching entry and <see cref="TestDatabaseScope"/> exist to
/// deal with. Only three things are not the application's own: token validation
/// (<see cref="TestAuthHandler"/>), the context registration (<see cref="TestDatabaseScope"/>, with the
/// step 1B fallback to the host's own database during startup), and the collaborators that leave the
/// process.
/// </para>
/// <para>
/// No Crucible API's <c>Program</c> matches the <c>CreateHostBuilder</c> convention
/// <c>HostFactoryResolver</c> looks for, so <c>WebApplicationFactory</c> invokes <c>Program.Main</c>, and
/// Steamfitter's <c>Main</c> calls <c>InitializeDatabase</c> with no switch to skip it. The host gets a
/// throwaway database cloned from the template for that (step 1B); migrating it is a no-op.
/// </para>
/// </remarks>
public sealed class SteamfitterAppFactory : WebApplicationFactory<Program>, ITestHttpHost
{
    /// <summary>Answers every request the application makes over HTTP. Arrange a url of your own on it.</summary>
    public StubHttpMessageHandler OutboundHttp { get; } = new();

    /// <summary>
    /// What the task executors (Player VM API, SSH, SMTP) were asked to do. Nothing in the host calls them
    /// while the hosted <c>TaskExecutionService</c> is removed; they are registered so nothing can reach a
    /// network if that changes.
    /// </summary>
    public TaskActionRecorder Executors { get; } = new();

    private readonly ConcurrentDictionary<System.Type, object> _hubs = new();

    /// <summary>What the application broadcast through a hub, per audience.</summary>
    public HubRecorder<THub> Hub<THub>() where THub : Hub =>
        (HubRecorder<THub>)_hubs.GetOrAdd(typeof(THub), _ => new HubRecorder<THub>());

    /// <summary>The bearer token every client sends, which the Player clients forward.</summary>
    public const string BearerToken = "steamfitter-tests";

    /// <summary>
    /// Every client carries an <c>Authorization</c> header, as a browser's request does. The request-scoped
    /// Player and Player VM clients (<c>AddPlayerApiClient</c>, <c>AddPlayerVmApiClient</c>) copy it onto
    /// their own requests and throw <c>FormatException</c> while being constructed when there is none,
    /// which fails every controller that reaches <c>TaskService</c>. <see cref="TestAuthHandler"/> ignores
    /// the header: identity still comes from <c>X-Test-User</c>, and a client without it is still a 401.
    /// </summary>
    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", BearerToken);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Production, so the developer exception page stays off and JsonExceptionFilter answers a 500 as
        // deployed: the message in `detail`, a fixed `title`.
        builder.UseEnvironment("Production");

        // 1B. Main's InitializeDatabase migrates and seeds the configured database, so the host gets a
        //     throwaway database of its own. Host settings, because Main reads them before the in-memory
        //     collection below is layered in.
        builder.UseSetting("Database:Provider", "PostgreSQL");
        builder.UseSetting("ConnectionStrings:PostgreSQL", DatabaseFixture.HostDatabase().ConnectionString);

        builder.ConfigureAppConfiguration(configuration =>
            configuration.AddInMemoryCollection(TestConfiguration.Values));

        builder.ConfigureTestServices(services =>
        {
            // TaskExecutionService, TaskMaintenanceService and XApiBackgroundService start in the
            // background and reach for databases and services no test owns. Each that matters gets a
            // test of its own that drives it directly (Services/).
            services.RemoveAll<IHostedService>();

            services
                .AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);

            // InitializeDatabase resolves the context from a scope of its own, outside any request. Until
            // the host has started, such a resolution gets the host's own database, over that session's
            // own services, so the seed's entity events never reach the real handlers or the recorder;
            // afterwards it throws, so a stray resolution outside a request still fails loudly.
            TestDatabaseScope.ReplaceRegistration<SteamfitterContext>(
                services, () => _started ? null : DatabaseFixture.HostDatabase());

            // SignalR registers hub contexts as an open generic, which RemoveAll of a closed type cannot
            // match; a later closed registration wins on resolution.
            services.AddSingleton<IHubContext<EngineHub>>(Hub<EngineHub>());

            services.RemoveAll<IHttpClientFactory>();
            services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(OutboundHttp));

            services.Replace(ServiceDescriptor.Singleton<IVmOperationsService>(Executors));
            services.Replace(ServiceDescriptor.Singleton<ISshService>(Executors));
            services.Replace(ServiceDescriptor.Singleton<IEmailService>(Executors));
        });
    }

    private readonly System.Threading.Lock _creating = new();
    private IHost _host;

    /// <summary>Set once the host has started, after <c>Program.Main</c>'s <c>InitializeDatabase</c>.</summary>
    private volatile bool _started;

    /// <summary>Builds the one host, under a lock, and hands it to every caller.</summary>
    /// <remarks>
    /// <c>WebApplicationFactory.StartServer</c> takes no lock, so tests that ask for their first client at
    /// once could each build a host, and the second host's <c>InitializeDatabase</c> would then run after
    /// <see cref="_started"/> was set, and throw. Every way into the host (<c>CreateClient</c> in any
    /// overload, <c>Services</c>, <c>Server</c>) goes through <c>StartServer</c> to here, so this lock
    /// covers them all, where a lock around <c>CreateClient()</c> alone did not.
    /// </remarks>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        lock (_creating)
        {
            if (_host is null)
            {
                _host = base.CreateHost(builder);
                _started = true;
            }

            return _host;
        }
    }
}
