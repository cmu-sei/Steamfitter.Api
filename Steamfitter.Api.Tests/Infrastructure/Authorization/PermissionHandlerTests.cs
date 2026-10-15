// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Threading.Tasks;
using Steamfitter.Api.Data;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Tests.Support;

namespace Steamfitter.Api.Tests.Infrastructure.Authorization;

/// <summary>The three requirement handlers production registers, run directly.</summary>
public class PermissionHandlerTests
{
    [Fact]
    public async Task SystemPermissionHandler_succeeds_for_a_held_permission()
    {
        var context = await AuthorizationHarness.HandleAsync(
            new SystemPermissionHandler(),
            new SystemPermissionRequirement([SystemPermission.ManageTasks]),
            new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ManageTasks).Build());

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task SystemPermissionHandler_does_not_succeed_for_another_permission()
    {
        var context = await AuthorizationHarness.HandleAsync(
            new SystemPermissionHandler(),
            new SystemPermissionRequirement([SystemPermission.ManageTasks]),
            new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.ExecuteScenarios).Build());

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task SystemPermissionHandler_succeeds_for_anyone_when_no_permission_is_required()
    {
        var context = await AuthorizationHarness.HandleAsync(
            new SystemPermissionHandler(),
            new SystemPermissionRequirement([]),
            ClaimsPrincipalBuilder.Anonymous());

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task ScenarioPermissionHandler_succeeds_for_a_permission_held_on_the_scenario()
    {
        var scenarioId = Guid.NewGuid();

        var context = await AuthorizationHarness.HandleAsync(
            new ScenarioPermissionHandler(),
            new ScenarioPermissionRequirement([ScenarioPermission.ExecuteScenario], scenarioId),
            new ClaimsPrincipalBuilder().WithScenario(scenarioId, ScenarioPermission.ExecuteScenario).Build());

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task ScenarioPermissionHandler_fails_for_a_permission_held_only_on_another_scenario()
    {
        var context = await AuthorizationHarness.HandleAsync(
            new ScenarioPermissionHandler(),
            new ScenarioPermissionRequirement([ScenarioPermission.ExecuteScenario], Guid.NewGuid()),
            new ClaimsPrincipalBuilder().WithScenario(Guid.NewGuid(), ScenarioPermission.ExecuteScenario).Build());

        Assert.True(context.HasFailed);
    }

    [Fact]
    public async Task ScenarioPermissionHandler_does_not_succeed_for_another_permission_on_the_scenario()
    {
        var scenarioId = Guid.NewGuid();

        var context = await AuthorizationHarness.HandleAsync(
            new ScenarioPermissionHandler(),
            new ScenarioPermissionRequirement([ScenarioPermission.ExecuteScenario], scenarioId),
            new ClaimsPrincipalBuilder().WithScenario(scenarioId, ScenarioPermission.EditScenario).Build());

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task ScenarioTemplatePermissionHandler_succeeds_for_a_permission_held_on_the_template()
    {
        var templateId = Guid.NewGuid();

        var context = await AuthorizationHarness.HandleAsync(
            new ScenarioTemplatePermissionHandler(),
            new ScenarioTemplatePermissionRequirement([ScenarioTemplatePermission.EditScenarioTemplate], templateId),
            new ClaimsPrincipalBuilder().WithScenarioTemplate(templateId, ScenarioTemplatePermission.EditScenarioTemplate).Build());

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task ScenarioTemplatePermissionHandler_fails_without_a_claim_on_the_template()
    {
        var context = await AuthorizationHarness.HandleAsync(
            new ScenarioTemplatePermissionHandler(),
            new ScenarioTemplatePermissionRequirement([ScenarioTemplatePermission.EditScenarioTemplate], Guid.NewGuid()),
            new ClaimsPrincipalBuilder().WithSystemPermissions(SystemPermission.EditScenarioTemplates).Build());

        Assert.True(context.HasFailed);
    }
}
