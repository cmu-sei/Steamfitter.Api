// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data;
using Steamfitter.Api.Hubs;
using Steamfitter.Api.Tests.Support;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Tests.Controllers;

/// <summary><c>ScenarioMembershipsController</c>: who belongs to a scenario, with which scenario role.</summary>
public class ScenarioMembershipControllerTests(DatabaseFixture fixture, SteamfitterAppFactory factory)
    : ApiTestBase(fixture, factory)
{
    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task An_unauthenticated_request_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync($"api/scenarios/{Guid.NewGuid()}/memberships", Ct));
    }

    [Fact]
    public async Task Get_returns_the_membership_to_a_member_holding_ViewScenario()
    {
        var (scenario, membership) = await SeedScenarioWithMember();
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        var read = await ReadAsync<SAVM.ScenarioMembership>(await Client(actor).GetAsync($"api/scenarios/memberships/{membership.Id}", Ct));

        Assert.Equal((membership.UserId, TestData.ScenarioRoles.Observer), (read.UserId, read.RoleId));
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_ViewScenario_only_on_another_scenario()
    {
        var (_, membership) = await SeedScenarioWithMember();
        var actor = await Actor().OnNewScenario(ScenarioPermission.ViewScenario).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/scenarios/memberships/{membership.Id}", Ct));
    }

    [Fact]
    public async Task Get_of_an_unknown_membership_is_not_found()
    {
        await AssertJsonError(HttpStatusCode.NotFound, await RootClient.GetAsync($"api/scenarios/memberships/{Guid.NewGuid()}", Ct));
    }

    [Fact]
    public async Task GetAll_returns_the_scenarios_memberships_to_a_member_holding_ViewScenario()
    {
        var (scenario, membership) = await SeedScenarioWithMember();
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        var memberships = await ReadAsync<List<SAVM.ScenarioMembership>>(await Client(actor).GetAsync($"api/scenarios/{scenario.Id}/memberships", Ct));

        Assert.Equal([membership.Id, actor.ScenarioMemberships[0].Id], memberships.Select(x => x.Id).OrderBy(x => x == membership.Id ? 0 : 1));
    }

    [Fact]
    public async Task GetAll_is_forbidden_for_a_member_holding_only_ViewTasks()
    {
        var (scenario, _) = await SeedScenarioWithMember();
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewTasks]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/scenarios/{scenario.Id}/memberships", Ct));
    }

    [Fact]
    public async Task Create_adds_a_user_to_the_scenario_for_a_member_holding_ManageScenario()
    {
        var scenario = TestData.Scenario();
        var user = TestData.User(name: "Joiner");
        await Seed(scenario, user);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ManageScenario]).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync(
            $"api/scenarios/{scenario.Id}/memberships",
            new { scenarioId = scenario.Id, userId = user.Id, roleId = TestData.ScenarioRoles.Facilitator },
            Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        await using var db = NewContext();
        Assert.Equal(TestData.ScenarioRoles.Facilitator, (await db.ScenarioMemberships.SingleAsync(x => x.ScenarioId == scenario.Id && x.UserId == user.Id, Ct)).RoleId);
    }

    [Fact]
    public async Task Create_broadcasts_the_membership_to_the_scenario_administrators()
    {
        var scenario = TestData.Scenario();
        var user = TestData.User(name: "Joiner");
        await Seed(scenario, user);

        var created = await ReadAsync<SAVM.ScenarioMembership>(await RootClient.PostAsJsonAsync(
            $"api/scenarios/{scenario.Id}/memberships",
            new { scenarioId = scenario.Id, userId = user.Id, roleId = TestData.ScenarioRoles.Member },
            Ct));

        Assert.Contains(Factory.Hub<EngineHub>().ToGroup(EngineHub.SCENARIO_GROUP),
            x => x.Method == EngineMethods.ScenarioMembershipCreated && ((SAVM.ScenarioMembership)x.Argument).Id == created.Id);
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_member_holding_only_EditScenario()
    {
        var scenario = TestData.Scenario();
        var user = TestData.User(name: "Joiner");
        await Seed(scenario, user);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync($"api/scenarios/{scenario.Id}/memberships", new { scenarioId = scenario.Id, userId = user.Id, roleId = TestData.ScenarioRoles.Member }, Ct);

        await AssertStatus(HttpStatusCode.Forbidden, response);
        await using var db = NewContext();
        Assert.False(await db.ScenarioMemberships.AnyAsync(x => x.UserId == user.Id, Ct));
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_ManageScenario_only_on_another_scenario()
    {
        var scenario = TestData.Scenario();
        var user = TestData.User(name: "Joiner");
        await Seed(scenario, user);
        var actor = await Actor().OnNewScenario(ScenarioPermission.ManageScenario).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync(
            $"api/scenarios/{scenario.Id}/memberships",
            new { scenarioId = scenario.Id, userId = user.Id, roleId = TestData.ScenarioRoles.Member },
            Ct));
    }

    /// <summary>A membership for a group is answered with a 500.</summary>
    [Fact]
    public async Task Create_answers_a_group_membership_with_a_server_error()
    {
        var scenario = TestData.Scenario();
        var group = TestData.Group();
        await Seed(scenario, group);

        var response = await RootClient.PostAsJsonAsync($"api/scenarios/{scenario.Id}/memberships", new { scenarioId = scenario.Id, groupId = group.Id, roleId = TestData.ScenarioRoles.Member }, Ct);

        var error = await AssertJsonError(HttpStatusCode.InternalServerError, response);
        Assert.Equal("The requested user is not a Steamfitter user.", error.Detail);
        await using var db = NewContext();
        Assert.False(await db.ScenarioMemberships.AnyAsync(x => x.GroupId == group.Id, Ct));
    }

    [Fact]
    public async Task Update_changes_the_role_for_a_caller_holding_ManageScenarios()
    {
        var (scenario, membership) = await SeedScenarioWithMember();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync(
            $"api/scenarios/memberships/{membership.Id}",
            new { id = membership.Id, scenarioId = scenario.Id, userId = membership.UserId, roleId = TestData.ScenarioRoles.Manager },
            Ct));

        await using var db = NewContext();
        Assert.Equal(TestData.ScenarioRoles.Manager, (await db.ScenarioMemberships.SingleAsync(x => x.Id == membership.Id, Ct)).RoleId);
        Assert.Contains(Factory.Hub<EngineHub>().ToGroup(EngineHub.SCENARIO_GROUP),
            x => x.Method == EngineMethods.ScenarioMembershipUpdated && ((SAVM.ScenarioMembership)x.Argument).Id == membership.Id);
    }

    /// <summary>A member holding ManageScenario on the membership's scenario is refused.</summary>
    [Fact]
    public async Task Update_is_forbidden_for_a_member_holding_ManageScenario_on_the_memberships_scenario()
    {
        var (scenario, membership) = await SeedScenarioWithMember();
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ManageScenario]).SeedAsync();

        var response = await Client(actor).PutAsJsonAsync(
            $"api/scenarios/memberships/{membership.Id}",
            new { id = membership.Id, scenarioId = scenario.Id, userId = membership.UserId, roleId = TestData.ScenarioRoles.Manager },
            Ct);

        await AssertStatus(HttpStatusCode.Forbidden, response);
        await using var db = NewContext();
        Assert.Equal(TestData.ScenarioRoles.Observer, (await db.ScenarioMemberships.SingleAsync(x => x.Id == membership.Id, Ct)).RoleId);
    }

    // Same case as Update_is_forbidden_for_a_member_holding_ManageScenario_on_the_memberships_scenario.
    [Fact]
    public async Task Update_makes_an_observer_the_manager_of_a_scenario_when_the_body_names_a_membership_they_manage_as_the_scenario()
    {
        var managed = TestData.Scenario("Managed");
        var observed = TestData.Scenario("Observed");
        await Seed(managed, observed);
        var actor = await Actor()
            .OnScenario(managed.Id, permissions: [ScenarioPermission.ManageScenario])
            .OnScenario(observed.Id, TestData.ScenarioRoles.Observer)
            .SeedAsync();
        var managedMembership = actor.ScenarioMemberships[0].Id;
        var observedMembership = actor.ScenarioMemberships[1].Id;

        var response = await Client(actor).PutAsJsonAsync(
            $"api/scenarios/memberships/{observedMembership}",
            new { id = observedMembership, scenarioId = managedMembership, userId = actor.Id, roleId = TestData.ScenarioRoles.Manager },
            Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        await using var db = NewContext();
        var stored = await db.ScenarioMemberships.SingleAsync(x => x.Id == observedMembership, Ct);
        Assert.Equal((observed.Id, TestData.ScenarioRoles.Manager), (stored.ScenarioId, stored.RoleId));
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_only_EditScenarios()
    {
        var (scenario, membership) = await SeedScenarioWithMember();
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync(
            $"api/scenarios/memberships/{membership.Id}",
            new { id = membership.Id, scenarioId = scenario.Id, userId = membership.UserId, roleId = TestData.ScenarioRoles.Manager },
            Ct));
    }

    [Fact]
    public async Task Delete_removes_the_membership_for_a_member_holding_ManageScenario()
    {
        var (scenario, membership) = await SeedScenarioWithMember();
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ManageScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/scenarios/memberships/{membership.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.ScenarioMemberships.AnyAsync(x => x.Id == membership.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_member_holding_only_EditScenario()
    {
        var (scenario, membership) = await SeedScenarioWithMember();
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/scenarios/memberships/{membership.Id}", Ct));
        await using var db = NewContext();
        Assert.True(await db.ScenarioMemberships.AnyAsync(x => x.Id == membership.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_ManageScenario_only_on_another_scenario()
    {
        var (_, membership) = await SeedScenarioWithMember();
        var actor = await Actor().OnNewScenario(ScenarioPermission.ManageScenario).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/scenarios/memberships/{membership.Id}", Ct));
    }

    private async Task<(Data.Models.ScenarioEntity Scenario, Data.Models.ScenarioMembershipEntity Membership)> SeedScenarioWithMember()
    {
        var scenario = TestData.Scenario();
        var user = TestData.User(name: "Member");
        var membership = TestData.ScenarioMembership(scenario.Id, userId: user.Id, roleId: TestData.ScenarioRoles.Observer);
        await Seed(scenario, user, membership);

        return (scenario, membership);
    }
}
