// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Steamfitter.Api.Data;
using Steamfitter.Api.Tests.Support;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Tests.Controllers;

/// <summary><c>ScenarioRolesController</c> and <c>ScenarioTemplateRolesController</c>: the seeded roles, behind ViewRoles.</summary>
public class ScenarioRoleControllerTests(DatabaseFixture fixture, SteamfitterAppFactory factory)
    : ApiTestBase(fixture, factory)
{
    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task An_unauthenticated_request_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync("api/scenario-roles", Ct));
    }

    [Fact]
    public async Task GetAll_scenario_roles_returns_the_seeded_roles_to_a_caller_holding_ViewRoles()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewRoles).SeedAsync();

        var roles = await ReadAsync<List<SAVM.ScenarioRole>>(await Client(actor).GetAsync("api/scenario-roles", Ct));

        Assert.Equal(["Facilitator", "Manager", "Member", "Observer"], roles.Select(x => x.Name).Order());
    }

    [Fact]
    public async Task GetAll_scenario_roles_is_forbidden_for_a_caller_holding_only_ViewUsers()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewUsers).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync("api/scenario-roles", Ct));
    }

    [Fact]
    public async Task Get_scenario_role_returns_the_role_to_a_caller_holding_ViewRoles()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewRoles).SeedAsync();

        var role = await ReadAsync<SAVM.ScenarioRole>(await Client(actor).GetAsync($"api/scenario-roles/{TestData.ScenarioRoles.Facilitator}", Ct));

        Assert.Equal([ScenarioPermission.ViewScenario, ScenarioPermission.ExecuteScenario], role.Permissions);
    }

    [Fact]
    public async Task Get_scenario_role_is_forbidden_for_a_caller_holding_only_ManageScenarios()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/scenario-roles/{TestData.ScenarioRoles.Member}", Ct));
    }

    [Fact]
    public async Task Get_of_an_unknown_scenario_role_is_not_found()
    {
        await AssertJsonError(HttpStatusCode.NotFound, await RootClient.GetAsync($"api/scenario-roles/{Guid.NewGuid()}", Ct));
    }

    [Fact]
    public async Task GetAll_template_roles_returns_the_seeded_roles_to_a_caller_holding_ViewRoles()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewRoles).SeedAsync();

        var roles = await ReadAsync<List<SAVM.ScenarioTemplateRole>>(await Client(actor).GetAsync("api/scenarioTemplate-roles", Ct));

        Assert.Equal(["Manager", "Member", "Observer"], roles.Select(x => x.Name).Order());
    }

    [Fact]
    public async Task GetAll_template_roles_is_forbidden_for_a_caller_holding_only_ViewGroups()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewGroups).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync("api/scenarioTemplate-roles", Ct));
    }

    [Fact]
    public async Task Get_template_role_returns_the_role_to_a_caller_holding_ViewRoles()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewRoles).SeedAsync();

        var role = await ReadAsync<SAVM.ScenarioTemplateRole>(await Client(actor).GetAsync($"api/scenarioTemplate-roles/{TestData.ScenarioTemplateRoles.Manager}", Ct));

        Assert.True(role.AllPermissions);
    }

    [Fact]
    public async Task Get_template_role_is_forbidden_for_a_caller_holding_only_ManageScenarioTemplates()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/scenarioTemplate-roles/{TestData.ScenarioTemplateRoles.Manager}", Ct));
    }
}
