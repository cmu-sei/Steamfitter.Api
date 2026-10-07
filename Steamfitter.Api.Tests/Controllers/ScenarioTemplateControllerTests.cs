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

/// <summary>
/// <c>ScenarioTemplateController</c>: <c>api/scenarioTemplates</c>. Each gate takes a system permission,
/// or the matching permission from a membership on the template itself.
/// </summary>
public class ScenarioTemplateControllerTests(DatabaseFixture fixture, SteamfitterAppFactory factory)
    : ApiTestBase(fixture, factory)
{
    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task An_unauthenticated_request_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync("api/scenarioTemplates", Ct));
    }

    [Fact]
    public async Task GetAll_returns_every_template_to_a_caller_holding_ViewScenarioTemplates()
    {
        var template = TestData.ScenarioTemplate("Unrelated");
        await Seed(template);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        var templates = await ReadAsync<List<SAVM.ScenarioTemplate>>(await Client(actor).GetAsync("api/scenarioTemplates", Ct));

        Assert.Contains(template.Id, templates.Select(x => x.Id));
    }

    [Fact]
    public async Task GetAll_returns_only_the_templates_a_caller_without_ViewScenarioTemplates_is_a_member_of()
    {
        var mine = TestData.ScenarioTemplate("Mine");
        var other = TestData.ScenarioTemplate("Other");
        await Seed(mine, other);
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.CreateScenarioTemplates)
            .OnScenarioTemplate(mine.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate])
            .SeedAsync();

        var templates = await ReadAsync<List<SAVM.ScenarioTemplate>>(await Client(actor).GetAsync("api/scenarioTemplates", Ct));

        Assert.Equal([mine.Id], templates.Select(x => x.Id));
    }

    [Fact]
    public async Task GetAll_lists_the_permissions_the_caller_holds_on_each_template()
    {
        var mine = TestData.ScenarioTemplate("Mine");
        await Seed(mine);
        var actor = await Actor()
            .OnScenarioTemplate(mine.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate, ScenarioTemplatePermission.EditScenarioTemplate])
            .SeedAsync();

        var templates = await ReadAsync<List<SAVM.ScenarioTemplate>>(await Client(actor).GetAsync("api/scenarioTemplates", Ct));

        Assert.Equal(["EditScenarioTemplate", "ViewScenarioTemplate"], Assert.Single(templates).ScenarioTemplatePermissions.Order());
    }

    /// <summary>A template the caller reaches only through a group membership is left out of the caller's list.</summary>
    [Fact]
    public async Task GetAll_leaves_out_a_template_the_caller_is_a_member_of_through_a_group()
    {
        var group = TestData.Group();
        var template = TestData.ScenarioTemplate("Group's");
        await Seed(group, template, TestData.ScenarioTemplateMembership(template.Id, groupId: group.Id, roleId: TestData.ScenarioTemplateRoles.Observer));
        var actor = await Actor().InGroup(group.Id).SeedAsync();

        var templates = await ReadAsync<List<SAVM.ScenarioTemplate>>(await Client(actor).GetAsync("api/scenarioTemplates", Ct));

        Assert.Empty(templates);
    }

    // Same case as GetAll_leaves_out_a_template_the_caller_is_a_member_of_through_a_group.
    [Fact]
    public async Task GetAll_lists_a_template_the_caller_created_once_per_membership_on_it()
    {
        var colleague = TestData.User(name: "Colleague");
        await Seed(colleague);
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateScenarioTemplates).SeedAsync();
        var template = TestData.ScenarioTemplate("Created", createdBy: actor.Id);
        await Seed(
            template,
            TestData.ScenarioTemplateMembership(template.Id, userId: actor.Id, roleId: TestData.ScenarioTemplateRoles.Manager),
            TestData.ScenarioTemplateMembership(template.Id, userId: colleague.Id, roleId: TestData.ScenarioTemplateRoles.Observer));

        var templates = await ReadAsync<List<SAVM.ScenarioTemplate>>(await Client(actor).GetAsync("api/scenarioTemplates", Ct));

        Assert.Equal([template.Id, template.Id], templates.Select(x => x.Id));
    }

    [Fact]
    public async Task Get_returns_the_template_to_a_caller_holding_ViewScenarioTemplates()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        var read = await ReadAsync<SAVM.ScenarioTemplate>(await Client(actor).GetAsync($"api/scenarioTemplates/{template.Id}", Ct));

        Assert.Equal(template.Name, read.Name);
    }

    [Fact]
    public async Task Get_returns_the_template_to_a_member_holding_ViewScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate]).SeedAsync();

        var read = await ReadAsync<SAVM.ScenarioTemplate>(await Client(actor).GetAsync($"api/scenarioTemplates/{template.Id}", Ct));

        Assert.Equal(template.Id, read.Id);
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_only_CreateScenarioTemplates()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/scenarioTemplates/{template.Id}", Ct));
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_ViewScenarioTemplate_only_on_another_template()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().OnNewScenarioTemplate(ScenarioTemplatePermission.ViewScenarioTemplate).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/scenarioTemplates/{template.Id}", Ct));
    }

    [Fact]
    public async Task Get_of_an_unknown_template_is_not_found()
    {
        var error = await AssertJsonError(HttpStatusCode.NotFound, await RootClient.GetAsync($"api/scenarioTemplates/{Guid.NewGuid()}", Ct));

        Assert.Equal("Scenario Template not found", error.Title);
    }

    [Fact]
    public async Task Create_persists_the_template_and_makes_the_caller_its_manager()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateScenarioTemplates).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync("api/scenarioTemplates", new { name = "Created", durationHours = 4 }, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var created = await ReadAsync<SAVM.ScenarioTemplate>(response);
        await using var db = NewContext();
        var stored = await db.ScenarioTemplates.Include(x => x.Memberships).SingleAsync(x => x.Id == created.Id, Ct);
        Assert.Equal(("Created", 4, actor.Id), (stored.Name, stored.DurationHours.Value, stored.CreatedBy));
        var membership = Assert.Single(stored.Memberships);
        Assert.Equal((actor.Id, TestData.ScenarioTemplateRoles.Manager), (membership.UserId.Value, membership.RoleId));
    }

    [Fact]
    public async Task Create_broadcasts_the_template_to_the_template_administrators_and_its_own_group()
    {
        var response = await RootClient.PostAsJsonAsync("api/scenarioTemplates", new { name = "Broadcast" }, Ct);

        var created = await ReadAsync<SAVM.ScenarioTemplate>(response);
        var sent = Assert.Single(Factory.Hub<EngineHub>().ToGroup(created.Id), x => x.Method == EngineMethods.ScenarioTemplateCreated);
        Assert.Equal(created.Id, Assert.IsType<SAVM.ScenarioTemplate>(sent.Argument).Id);
        Assert.Contains(Factory.Hub<EngineHub>().ToGroup(EngineHub.SCENARIO_TEMPLATE_GROUP),
            x => x.Method == EngineMethods.ScenarioTemplateCreated && ((SAVM.ScenarioTemplate)x.Argument).Id == created.Id);
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_only_EditScenarioTemplates()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarioTemplates).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync("api/scenarioTemplates", new { name = "Refused" }, Ct);

        await AssertStatus(HttpStatusCode.Forbidden, response);
        await using var db = NewContext();
        Assert.False(await db.ScenarioTemplates.AnyAsync(x => x.Name == "Refused", Ct));
    }

    [Fact]
    public async Task Copy_copies_the_template_its_tasks_and_credentials_for_a_creator_who_may_view_it()
    {
        var template = TestData.ScenarioTemplate("Original");
        await Seed(template, TestData.TemplateTask(template.Id, "Copied Task"), TestData.VmCredential(scenarioTemplateId: template.Id, username: "copied"));
        var actor = await Actor()
            .WithName("Copier")
            .WithSystemPermissions(SystemPermission.CreateScenarioTemplates)
            .OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate])
            .SeedAsync();

        var response = await Client(actor).PostAsync($"api/scenarioTemplates/{template.Id}/copy", null, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var copy = await ReadAsync<SAVM.ScenarioTemplate>(response);
        await using var db = NewContext();
        var stored = await db.ScenarioTemplates.Include(x => x.Tasks).Include(x => x.VmCredentials).SingleAsync(x => x.Id == copy.Id, Ct);
        Assert.Equal("Original - Copier", stored.Name);
        Assert.Equal("Copied Task", Assert.Single(stored.Tasks).Name);
        Assert.Equal("copied", Assert.Single(stored.VmCredentials).Username);
    }

    /// <summary>The caller's membership on the original is moved onto the copy.</summary>
    [Fact]
    public async Task Copy_moves_the_callers_membership_from_the_original_to_the_copy()
    {
        var template = TestData.ScenarioTemplate("Original");
        await Seed(template);
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.CreateScenarioTemplates)
            .OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate])
            .SeedAsync();

        var copy = await ReadAsync<SAVM.ScenarioTemplate>(await Client(actor).PostAsync($"api/scenarioTemplates/{template.Id}/copy", null, Ct));

        await using var db = NewContext();
        var rows = await db.ScenarioTemplateMemberships.Where(x => x.UserId == actor.Id).ToListAsync(Ct);
        Assert.Equal([(actor.ScenarioTemplateMemberships[0].Id, copy.Id)], rows.Select(x => (x.Id, x.ScenarioTemplateId)));
    }

    [Fact]
    public async Task Copy_is_forbidden_for_a_caller_holding_only_ViewScenarioTemplates()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsync($"api/scenarioTemplates/{template.Id}/copy", null, Ct));
    }

    [Fact]
    public async Task Copy_is_forbidden_for_a_creator_holding_ViewScenarioTemplate_only_on_another_template()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.CreateScenarioTemplates)
            .OnNewScenarioTemplate(ScenarioTemplatePermission.ViewScenarioTemplate)
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsync($"api/scenarioTemplates/{template.Id}/copy", null, Ct));
    }

    [Fact]
    public async Task Update_saves_the_form_for_a_caller_holding_EditScenarioTemplates()
    {
        var template = TestData.ScenarioTemplate("Before");
        await Seed(template);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarioTemplates).SeedAsync();

        var response = await Client(actor).PutAsJsonAsync($"api/scenarioTemplates/{template.Id}", new { name = "After", description = "Changed" }, Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        await using var db = NewContext();
        var stored = await db.ScenarioTemplates.SingleAsync(x => x.Id == template.Id, Ct);
        Assert.Equal(("After", "Changed", actor.Id), (stored.Name, stored.Description, stored.ModifiedBy.Value));
    }

    [Fact]
    public async Task Update_saves_the_form_for_a_member_holding_EditScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate("Before");
        await Seed(template);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/scenarioTemplates/{template.Id}", new { name = "After" }, Ct));
    }

    [Fact]
    public async Task Update_broadcasts_the_template_to_its_own_group()
    {
        var template = TestData.ScenarioTemplate("Before");
        await Seed(template);

        await ReadAsync<SAVM.ScenarioTemplate>(await RootClient.PutAsJsonAsync($"api/scenarioTemplates/{template.Id}", new { name = "Broadcast" }, Ct));

        var sent = Assert.Single(Factory.Hub<EngineHub>().ToGroup(template.Id), x => x.Method == EngineMethods.ScenarioTemplateUpdated);
        Assert.Equal("Broadcast", Assert.IsType<SAVM.ScenarioTemplate>(sent.Argument).Name);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_member_holding_only_ViewScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate("Before");
        await Seed(template);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/scenarioTemplates/{template.Id}", new { name = "After" }, Ct));
        await using var db = NewContext();
        Assert.Equal("Before", (await db.ScenarioTemplates.SingleAsync(x => x.Id == template.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_EditScenarioTemplate_only_on_another_template()
    {
        var template = TestData.ScenarioTemplate("Before");
        await Seed(template);
        var actor = await Actor().OnNewScenarioTemplate(ScenarioTemplatePermission.EditScenarioTemplate).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/scenarioTemplates/{template.Id}", new { name = "After" }, Ct));
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_only_ViewScenarioTemplates()
    {
        var template = TestData.ScenarioTemplate("Before");
        await Seed(template);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/scenarioTemplates/{template.Id}", new { name = "After" }, Ct));
    }

    [Fact]
    public async Task Delete_removes_the_template_for_a_caller_holding_ManageScenarioTemplates()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/scenarioTemplates/{template.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.ScenarioTemplates.AnyAsync(x => x.Id == template.Id, Ct));
    }

    [Fact]
    public async Task Delete_removes_the_template_for_a_member_holding_ManageScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ManageScenarioTemplate]).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/scenarioTemplates/{template.Id}", Ct));
    }

    [Fact]
    public async Task Delete_broadcasts_the_id_to_the_template_administrators()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);

        await AssertStatus(HttpStatusCode.NoContent, await RootClient.DeleteAsync($"api/scenarioTemplates/{template.Id}", Ct));

        Assert.Contains(Factory.Hub<EngineHub>().ToGroup(EngineHub.SCENARIO_TEMPLATE_GROUP),
            x => x.Method == EngineMethods.ScenarioTemplateDeleted && (Guid)x.Argument == template.Id);
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_only_EditScenarioTemplates()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/scenarioTemplates/{template.Id}", Ct));
        await using var db = NewContext();
        Assert.True(await db.ScenarioTemplates.AnyAsync(x => x.Id == template.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_member_holding_only_EditScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/scenarioTemplates/{template.Id}", Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_ManageScenarioTemplate_only_on_another_template()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().OnNewScenarioTemplate(ScenarioTemplatePermission.ManageScenarioTemplate).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/scenarioTemplates/{template.Id}", Ct));
    }
}
