// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// Run through Steamfitter's UserClaimsService, with caching and the IdP role and group paths off as
// TestConfiguration has them.

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Steamfitter.Api.Data;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Infrastructure.Options;
using Steamfitter.Api.Services;

namespace Steamfitter.Api.Tests.Support;

/// <summary>Tests for <see cref="TestActorBuilder"/>: an actor that holds more than asked turns an authorization test into a formality.</summary>
public class TestActorTests(DatabaseFixture fixture) : DatabaseTestBase(fixture)
{
    [Fact]
    public async Task WithAllSystemPermissions_grants_every_system_permission()
    {
        var actor = await Actor().WithAllSystemPermissions().SeedAsync();

        Assert.Equal(Enum.GetNames<SystemPermission>().Order(), Permissions(await ClaimsOf(actor)));
    }

    [Fact]
    public async Task WithSystemPermissions_grants_exactly_what_it_names()
    {
        var first = Enum.GetValues<SystemPermission>()[0];
        var actor = await Actor().WithSystemPermissions(first).SeedAsync();

        Assert.Equal([first.ToString()], Permissions(await ClaimsOf(actor)));
    }

    [Fact]
    public async Task An_actor_with_no_role_holds_nothing()
    {
        var actor = await Actor().SeedAsync();

        var claims = await ClaimsOf(actor);

        Assert.Empty(Permissions(claims));
        Assert.Empty(ScenarioClaims(claims));
        Assert.Empty(ScenarioTemplateClaims(claims));
    }

    [Fact]
    public void WithRole_after_WithSystemPermissions_throws()
    {
        var builder = Actor().WithSystemPermissions(Enum.GetValues<SystemPermission>()[0]);

        Assert.Throws<InvalidOperationException>(() => builder.WithRole(TestData.Roles.Administrator));
    }

    [Fact]
    public void WithSystemPermissions_after_WithRole_throws()
    {
        var builder = Actor().WithRole(TestData.Roles.Observer);

        Assert.Throws<InvalidOperationException>(() => builder.WithSystemPermissions(SystemPermission.ViewScenarios));
    }

    [Fact]
    public async Task OnScenario_with_permissions_grants_exactly_those_on_that_scenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);

        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ExecuteScenario]).SeedAsync();

        var claim = Assert.Single(ScenarioClaims(await ClaimsOf(actor)));
        Assert.Equal(scenario.Id, claim.ScenarioId);
        Assert.Equal([ScenarioPermission.ExecuteScenario], claim.Permissions);
    }

    [Fact]
    public async Task OnScenario_with_a_seeded_role_grants_that_role()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);

        var actor = await Actor().OnScenario(scenario.Id, TestData.ScenarioRoles.Facilitator).SeedAsync();

        var claim = Assert.Single(ScenarioClaims(await ClaimsOf(actor)));
        Assert.Equal([ScenarioPermission.ViewScenario, ScenarioPermission.ExecuteScenario], claim.Permissions.Order());
    }

    [Fact]
    public void OnScenario_with_neither_a_role_nor_permissions_throws()
    {
        Assert.Throws<InvalidOperationException>(() => Actor().OnScenario(Guid.NewGuid()));
    }

    [Fact]
    public void OnScenario_with_both_a_role_and_permissions_throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Actor().OnScenario(Guid.NewGuid(), TestData.ScenarioRoles.Observer, ScenarioPermission.ViewScenario));
    }

    /// <summary>The minted scenario's role grants nothing beyond what the call names.</summary>
    [Fact]
    public async Task OnNewScenario_grants_exactly_what_it_names_on_a_scenario_of_its_own()
    {
        var actor = await Actor().OnNewScenario(ScenarioPermission.ViewTasks).SeedAsync();

        var claim = Assert.Single(ScenarioClaims(await ClaimsOf(actor)));
        Assert.Equal(actor.ScenarioMemberships[0].ResourceId, claim.ScenarioId);
        Assert.Equal([ScenarioPermission.ViewTasks], claim.Permissions);
    }

    [Fact]
    public async Task OnScenarioTemplate_with_permissions_grants_exactly_those_on_that_template()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);

        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate]).SeedAsync();

        var claim = Assert.Single(ScenarioTemplateClaims(await ClaimsOf(actor)));
        Assert.Equal(template.Id, claim.ScenarioTemplateId);
        Assert.Equal([ScenarioTemplatePermission.EditScenarioTemplate], claim.Permissions);
    }

    [Fact]
    public async Task OnNewScenarioTemplate_grants_exactly_what_it_names_on_a_template_of_its_own()
    {
        var actor = await Actor().OnNewScenarioTemplate(ScenarioTemplatePermission.ViewScenarioTemplate).SeedAsync();

        var claim = Assert.Single(ScenarioTemplateClaims(await ClaimsOf(actor)));
        Assert.Equal(actor.ScenarioTemplateMemberships[0].ResourceId, claim.ScenarioTemplateId);
        Assert.Equal([ScenarioTemplatePermission.ViewScenarioTemplate], claim.Permissions);
    }

    /// <summary>A group's scenario membership reaches its members through the transformer's group lookup.</summary>
    [Fact]
    public async Task InGroup_grants_what_the_group_holds()
    {
        var group = TestData.Group();
        var scenario = TestData.Scenario();
        await Seed(group, scenario, TestData.ScenarioMembership(scenario.Id, groupId: group.Id, roleId: TestData.ScenarioRoles.Observer));

        var actor = await Actor().InGroup(group.Id).SeedAsync();

        var claim = Assert.Single(ScenarioClaims(await ClaimsOf(actor)));
        Assert.Equal(scenario.Id, claim.ScenarioId);
        Assert.Equal([ScenarioPermission.ViewScenario], claim.Permissions);
    }

    private TestActorBuilder Actor() => new(Db, Ct);

    private async Task<ClaimsPrincipal> ClaimsOf(TestActor actor)
    {
        await using var context = NewContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new UserClaimsService(context, cache, new ClaimsTransformationOptions());
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", actor.Id.ToString())], "Test"));

        return await service.AddUserClaims(principal, update: false);
    }

    private static string[] Permissions(ClaimsPrincipal principal) =>
        [.. principal.Claims
            .Where(x => x.Type == AuthorizationConstants.PermissionClaimType)
            .Select(x => x.Value)
            .Order()];

    private static ScenarioPermissionClaim[] ScenarioClaims(ClaimsPrincipal principal) =>
        [.. principal.Claims
            .Where(x => x.Type == AuthorizationConstants.ScenarioPermissionClaimType)
            .Select(x => ScenarioPermissionClaim.FromString(x.Value))];

    private static ScenarioTemplatePermissionClaim[] ScenarioTemplateClaims(ClaimsPrincipal principal) =>
        [.. principal.Claims
            .Where(x => x.Type == AuthorizationConstants.ScenarioTemplatePermissionClaimType)
            .Select(x => ScenarioTemplatePermissionClaim.FromString(x.Value))];
}
