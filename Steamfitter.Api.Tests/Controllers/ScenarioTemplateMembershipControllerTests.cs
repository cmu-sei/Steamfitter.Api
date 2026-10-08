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
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Tests.Support;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Tests.Controllers;

/// <summary><c>ScenarioTemplateMembershipsController</c>: who belongs to a scenario template, with which role.</summary>
public class ScenarioTemplateMembershipControllerTests(DatabaseFixture fixture, SteamfitterAppFactory factory)
    : ApiTestBase(fixture, factory)
{
    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task An_unauthenticated_request_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync($"api/scenarioTemplates/{Guid.NewGuid()}/memberships", Ct));
    }

    [Fact]
    public async Task Get_returns_the_membership_to_a_member_holding_ViewScenarioTemplate()
    {
        var (template, membership) = await SeedTemplateWithMember();
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate]).SeedAsync();

        var read = await ReadAsync<SAVM.ScenarioTemplateMembership>(await Client(actor).GetAsync($"api/scenarioTemplates/memberships/{membership.Id}", Ct));

        Assert.Equal((membership.UserId, TestData.ScenarioTemplateRoles.Observer), (read.UserId, read.RoleId));
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_ViewScenarioTemplate_only_on_another_template()
    {
        var (_, membership) = await SeedTemplateWithMember();
        var actor = await Actor().OnNewScenarioTemplate(ScenarioTemplatePermission.ViewScenarioTemplate).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/scenarioTemplates/memberships/{membership.Id}", Ct));
    }

    [Fact]
    public async Task GetAll_returns_the_templates_memberships_to_a_caller_holding_ViewScenarioTemplates()
    {
        var (template, membership) = await SeedTemplateWithMember();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        var memberships = await ReadAsync<List<SAVM.ScenarioTemplateMembership>>(await Client(actor).GetAsync($"api/scenarioTemplates/{template.Id}/memberships", Ct));

        Assert.Equal([membership.Id], memberships.Select(x => x.Id));
    }

    [Fact]
    public async Task GetAll_is_forbidden_for_a_caller_holding_only_ViewScenarios()
    {
        var (template, _) = await SeedTemplateWithMember();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/scenarioTemplates/{template.Id}/memberships", Ct));
    }

    [Fact]
    public async Task Create_adds_a_group_to_the_template_for_a_member_holding_ManageScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        var group = TestData.Group();
        await Seed(template, group);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ManageScenarioTemplate]).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync(
            $"api/scenarioTemplates/{template.Id}/memberships",
            new { scenarioTemplateId = template.Id, groupId = group.Id, roleId = TestData.ScenarioTemplateRoles.Observer },
            Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        await using var db = NewContext();
        Assert.Equal(TestData.ScenarioTemplateRoles.Observer, (await db.ScenarioTemplateMemberships.SingleAsync(x => x.GroupId == group.Id, Ct)).RoleId);
        Assert.Contains(Factory.Hub<EngineHub>().ToGroup(EngineHub.SCENARIO_TEMPLATE_GROUP), x => x.Method == EngineMethods.ScenarioTemplateMembershipCreated);
    }

    [Fact]
    public async Task Create_adds_a_member_for_a_caller_holding_the_manager_role_on_the_template()
    {
        var template = TestData.ScenarioTemplate();
        var user = TestData.User(name: "Joiner");
        await Seed(template, user);
        var actor = await Actor().OnScenarioTemplate(template.Id, roleId: TestData.ScenarioTemplateRoles.Manager).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync(
            $"api/scenarioTemplates/{template.Id}/memberships",
            new { scenarioTemplateId = template.Id, userId = user.Id, roleId = TestData.ScenarioTemplateRoles.Member },
            Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        await using var db = NewContext();
        Assert.Equal(TestData.ScenarioTemplateRoles.Member, (await db.ScenarioTemplateMemberships.SingleAsync(x => x.ScenarioTemplateId == template.Id && x.UserId == user.Id, Ct)).RoleId);
    }

    /// <summary>A caller holding only the system ManageScenarioTemplates makes itself the template's Manager and then holds EditScenarioTemplate on it.</summary>
    [Fact]
    public async Task Create_lets_a_caller_holding_only_ManageScenarioTemplates_make_itself_the_templates_manager()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageScenarioTemplates).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync(
            $"api/scenarioTemplates/{template.Id}/memberships",
            new { scenarioTemplateId = template.Id, userId = actor.Id, roleId = TestData.ScenarioTemplateRoles.Manager },
            Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        await using var db = NewContext();
        Assert.Equal(TestData.ScenarioTemplateRoles.Manager, (await db.ScenarioTemplateMemberships.SingleAsync(x => x.ScenarioTemplateId == template.Id && x.UserId == actor.Id, Ct)).RoleId);
        var claims = await ReadAsync<List<ScenarioTemplatePermissionClaim>>(await Client(actor).GetAsync($"api/scenarioTemplates/{template.Id}/me/permissions", Ct));
        Assert.Contains(ScenarioTemplatePermission.EditScenarioTemplate, Assert.Single(claims, x => x.ScenarioTemplateId == template.Id).Permissions);
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_member_holding_only_EditScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        var user = TestData.User();
        await Seed(template, user);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync(
            $"api/scenarioTemplates/{template.Id}/memberships",
            new { scenarioTemplateId = template.Id, userId = user.Id, roleId = TestData.ScenarioTemplateRoles.Member },
            Ct));
        await using var db = NewContext();
        Assert.False(await db.ScenarioTemplateMemberships.AnyAsync(x => x.UserId == user.Id, Ct));
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_ManageScenarioTemplate_only_on_another_template()
    {
        var template = TestData.ScenarioTemplate();
        var user = TestData.User();
        await Seed(template, user);
        var actor = await Actor().OnNewScenarioTemplate(ScenarioTemplatePermission.ManageScenarioTemplate).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync(
            $"api/scenarioTemplates/{template.Id}/memberships",
            new { scenarioTemplateId = template.Id, userId = user.Id, roleId = TestData.ScenarioTemplateRoles.Member },
            Ct));
    }

    /// <summary>A body naming another template than the route is answered with a 500.</summary>
    [Fact]
    public async Task Create_answers_a_body_naming_another_template_with_a_server_error()
    {
        var template = TestData.ScenarioTemplate();
        var other = TestData.ScenarioTemplate("Other");
        var user = TestData.User();
        await Seed(template, other, user);

        var response = await RootClient.PostAsJsonAsync(
            $"api/scenarioTemplates/{template.Id}/memberships",
            new { scenarioTemplateId = other.Id, userId = user.Id, roleId = TestData.ScenarioTemplateRoles.Member },
            Ct);

        var error = await AssertJsonError(HttpStatusCode.InternalServerError, response);
        Assert.Equal("The ScenarioTemplateId of the membership must match the ScenarioTemplateId of the URL.", error.Detail);
    }

    [Fact]
    public async Task Update_changes_the_role_for_a_member_holding_ManageScenarioTemplate()
    {
        var (template, membership) = await SeedTemplateWithMember();
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ManageScenarioTemplate]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync(
            $"api/scenarioTemplates/memberships/{membership.Id}",
            new { id = membership.Id, scenarioTemplateId = template.Id, userId = membership.UserId, roleId = TestData.ScenarioTemplateRoles.Manager },
            Ct));

        await using var db = NewContext();
        Assert.Equal(TestData.ScenarioTemplateRoles.Manager, (await db.ScenarioTemplateMemberships.SingleAsync(x => x.Id == membership.Id, Ct)).RoleId);
    }

    [Fact]
    public async Task Update_makes_an_observer_a_member_for_a_caller_holding_the_manager_role_on_the_template()
    {
        var (template, membership) = await SeedTemplateWithMember();
        var actor = await Actor().OnScenarioTemplate(template.Id, roleId: TestData.ScenarioTemplateRoles.Manager).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync(
            $"api/scenarioTemplates/memberships/{membership.Id}",
            new { id = membership.Id, scenarioTemplateId = template.Id, userId = membership.UserId, roleId = TestData.ScenarioTemplateRoles.Member },
            Ct));

        await using var db = NewContext();
        Assert.Equal(TestData.ScenarioTemplateRoles.Member, (await db.ScenarioTemplateMemberships.SingleAsync(x => x.Id == membership.Id, Ct)).RoleId);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_member_holding_only_EditScenarioTemplate()
    {
        var (template, membership) = await SeedTemplateWithMember();
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync(
            $"api/scenarioTemplates/memberships/{membership.Id}",
            new { id = membership.Id, scenarioTemplateId = template.Id, userId = membership.UserId, roleId = TestData.ScenarioTemplateRoles.Manager },
            Ct));
        await using var db = NewContext();
        Assert.Equal(TestData.ScenarioTemplateRoles.Observer, (await db.ScenarioTemplateMemberships.SingleAsync(x => x.Id == membership.Id, Ct)).RoleId);
    }

    [Fact]
    public async Task Delete_removes_the_membership_for_a_caller_holding_ManageScenarioTemplates()
    {
        var (_, membership) = await SeedTemplateWithMember();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/scenarioTemplates/memberships/{membership.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.ScenarioTemplateMemberships.AnyAsync(x => x.Id == membership.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_ManageScenarioTemplate_only_on_another_template()
    {
        var (_, membership) = await SeedTemplateWithMember();
        var actor = await Actor().OnNewScenarioTemplate(ScenarioTemplatePermission.ManageScenarioTemplate).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/scenarioTemplates/memberships/{membership.Id}", Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_only_EditScenarioTemplates()
    {
        var (_, membership) = await SeedTemplateWithMember();
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/scenarioTemplates/memberships/{membership.Id}", Ct));
    }

    private async Task<(Data.Models.ScenarioTemplateEntity Template, Data.Models.ScenarioTemplateMembershipEntity Membership)> SeedTemplateWithMember()
    {
        var template = TestData.ScenarioTemplate();
        var user = TestData.User(name: "Member");
        var membership = TestData.ScenarioTemplateMembership(template.Id, userId: user.Id, roleId: TestData.ScenarioTemplateRoles.Observer);
        await Seed(template, user, membership);

        return (template, membership);
    }
}
