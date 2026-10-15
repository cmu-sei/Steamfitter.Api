// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Steamfitter.Api.Data;
using Steamfitter.Api.Tests.Support;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Tests.Infrastructure.Authorization;

/// <summary>
/// Steamfitter's <c>AuthorizationService</c> over the real handlers: the system path, and the lookups that
/// turn a task, result, view or membership id into the scenario or template whose permission decides.
/// </summary>
public class AuthorizationServiceTests(DatabaseFixture fixture) : DatabaseTestBase(fixture)
{
    [Fact]
    public async Task A_system_permission_grants_without_a_resource()
    {
        var service = Service(new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ViewScenarios));

        Assert.True(await service.AuthorizeAsync([SystemPermission.ViewScenarios], Ct));
    }

    [Fact]
    public async Task Any_one_of_the_listed_system_permissions_grants()
    {
        var service = Service(new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ViewUsers));

        Assert.True(await service.AuthorizeAsync([SystemPermission.ManageUsers, SystemPermission.ViewUsers], Ct));
    }

    [Fact]
    public async Task A_different_system_permission_does_not_grant()
    {
        var service = Service(new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ViewUsers));

        Assert.False(await service.AuthorizeAsync([SystemPermission.ManageUsers], Ct));
    }

    [Fact]
    public async Task A_scenario_permission_on_a_tasks_scenario_grants_for_the_task()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);
        var service = Service(new ClaimsPrincipalBuilder().WithScenario(scenario.Id, ScenarioPermission.EditScenario));

        Assert.True(await service.AuthorizeAsync<SAVM.Task>(task.Id, [SystemPermission.EditScenarios], [ScenarioPermission.EditScenario], Ct));
    }

    [Fact]
    public async Task A_scenario_permission_on_a_results_scenario_grants_for_the_result()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id);
        var result = TestData.Result(task.Id);
        await Seed(scenario, task, result);
        var service = Service(new ClaimsPrincipalBuilder().WithScenario(scenario.Id, ScenarioPermission.ViewScenario));

        Assert.True(await service.AuthorizeAsync<SAVM.Result>(result.Id, [SystemPermission.ViewScenarios], [ScenarioPermission.ViewScenario], Ct));
    }

    [Fact]
    public async Task A_scenario_permission_grants_for_the_view_the_scenario_belongs_to()
    {
        var viewId = Guid.NewGuid();
        var scenario = TestData.Scenario(viewId: viewId);
        await Seed(scenario);
        var service = Service(new ClaimsPrincipalBuilder().WithScenario(scenario.Id, ScenarioPermission.ViewTasks));

        Assert.True(await service.AuthorizeAsync<SAVM.PlayerView>(viewId, [SystemPermission.ViewScenarios], [ScenarioPermission.ViewTasks], Ct));
    }

    [Fact]
    public async Task A_scenario_permission_on_a_memberships_scenario_grants_for_the_membership()
    {
        var scenario = TestData.Scenario();
        var user = TestData.User();
        var membership = TestData.ScenarioMembership(scenario.Id, userId: user.Id, roleId: TestData.ScenarioRoles.Observer);
        await Seed(scenario, user, membership);
        var service = Service(new ClaimsPrincipalBuilder().WithScenario(scenario.Id, ScenarioPermission.ManageScenario));

        Assert.True(await service.AuthorizeAsync<SAVM.ScenarioMembership>(membership.Id, [SystemPermission.ManageScenarios], [ScenarioPermission.ManageScenario], Ct));
    }

    [Fact]
    public async Task A_template_permission_grants_for_the_template()
    {
        var templateId = Guid.NewGuid();
        var service = Service(new ClaimsPrincipalBuilder().WithScenarioTemplate(templateId, ScenarioTemplatePermission.ViewScenarioTemplate));

        Assert.True(await service.AuthorizeAsync<SAVM.ScenarioTemplate>(templateId, [SystemPermission.ViewScenarioTemplates], [ScenarioTemplatePermission.ViewScenarioTemplate], Ct));
    }

    /// <summary>The template lookup for a scenario id throws.</summary>
    [Fact]
    public async Task The_template_lookup_for_a_scenario_throws()
    {
        var template = TestData.ScenarioTemplate();
        var scenario = TestData.Scenario(scenarioTemplateId: template.Id);
        await Seed(template, scenario);
        var service = Service(new ClaimsPrincipalBuilder().WithScenarioTemplate(template.Id, ScenarioTemplatePermission.ViewScenarioTemplate));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AuthorizeAsync<SAVM.Scenario>(scenario.Id, [SystemPermission.ViewScenarioTemplates], [ScenarioTemplatePermission.ViewScenarioTemplate], Ct));

        Assert.Equal("Nullable object must have a value.", error.Message);
    }

    [Fact]
    public async Task A_scenario_permission_on_another_scenario_does_not_grant()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var service = Service(new ClaimsPrincipalBuilder().WithScenario(Guid.NewGuid(), ScenarioPermission.ManageScenario));

        Assert.False(await service.AuthorizeAsync<SAVM.Scenario>(scenario.Id, [SystemPermission.ManageScenarios], [ScenarioPermission.ManageScenario], Ct));
    }

    [Fact]
    public void GetAuthorizedScenarioIds_lists_every_scenario_the_caller_holds_a_claim_on()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var service = Service(new ClaimsPrincipalBuilder().WithScenario(first, ScenarioPermission.ViewScenario).WithScenario(second));

        Assert.Equal(new[] { first, second }.Order(), service.GetAuthorizedScenarioIds().Order());
    }

    [Fact]
    public void GetSystemPermissions_skips_values_that_are_not_permissions()
    {
        var service = Service(new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ViewRoles).WithRawSystemPermission("NotAPermission"));

        Assert.Equal([SystemPermission.ViewRoles], service.GetSystemPermissions());
    }

    private Steamfitter.Api.Infrastructure.Authorization.ISteamfitterAuthorizationService Service(ClaimsPrincipalBuilder user) =>
        AuthorizationHarness.CreateSteamfitterAuthorizationService(user.Build(), Db);
}
