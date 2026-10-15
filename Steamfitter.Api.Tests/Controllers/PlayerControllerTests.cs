// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Steamfitter.Api.Data;
using Steamfitter.Api.Tests.Support;

namespace Steamfitter.Api.Tests.Controllers;

/// <summary>
/// <c>PlayerController</c> proxies Player (<c>api/views</c>) and the Player VM API (<c>api/vms</c>) with the
/// caller's own token, through the request-scoped generated clients over <c>Factory.OutboundHttp</c>.
/// </summary>
public class PlayerControllerTests(DatabaseFixture fixture, SteamfitterAppFactory factory)
    : ApiTestBase(fixture, factory)
{
    /// <summary>The Player VM API's base url, <c>ClientSettings:urls:vmApi</c> in appsettings.json.</summary>
    private const string VmApi = "http://localhost:4302/";

    /// <summary>Player's own views of the caller, from <c>ClientSettings:urls:playerApi</c>.</summary>
    private const string MyViewsUrl = "http://localhost:4300/api/me/views";

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task An_unauthenticated_request_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync("api/views", Ct));
    }

    /// <summary>
    /// Player's <c>api/me/views</c> carries no key of the test's own, so this is the one test in the run
    /// that arranges it.
    /// </summary>
    [Fact]
    public async Task GetViews_answers_with_the_views_player_returns_for_the_caller()
    {
        var viewId = Guid.NewGuid();
        Factory.OutboundHttp.Respond(MyViewsUrl, Encoding.UTF8.GetBytes($$"""[{"id":"{{viewId}}","name":"Exercise"}]"""), "application/json");
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        var views = await ReadAsync<List<VmOnly>>(await Client(actor).GetAsync("api/views", Ct));

        Assert.Contains((viewId, "Exercise"), views.Select(x => (x.Id, x.Name)));
    }

    [Fact]
    public async Task GetViewVms_answers_with_the_vms_the_vm_api_returns_for_the_view()
    {
        var viewId = Guid.NewGuid();
        var vmId = Guid.NewGuid();
        Factory.OutboundHttp.Respond(VmsUrl(viewId), Encoding.UTF8.GetBytes($$"""[{"id":"{{vmId}}","name":"vm-1"}]"""), "application/json");
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        var vms = await ReadAsync<List<VmOnly>>(await Client(actor).GetAsync($"api/vms?viewId={viewId}", Ct));

        Assert.Equal([(vmId, "vm-1")], vms.Select(x => (x.Id, x.Name)));
    }

    [Fact]
    public async Task GetViewVms_forwards_the_callers_bearer_token_to_the_vm_api()
    {
        var viewId = Guid.NewGuid();
        Factory.OutboundHttp.Respond(VmsUrl(viewId), Encoding.UTF8.GetBytes("[]"), "application/json");

        await AssertStatus(HttpStatusCode.OK, await RootClient.GetAsync($"api/vms?viewId={viewId}", Ct));

        var sent = Assert.Single(Factory.OutboundHttp.Sent, x => x.Uri == VmsUrl(viewId));
        Assert.Equal($"Bearer {SteamfitterAppFactory.BearerToken}", sent.Headers["Authorization"]);
    }

    /// <summary>A view the VM API does not know (404 with no body) is answered with a 500.</summary>
    [Fact]
    public async Task GetViewVms_answers_a_vm_api_404_with_a_server_error()
    {
        var error = await AssertJsonError(HttpStatusCode.InternalServerError, await RootClient.GetAsync($"api/vms?viewId={Guid.NewGuid()}", Ct));

        Assert.Contains("Status: 404", error.Detail);
    }

    private static string VmsUrl(Guid viewId) => $"{VmApi}api/views/{viewId}/vms?includePersonal=true&onlyMine=false";

    private sealed record VmOnly(Guid Id, string Name);
}
