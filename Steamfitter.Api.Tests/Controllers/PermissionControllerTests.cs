// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Steamfitter.Api.Data;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Tests.Support;

namespace Steamfitter.Api.Tests.Controllers;

/// <summary>
/// <c>SystemPermissionsController</c>, <c>ScenarioPermissionsController</c> and
/// <c>ScenarioTemplatePermissionsController</c>: the caller's own permissions, which any signed-in caller
/// may read.
/// </summary>
public class PermissionControllerTests(DatabaseFixture fixture, SteamfitterAppFactory factory)
    : ApiTestBase(fixture, factory)
{
    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task An_unauthenticated_request_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync("api/me/systemPermissions", Ct));
    }

    [Fact]
    public async Task GetMySystemPermissions_returns_the_permissions_of_the_callers_role()
    {
        var actor = await Actor().WithRole(TestData.Roles.ContentDeveloper).SeedAsync();

        var permissions = await ReadAsync<List<SystemPermission>>(await Client(actor).GetAsync("api/me/systemPermissions", Ct));

        Assert.Equal([SystemPermission.CreateScenarioTemplates, SystemPermission.CreateScenarios, SystemPermission.ExecuteScenarios], permissions.Order());
    }

    [Fact]
    public async Task GetMyScenarioPermissions_returns_the_callers_permissions_on_the_scenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ExecuteScenario]).SeedAsync();

        var claims = await ReadAsync<List<ScenarioPermissionClaim>>(await Client(actor).GetAsync($"api/scenarios/{scenario.Id}/me/permissions", Ct));

        var claim = Assert.Single(claims);
        Assert.Equal(scenario.Id, claim.ScenarioId);
        Assert.Equal([ScenarioPermission.ExecuteScenario], claim.Permissions);
    }

    /// <summary>The permissions on every scenario the caller belongs to are returned, whichever scenario the route names.</summary>
    [Fact]
    public async Task GetMyScenarioPermissions_returns_the_callers_permissions_on_every_scenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor()
            .OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario])
            .OnNewScenario(ScenarioPermission.ManageScenario)
            .SeedAsync();

        var claims = await ReadAsync<List<ScenarioPermissionClaim>>(await Client(actor).GetAsync($"api/scenarios/{scenario.Id}/me/permissions", Ct));

        Assert.Equal(2, claims.Count);
        Assert.Contains(actor.ScenarioMemberships[1].ResourceId, claims.Select(x => x.ScenarioId));
    }

    /// <summary>The permissions on every template the caller belongs to are returned, whichever template the route names.</summary>
    [Fact]
    public async Task GetMyScenarioTemplatePermissions_returns_the_callers_permissions_on_every_template()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor()
            .OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate])
            .OnNewScenarioTemplate(ScenarioTemplatePermission.ManageScenarioTemplate)
            .SeedAsync();

        var claims = await ReadAsync<List<ScenarioTemplatePermissionClaim>>(await Client(actor).GetAsync($"api/scenarioTemplates/{template.Id}/me/permissions", Ct));

        Assert.Equal(2, claims.Count);
        Assert.Contains(actor.ScenarioTemplateMemberships[1].ResourceId, claims.Select(x => x.ScenarioTemplateId));
    }
}
