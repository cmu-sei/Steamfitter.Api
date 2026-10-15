// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Steamfitter.Api.Data;
using Steamfitter.Api.Hubs;
using Steamfitter.Api.Tests.Support;

namespace Steamfitter.Api.Tests.Hubs;

/// <summary>
/// <c>EngineHub</c>'s own <c>[Authorize(AuthenticationSchemes = "Bearer")]</c>, over the in-process server at
/// <c>/hubs/engine</c>, where <c>Startup.Configure</c> maps it: a real SignalR connection over WebSockets, and
/// the negotiate request SignalR authorizes before any hub method runs. The attribute takes the default
/// policy <c>AuthorizationPolicyExtensions.AddAuthorizationPolicy</c> builds, an authenticated user whose token
/// carries every scope of <c>Authorization:AuthorizationScope</c> ("steamfitter player player-vm" as
/// shipped). The methods' own checks are <c>EngineHubTests</c>.
/// </summary>
/// <remarks>
/// The connection goes over WebSockets because its upgrade request carries the test's
/// <c>X-Test-Session</c> header, so the hub's <c>SteamfitterContext</c> is the test's database. Under long
/// polling an invocation runs outside any request and <c>TestDatabaseScope</c> could not pick one.
/// </remarks>
public class EngineHubConnectionTests(DatabaseFixture fixture, SteamfitterAppFactory factory) : ApiTestBase(fixture, factory)
{
    private const string HubPath = "/hubs/engine";

    private const string Negotiate = "/hubs/engine/negotiate?negotiateVersion=1";

    /// <summary>The method the probe sends to the scenario's group; nothing in the application sends it.</summary>
    private const string ProbeMethod = "ConnectionProbe";

    /// <summary>The shipped scopes without "steamfitter".</summary>
    private const string WithoutSteamfitter = "player player-vm";

    /// <summary>The shipped scopes without "player".</summary>
    private const string WithoutPlayer = "steamfitter player-vm";

    /// <summary>The shipped scopes without "player-vm".</summary>
    private const string WithoutPlayerVm = "steamfitter player";

    /// <summary>
    /// The member joins the scenario's group through the real connection, so a send to that group reaches it.
    /// The hub's context is the test's database, read through the connection's own request.
    /// </summary>
    [Fact]
    public async Task A_member_holding_ViewScenario_connects_over_WebSockets_and_joins_the_scenarios_group()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();
        await using var connection = Connection(actor);
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<string>(ProbeMethod, value => received.TrySetResult(value));
        await connection.StartAsync(Ct);

        await connection.InvokeAsync(nameof(EngineHub.JoinScenario), scenario.Id, Ct);

        await Factory.Services.GetRequiredService<HubLifetimeManager<EngineHub>>()
            .SendGroupAsync(scenario.Id.ToString(), ProbeMethod, [scenario.Name], Ct);
        Assert.Equal(scenario.Name, await received.Task.WaitAsync(TimeSpan.FromSeconds(10), Ct));
    }

    [Fact]
    public async Task A_signed_in_user_may_negotiate_a_connection()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        using var response = await Client(actor).PostAsync(Negotiate, null, Ct);

        await AssertStatus(HttpStatusCode.OK, response);
    }

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task A_connection_with_no_identity_is_unauthorized()
    {
        using var response = await Client().PostAsync(Negotiate, null, Ct);

        await AssertStatus(HttpStatusCode.Unauthorized, response);
    }

    [Fact]
    public async Task Negotiate_is_forbidden_for_a_token_without_the_steamfitter_scope_but_allowed_with_it()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        using var refused = await Client(actor).SendAsync(NegotiateWithScopes(WithoutSteamfitter), Ct);

        await AssertStatus(HttpStatusCode.Forbidden, refused);
        await AssertStatus(HttpStatusCode.OK, await Client(actor).PostAsync(Negotiate, null, Ct));
    }

    [Fact]
    public async Task Negotiate_is_forbidden_for_a_token_without_the_player_scope_but_allowed_with_it()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        using var refused = await Client(actor).SendAsync(NegotiateWithScopes(WithoutPlayer), Ct);

        await AssertStatus(HttpStatusCode.Forbidden, refused);
        await AssertStatus(HttpStatusCode.OK, await Client(actor).PostAsync(Negotiate, null, Ct));
    }

    [Fact]
    public async Task Negotiate_is_forbidden_for_a_token_without_the_player_vm_scope_but_allowed_with_it()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        using var refused = await Client(actor).SendAsync(NegotiateWithScopes(WithoutPlayerVm), Ct);

        await AssertStatus(HttpStatusCode.Forbidden, refused);
        await AssertStatus(HttpStatusCode.OK, await Client(actor).PostAsync(Negotiate, null, Ct));
    }

    /// <summary>A negotiate request whose token carries only <paramref name="scopes"/>.</summary>
    private static HttpRequestMessage NegotiateWithScopes(string scopes)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Negotiate);
        request.Headers.Add(TestAuthHandler.ScopeHeader, scopes);

        return request;
    }

    /// <summary>A WebSocket to the TestServer, carrying the headers every ApiTestBase client sends.</summary>
    private HubConnection Connection(TestActor actor)
    {
        var session = Client().DefaultRequestHeaders.GetValues(TestDatabaseScope.HeaderName).Single();

        return new HubConnectionBuilder()
            .WithUrl($"http://localhost{HubPath}", options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.SkipNegotiation = true;
                options.WebSocketFactory = async (context, ct) =>
                {
                    var client = Factory.Server.CreateWebSocketClient();
                    client.ConfigureRequest = request =>
                    {
                        request.Headers[TestAuthHandler.UserHeader] = actor.Id.ToString();
                        request.Headers[TestAuthHandler.NameHeader] = actor.Name;
                        request.Headers[TestDatabaseScope.HeaderName] = session;
                    };

                    return await client.ConnectAsync(context.Uri, ct);
                };
            })
            .Build();
    }
}
