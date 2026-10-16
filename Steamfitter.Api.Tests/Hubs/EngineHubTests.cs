// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Steamfitter.Api.Data;
using Steamfitter.Api.Hubs;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Tests.Support;

namespace Steamfitter.Api.Tests.Hubs;

/// <summary>
/// <c>EngineHub</c>: which groups a connection joins and leaves. The hub is built over an
/// <see cref="ApiTestHost"/> whose real <c>AuthorizationService</c> reads the principal the test built.
/// </summary>
public class EngineHubTests(DatabaseFixture fixture) : ServiceTestBase(fixture)
{
    [Fact]
    public async Task JoinScenario_adds_a_caller_holding_ViewScenario_on_it_to_the_scenarios_group()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var scenarioId = scenario.Id;
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder().WithScenario(scenarioId, ScenarioPermission.ViewScenario).Build());

        await hub.JoinScenario(scenarioId);

        await harness.Groups.Received(1).AddToGroupAsync(HubHarness.ConnectionId, scenarioId.ToString(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinScenario_of_a_scenario_that_is_not_there_joins_nothing()
    {
        var scenarioId = Guid.NewGuid();
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder().WithScenario(scenarioId, ScenarioPermission.ViewScenario).Build());

        await hub.JoinScenario(scenarioId);

        await harness.Groups.DidNotReceive().AddToGroupAsync(HubHarness.ConnectionId, scenarioId.ToString(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinScenario_adds_a_caller_holding_ViewScenarios_to_the_scenarios_group()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var scenarioId = scenario.Id;
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ViewScenarios).Build());

        await hub.JoinScenario(scenarioId);

        await harness.Groups.Received(1).AddToGroupAsync(HubHarness.ConnectionId, scenarioId.ToString(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinScenario_leaves_out_a_caller_holding_ViewScenario_only_on_another_scenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var scenarioId = scenario.Id;
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder().WithScenario(Guid.NewGuid(), ScenarioPermission.ViewScenario).Build());

        await hub.JoinScenario(scenarioId);

        await harness.Groups.DidNotReceive().AddToGroupAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinScenario_leaves_out_a_member_holding_only_ViewTasks()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var scenarioId = scenario.Id;
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder().WithScenario(scenarioId, ScenarioPermission.ViewTasks).Build());

        await hub.JoinScenario(scenarioId);

        await harness.Groups.DidNotReceive().AddToGroupAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinScenario_leaves_out_a_caller_holding_only_ViewScenarioTemplates()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var scenarioId = scenario.Id;
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).Build());

        await hub.JoinScenario(scenarioId);

        await harness.Groups.DidNotReceive().AddToGroupAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinSystem_adds_a_caller_holding_only_ViewScenarios_to_the_scenario_administrator_group()
    {
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ViewScenarios).Build());

        await hub.JoinSystem();

        await harness.Groups.Received(1).AddToGroupAsync(HubHarness.ConnectionId, EngineHub.SCENARIO_GROUP, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinSystem_adds_a_caller_holding_only_ViewScenarioTemplates_to_the_groups_of_the_scenarios_they_may_view_instead_of_the_scenario_administrator_group()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder()
            .WithSystemPermissions(SystemPermission.ViewScenarioTemplates)
            .WithScenario(scenario.Id, ScenarioPermission.ViewScenario)
            .Build());

        await hub.JoinSystem();

        await harness.Groups.Received(1).AddToGroupAsync(HubHarness.ConnectionId, scenario.Id.ToString(), Arg.Any<CancellationToken>());
        await harness.Groups.DidNotReceive().AddToGroupAsync(HubHarness.ConnectionId, EngineHub.SCENARIO_GROUP, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinSystem_adds_a_caller_holding_only_ViewScenarioTemplates_to_the_template_administrator_group()
    {
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).Build());

        await hub.JoinSystem();

        await harness.Groups.Received(1).AddToGroupAsync(HubHarness.ConnectionId, EngineHub.SCENARIO_TEMPLATE_GROUP, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinSystem_adds_a_caller_holding_only_ViewScenarios_to_the_groups_of_the_templates_they_may_view_instead_of_the_template_administrator_group()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder()
            .WithSystemPermissions(SystemPermission.ViewScenarios)
            .WithScenarioTemplate(template.Id, ScenarioTemplatePermission.ViewScenarioTemplate)
            .Build());

        await hub.JoinSystem();

        await harness.Groups.Received(1).AddToGroupAsync(HubHarness.ConnectionId, template.Id.ToString(), Arg.Any<CancellationToken>());
        await harness.Groups.DidNotReceive().AddToGroupAsync(HubHarness.ConnectionId, EngineHub.SCENARIO_TEMPLATE_GROUP, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinSystem_adds_a_caller_holding_only_ViewGroups_to_the_group_administrator_group()
    {
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ViewGroups).Build());

        await hub.JoinSystem();

        await harness.Groups.Received(1).AddToGroupAsync(HubHarness.ConnectionId, EngineHub.GROUP_GROUP, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinSystem_does_not_add_a_caller_holding_only_ViewRoles_to_the_group_administrator_group()
    {
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ViewRoles).Build());

        await hub.JoinSystem();

        await harness.Groups.DidNotReceive().AddToGroupAsync(HubHarness.ConnectionId, EngineHub.GROUP_GROUP, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinSystem_adds_a_caller_holding_only_ViewRoles_to_the_role_administrator_group()
    {
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ViewRoles).Build());

        await hub.JoinSystem();

        await harness.Groups.Received(1).AddToGroupAsync(HubHarness.ConnectionId, EngineHub.ROLE_GROUP, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinSystem_does_not_add_a_caller_holding_only_ViewUsers_to_the_role_administrator_group()
    {
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ViewUsers).Build());

        await hub.JoinSystem();

        await harness.Groups.DidNotReceive().AddToGroupAsync(HubHarness.ConnectionId, EngineHub.ROLE_GROUP, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinSystem_adds_a_caller_holding_only_ViewUsers_to_the_user_administrator_group()
    {
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ViewUsers).Build());

        await hub.JoinSystem();

        await harness.Groups.Received(1).AddToGroupAsync(HubHarness.ConnectionId, EngineHub.USER_GROUP, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinSystem_does_not_add_a_caller_holding_only_ViewGroups_to_the_user_administrator_group()
    {
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ViewGroups).Build());

        await hub.JoinSystem();

        await harness.Groups.DidNotReceive().AddToGroupAsync(HubHarness.ConnectionId, EngineHub.USER_GROUP, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinSystem_adds_a_caller_holding_every_view_permission_to_the_administrator_groups()
    {
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder()
            .WithSystemPermissions(SystemPermission.ViewScenarios, SystemPermission.ViewScenarioTemplates, SystemPermission.ViewGroups, SystemPermission.ViewRoles, SystemPermission.ViewUsers)
            .Build());

        await hub.JoinSystem();

        Assert.Equal([EngineHub.SCENARIO_GROUP, EngineHub.SCENARIO_TEMPLATE_GROUP, EngineHub.GROUP_GROUP, EngineHub.ROLE_GROUP, EngineHub.USER_GROUP], harness.JoinedGroups);
    }

    [Fact]
    public async Task JoinSystem_adds_a_caller_without_ViewScenarios_to_the_groups_of_the_scenarios_and_templates_they_may_view()
    {
        var scenario = TestData.Scenario();
        var template = TestData.ScenarioTemplate();
        await Seed(scenario, template);
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder()
            .WithSystemPermissions(SystemPermission.CreateScenarios)
            .WithScenario(scenario.Id, ScenarioPermission.ViewScenario)
            .WithScenarioTemplate(template.Id, ScenarioTemplatePermission.ViewScenarioTemplate)
            .Build());

        await hub.JoinSystem();

        await harness.Groups.Received(1).AddToGroupAsync(HubHarness.ConnectionId, scenario.Id.ToString(), Arg.Any<CancellationToken>());
        await harness.Groups.Received(1).AddToGroupAsync(HubHarness.ConnectionId, template.Id.ToString(), Arg.Any<CancellationToken>());
        await harness.Groups.DidNotReceive().AddToGroupAsync(HubHarness.ConnectionId, EngineHub.SCENARIO_GROUP, Arg.Any<CancellationToken>());
    }

    /// <summary>A membership whose role grants no view permission joins nothing, as JoinScenario decides.</summary>
    [Fact]
    public async Task JoinSystem_leaves_out_a_scenario_and_template_the_caller_belongs_to_without_a_view_permission()
    {
        var scenario = TestData.Scenario();
        var template = TestData.ScenarioTemplate();
        await Seed(scenario, template);
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder()
            .WithScenario(scenario.Id, ScenarioPermission.ExecuteScenario)
            .WithScenarioTemplate(template.Id, ScenarioTemplatePermission.EditScenarioTemplate)
            .WithScenario(Guid.NewGuid())
            .Build());

        await hub.JoinSystem();

        await harness.Groups.DidNotReceive().AddToGroupAsync(HubHarness.ConnectionId, scenario.Id.ToString(), Arg.Any<CancellationToken>());
        await harness.Groups.DidNotReceive().AddToGroupAsync(HubHarness.ConnectionId, template.Id.ToString(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinSystem_adds_the_group_of_a_scenario_the_caller_may_view_through_a_group()
    {
        var user = new ClaimsPrincipalBuilder();
        var group = TestData.Group();
        var scenario = TestData.Scenario();
        await Seed(TestData.User(user.UserId), group, scenario,
            TestData.GroupMembership(group.Id, user.UserId),
            TestData.ScenarioMembership(scenario.Id, groupId: group.Id, roleId: TestData.ScenarioRoles.Observer));
        var (hub, harness) = Connect(user.WithScenario(scenario.Id, ScenarioPermission.ViewScenario).Build());

        await hub.JoinSystem();

        await harness.Groups.Received(1).AddToGroupAsync(HubHarness.ConnectionId, scenario.Id.ToString(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LeaveScenario_removes_the_connection_from_the_scenarios_group()
    {
        var scenarioId = Guid.NewGuid();
        var (hub, harness) = Connect(ClaimsPrincipalBuilder.Anonymous());

        await hub.LeaveScenario(scenarioId);

        await harness.Groups.Received(1).RemoveFromGroupAsync(HubHarness.ConnectionId, scenarioId.ToString(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LeaveSystem_removes_a_caller_holding_ViewScenarios_from_the_administrator_groups()
    {
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ViewScenarios).Build());

        await hub.LeaveSystem();

        await harness.Groups.Received(1).RemoveFromGroupAsync(HubHarness.ConnectionId, EngineHub.SCENARIO_GROUP, Arg.Any<CancellationToken>());
        await harness.Groups.Received(1).RemoveFromGroupAsync(HubHarness.ConnectionId, EngineHub.USER_GROUP, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LeaveSystem_removes_a_caller_holding_only_ViewScenarioTemplates_from_the_template_administrator_group()
    {
        var (hub, harness) = Connect(new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).Build());

        await hub.LeaveSystem();

        await harness.Groups.Received(1).RemoveFromGroupAsync(HubHarness.ConnectionId, EngineHub.SCENARIO_TEMPLATE_GROUP, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LeaveSystem_removes_a_caller_holding_only_ViewScenarioTemplates_from_their_scenarios_group_instead_of_the_scenario_administrator_group()
    {
        var user = new ClaimsPrincipalBuilder();
        var scenario = TestData.Scenario();
        await Seed(TestData.User(user.UserId), scenario,
            TestData.ScenarioMembership(scenario.Id, userId: user.UserId, roleId: TestData.ScenarioRoles.Observer));
        var (hub, harness) = Connect(user.WithSystemPermissions(SystemPermission.ViewScenarioTemplates).Build());

        await hub.LeaveSystem();

        await harness.Groups.Received(1).RemoveFromGroupAsync(HubHarness.ConnectionId, scenario.Id.ToString(), Arg.Any<CancellationToken>());
        await harness.Groups.DidNotReceive().RemoveFromGroupAsync(HubHarness.ConnectionId, EngineHub.SCENARIO_GROUP, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LeaveSystem_removes_a_caller_holding_only_ViewScenarios_from_their_templates_group_instead_of_the_template_administrator_group()
    {
        var user = new ClaimsPrincipalBuilder();
        var template = TestData.ScenarioTemplate();
        await Seed(TestData.User(user.UserId), template,
            TestData.ScenarioTemplateMembership(template.Id, userId: user.UserId, roleId: TestData.ScenarioTemplateRoles.Observer));
        var (hub, harness) = Connect(user.WithSystemPermissions(SystemPermission.ViewScenarios).Build());

        await hub.LeaveSystem();

        await harness.Groups.Received(1).RemoveFromGroupAsync(HubHarness.ConnectionId, template.Id.ToString(), Arg.Any<CancellationToken>());
        await harness.Groups.DidNotReceive().RemoveFromGroupAsync(HubHarness.ConnectionId, EngineHub.SCENARIO_TEMPLATE_GROUP, Arg.Any<CancellationToken>());
    }

    private (EngineHub Hub, HubHarness Harness) Connect(ClaimsPrincipal user)
    {
        var host = HostFor(user);
        var harness = new HubHarness(user: user);
        var hub = harness.Attach(new EngineHub(host.Resolve<SteamfitterContext>(), host.Resolve<ISteamfitterAuthorizationService>(), user));

        return (hub, harness);
    }
}
