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
/// <c>ScenarioController</c>: <c>api/scenarios</c> and <c>api/scenarioTemplates/{id}/scenarios</c>. Each
/// per-scenario gate takes a system permission, or the matching permission from a membership on the
/// scenario.
/// </summary>
public class ScenarioControllerTests(DatabaseFixture fixture, SteamfitterAppFactory factory)
    : ApiTestBase(fixture, factory)
{
    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task An_unauthenticated_request_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync("api/scenarios", Ct));
    }

    [Fact]
    public async Task GetAll_returns_every_scenario_to_a_caller_holding_ViewScenarios()
    {
        var scenario = TestData.Scenario("Unrelated");
        await Seed(scenario);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        var scenarios = await ReadAsync<List<SAVM.Scenario>>(await Client(actor).GetAsync("api/scenarios", Ct));

        Assert.Contains(scenario.Id, scenarios.Select(x => x.Id));
    }

    [Fact]
    public async Task GetAll_returns_only_their_scenarios_to_a_caller_holding_only_CreateScenarios()
    {
        var mine = TestData.Scenario("Mine");
        var other = TestData.Scenario("Other");
        await Seed(mine, other);
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.CreateScenarios)
            .OnScenario(mine.Id, permissions: [ScenarioPermission.ViewScenario])
            .SeedAsync();

        var scenarios = await ReadAsync<List<SAVM.Scenario>>(await Client(actor).GetAsync("api/scenarios", Ct));

        var listed = Assert.Single(scenarios);
        Assert.Equal(mine.Id, listed.Id);
        Assert.Equal(["ViewScenario"], listed.ScenarioPermissions);
    }

    [Fact]
    public async Task GetAll_is_forbidden_for_a_member_holding_only_ExecuteScenarios()
    {
        var mine = TestData.Scenario("Mine");
        await Seed(mine);
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.ExecuteScenarios)
            .OnScenario(mine.Id, permissions: [ScenarioPermission.ViewScenario])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync("api/scenarios", Ct));
    }

    /// <summary>A scenario the caller created is listed once per membership it has.</summary>
    [Fact]
    public async Task GetAll_lists_a_scenario_the_caller_created_once_per_membership_on_it()
    {
        var colleague = TestData.User(name: "Colleague");
        await Seed(colleague);
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateScenarios).SeedAsync();
        var scenario = TestData.Scenario("Created", createdBy: actor.Id);
        await Seed(
            scenario,
            TestData.ScenarioMembership(scenario.Id, userId: actor.Id, roleId: TestData.ScenarioRoles.Manager),
            TestData.ScenarioMembership(scenario.Id, userId: colleague.Id, roleId: TestData.ScenarioRoles.Observer));

        var scenarios = await ReadAsync<List<SAVM.Scenario>>(await Client(actor).GetAsync("api/scenarios", Ct));

        Assert.Equal([scenario.Id, scenario.Id], scenarios.Select(x => x.Id));
    }

    [Fact]
    public async Task GetByViewId_returns_the_views_scenarios_to_a_caller_holding_ViewScenarios()
    {
        var viewId = Guid.NewGuid();
        var inView = TestData.Scenario("In view", viewId: viewId);
        var elsewhere = TestData.Scenario("Elsewhere", viewId: Guid.NewGuid());
        await Seed(inView, elsewhere);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        var scenarios = await ReadAsync<List<SAVM.Scenario>>(await Client(actor).GetAsync($"api/scenarios/view/{viewId}", Ct));

        Assert.Equal([inView.Id], scenarios.Select(x => x.Id));
    }

    [Fact]
    public async Task GetByViewId_is_forbidden_for_a_member_of_the_views_scenario_holding_only_ViewScenario()
    {
        var viewId = Guid.NewGuid();
        var inView = TestData.Scenario("In view", viewId: viewId);
        await Seed(inView);
        var actor = await Actor().OnScenario(inView.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/scenarios/view/{viewId}", Ct));
    }

    [Fact]
    public async Task Get_returns_the_scenario_to_a_member_holding_ViewScenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        var read = await ReadAsync<SAVM.Scenario>(await Client(actor).GetAsync($"api/scenarios/{scenario.Id}", Ct));

        Assert.Equal(scenario.Name, read.Name);
    }

    [Fact]
    public async Task Get_returns_the_scenario_to_a_caller_holding_ViewScenarios()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync($"api/scenarios/{scenario.Id}", Ct));
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_only_ViewScenarioTemplates()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/scenarios/{scenario.Id}", Ct));
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_ViewScenario_only_on_another_scenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().OnNewScenario(ScenarioPermission.ViewScenario).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/scenarios/{scenario.Id}", Ct));
    }

    /// <summary>An unknown scenario id is answered with a 500.</summary>
    [Fact]
    public async Task Get_answers_an_unknown_scenario_with_a_server_error()
    {
        var error = await AssertJsonError(HttpStatusCode.InternalServerError, await RootClient.GetAsync($"api/scenarios/{Guid.NewGuid()}", Ct));

        Assert.Contains("Object reference not set", error.Detail);
    }

    [Fact]
    public async Task GetMyScenario_creates_the_callers_personal_scenario_for_a_caller_holding_ManageTasks()
    {
        var actor = await Actor().WithName("Builder").WithSystemPermissions(SystemPermission.ManageTasks).SeedAsync();

        var scenario = await ReadAsync<SAVM.Scenario>(await Client(actor).GetAsync("api/scenarios/me", Ct));

        Assert.Equal((actor.Id, "Builder User Scenario"), (scenario.Id, scenario.Name));
        await using var db = NewContext();
        Assert.Equal(ScenarioStatus.active, (await db.Scenarios.SingleAsync(x => x.Id == actor.Id, Ct)).Status);
    }

    [Fact]
    public async Task GetMyScenario_is_forbidden_for_a_caller_holding_only_ExecuteScenarios()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ExecuteScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync("api/scenarios/me", Ct));
    }

    [Fact]
    public async Task Create_persists_the_scenario_and_makes_the_caller_its_manager()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateScenarios).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync("api/scenarios", new { name = "Created", description = "New" }, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var created = await ReadAsync<SAVM.Scenario>(response);
        await using var db = NewContext();
        var stored = await db.Scenarios.Include(x => x.Memberships).SingleAsync(x => x.Id == created.Id, Ct);
        Assert.Equal(("Created", ScenarioStatus.ready, actor.Id), (stored.Name, stored.Status, stored.CreatedBy));
        var membership = Assert.Single(stored.Memberships);
        Assert.Equal((actor.Id, TestData.ScenarioRoles.Manager), (membership.UserId.Value, membership.RoleId));
    }

    /// <summary>The scenario's own group gets the summary first, then the full scenario; the administrators get the full one.</summary>
    [Fact]
    public async Task Create_broadcasts_the_scenario_to_the_scenario_administrators_and_its_own_group()
    {
        var created = await ReadAsync<SAVM.Scenario>(await RootClient.PostAsJsonAsync("api/scenarios", new { name = "Broadcast" }, Ct));

        var toScenario = Factory.Hub<EngineHub>().ToGroup(created.Id).Where(x => x.Method == EngineMethods.ScenarioCreated).ToList();
        Assert.Equal(2, toScenario.Count);
        Assert.All(toScenario, x => Assert.Equal(created.Id, Assert.IsType<SAVM.Scenario>(x.Argument).Id));
        Assert.Contains(Factory.Hub<EngineHub>().ToGroup(EngineHub.SCENARIO_GROUP),
            x => x.Method == EngineMethods.ScenarioCreated && ((SAVM.Scenario)x.Argument).Id == created.Id);
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_only_EditScenarios()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/scenarios", new { name = "Refused" }, Ct));
        await using var db = NewContext();
        Assert.False(await db.Scenarios.AnyAsync(x => x.Name == "Refused", Ct));
    }

    [Fact]
    public async Task CreateFromScenarioTemplate_copies_the_templates_tasks_and_adds_the_named_users_as_members()
    {
        var template = TestData.ScenarioTemplate("Template");
        var member = TestData.User(name: "Member");
        await Seed(template, member, TestData.TemplateTask(template.Id, "Template Task"));
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.CreateScenarios)
            .OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate])
            .SeedAsync();
        var viewId = Guid.NewGuid();

        var response = await Client(actor).PostAsJsonAsync(
            $"api/scenarioTemplates/{template.Id}/scenarios",
            new { nameSuffix = " (run)", viewId, userIds = new[] { member.Id } },
            Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var created = await ReadAsync<SAVM.Scenario>(response);
        await using var db = NewContext();
        var stored = await db.Scenarios.Include(x => x.Tasks).Include(x => x.Memberships).SingleAsync(x => x.Id == created.Id, Ct);
        Assert.Equal(("Template (run)", viewId, template.Id), (stored.Name, stored.ViewId.Value, stored.ScenarioTemplateId.Value));
        Assert.Equal("Template Task", Assert.Single(stored.Tasks).Name);
        Assert.Equal(
            [(actor.Id, TestData.ScenarioRoles.Manager), (member.Id, TestData.ScenarioRoles.Member)],
            stored.Memberships.Select(x => (x.UserId.Value, x.RoleId)).OrderBy(x => x.Item1 == actor.Id ? 0 : 1));
    }

    [Fact]
    public async Task CreateFromScenarioTemplate_is_forbidden_for_a_caller_holding_only_ViewScenarioTemplates()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/scenarioTemplates/{template.Id}/scenarios", new { }, Ct));
    }

    [Fact]
    public async Task CreateFromScenarioTemplate_is_forbidden_for_a_creator_holding_ViewScenarioTemplate_only_on_another_template()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.CreateScenarios)
            .OnNewScenarioTemplate(ScenarioTemplatePermission.ViewScenarioTemplate)
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/scenarioTemplates/{template.Id}/scenarios", new { }, Ct));
    }

    [Fact]
    public async Task Copy_copies_the_scenario_and_its_tasks_for_a_creator_who_may_view_it()
    {
        var scenario = TestData.Scenario("Original");
        await Seed(scenario, TestData.ScenarioTask(scenario.Id, "Copied Task"));
        var actor = await Actor()
            .WithName("Copier")
            .WithSystemPermissions(SystemPermission.CreateScenarios, SystemPermission.ViewScenarios)
            .SeedAsync();

        var response = await Client(actor).PostAsync($"api/scenarios/{scenario.Id}/copy", null, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var copy = await ReadAsync<SAVM.Scenario>(response);
        await using var db = NewContext();
        var stored = await db.Scenarios.Include(x => x.Tasks).Include(x => x.Memberships).SingleAsync(x => x.Id == copy.Id, Ct);
        Assert.Equal(("Original - Copier", ScenarioStatus.ready), (stored.Name, stored.Status));
        Assert.Equal("Copied Task", Assert.Single(stored.Tasks).Name);
        Assert.Equal((actor.Id, TestData.ScenarioRoles.Manager), (Assert.Single(stored.Memberships).UserId.Value, stored.Memberships.Single().RoleId));
    }

    /// <summary>The caller's membership on the original is moved onto the copy.</summary>
    [Fact]
    public async Task Copy_moves_the_callers_membership_from_the_original_to_the_copy()
    {
        var scenario = TestData.Scenario("Original");
        await Seed(scenario);
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.CreateScenarios)
            .OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario])
            .SeedAsync();

        var copy = await ReadAsync<SAVM.Scenario>(await Client(actor).PostAsync($"api/scenarios/{scenario.Id}/copy", null, Ct));

        await using var db = NewContext();
        var moved = await db.ScenarioMemberships.SingleAsync(x => x.Id == actor.ScenarioMemberships[0].Id, Ct);
        Assert.Equal(copy.Id, moved.ScenarioId);
    }

    [Fact]
    public async Task Copy_is_forbidden_for_a_caller_holding_only_ViewScenarios()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsync($"api/scenarios/{scenario.Id}/copy", null, Ct));
    }

    [Fact]
    public async Task Copy_is_forbidden_for_a_creator_holding_ViewScenario_only_on_another_scenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.CreateScenarios)
            .OnNewScenario(ScenarioPermission.ViewScenario)
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsync($"api/scenarios/{scenario.Id}/copy", null, Ct));
    }

    [Fact]
    public async Task Update_saves_the_form_for_a_member_holding_EditScenario()
    {
        var scenario = TestData.Scenario("Before");
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        var response = await Client(actor).PutAsJsonAsync($"api/scenarios/{scenario.Id}", new { name = "After", status = "ready" }, Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        await using var db = NewContext();
        var stored = await db.Scenarios.SingleAsync(x => x.Id == scenario.Id, Ct);
        Assert.Equal(("After", actor.Id), (stored.Name, stored.ModifiedBy.Value));
    }

    [Fact]
    public async Task Update_saves_the_form_for_a_caller_holding_EditScenarios()
    {
        var scenario = TestData.Scenario("Before");
        await Seed(scenario);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/scenarios/{scenario.Id}", new { name = "After", status = "ready" }, Ct));
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_member_holding_only_ViewScenario()
    {
        var scenario = TestData.Scenario("Before");
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/scenarios/{scenario.Id}", new { name = "After" }, Ct));
        await using var db = NewContext();
        Assert.Equal("Before", (await db.Scenarios.SingleAsync(x => x.Id == scenario.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_EditScenario_only_on_another_scenario()
    {
        var scenario = TestData.Scenario("Before");
        await Seed(scenario);
        var actor = await Actor().OnNewScenario(ScenarioPermission.EditScenario).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/scenarios/{scenario.Id}", new { name = "After" }, Ct));
    }

    [Fact]
    public async Task Start_activates_the_scenario_for_a_member_holding_ExecuteScenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ExecuteScenario]).SeedAsync();

        var response = await Client(actor).PutAsync($"api/scenarios/{scenario.Id}/start", null, Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        await using var db = NewContext();
        Assert.Equal(ScenarioStatus.active, (await db.Scenarios.SingleAsync(x => x.Id == scenario.Id, Ct)).Status);
    }

    /// <summary>A started scenario's tasks that are not triggered by hand are reset to pending for execution.</summary>
    [Fact]
    public async Task Start_resets_the_scenarios_timed_root_tasks_to_pending()
    {
        var scenario = TestData.Scenario();
        var timed = TestData.ScenarioTask(scenario.Id, "Timed");
        timed.TriggerCondition = TaskTrigger.Time;
        var manual = TestData.ScenarioTask(scenario.Id, "Manual");
        await Seed(scenario, timed, manual);

        await AssertStatus(HttpStatusCode.OK, await RootClient.PutAsync($"api/scenarios/{scenario.Id}/start", null, Ct));

        await using var db = NewContext();
        var statuses = await db.Tasks.Where(x => x.ScenarioId == scenario.Id).ToDictionaryAsync(x => x.Name, x => x.Status, Ct);
        Assert.Equal((Data.TaskStatus.pending, Data.TaskStatus.none), (statuses["Timed"], statuses["Manual"]));
    }

    [Fact]
    public async Task Start_is_forbidden_for_a_member_holding_only_EditScenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsync($"api/scenarios/{scenario.Id}/start", null, Ct));
        await using var db = NewContext();
        Assert.Equal(ScenarioStatus.ready, (await db.Scenarios.SingleAsync(x => x.Id == scenario.Id, Ct)).Status);
    }

    [Fact]
    public async Task Start_is_forbidden_for_a_caller_holding_ExecuteScenario_only_on_another_scenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().OnNewScenario(ScenarioPermission.ExecuteScenario).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsync($"api/scenarios/{scenario.Id}/start", null, Ct));
    }

    [Fact]
    public async Task Start_is_forbidden_for_a_caller_holding_only_EditScenarios()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsync($"api/scenarios/{scenario.Id}/start", null, Ct));
    }

    /// <summary>Starting an unknown scenario id is answered with a 500.</summary>
    [Fact]
    public async Task Start_answers_an_unknown_scenario_with_a_server_error()
    {
        var error = await AssertJsonError(HttpStatusCode.InternalServerError, await RootClient.PutAsync($"api/scenarios/{Guid.NewGuid()}/start", null, Ct));

        Assert.Equal("Sequence contains no elements.", error.Detail);
    }

    /// <summary>Pause and continue are answered with a 500 for a caller who may execute the scenario.</summary>
    [Theory]
    [InlineData("pause")]
    [InlineData("continue")]
    public async Task Pause_and_continue_answer_an_authorized_caller_with_a_server_error(string operation)
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ExecuteScenario]).SeedAsync();

        var error = await AssertJsonError(HttpStatusCode.InternalServerError, await Client(actor).PutAsync($"api/scenarios/{scenario.Id}/{operation}", null, Ct));

        Assert.Equal("The method or operation is not implemented.", error.Detail);
    }

    [Fact]
    public async Task Pause_is_forbidden_for_a_member_holding_only_EditScenario()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsync($"api/scenarios/{scenario.Id}/pause", null, Ct));
    }

    [Fact]
    public async Task Continue_is_forbidden_for_a_member_holding_only_EditScenario()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsync($"api/scenarios/{scenario.Id}/continue", null, Ct));
    }

    [Fact]
    public async Task End_ends_the_scenario_for_a_member_holding_ExecuteScenario()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ExecuteScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsync($"api/scenarios/{scenario.Id}/end", null, Ct));

        await using var db = NewContext();
        Assert.Equal(ScenarioStatus.ended, (await db.Scenarios.SingleAsync(x => x.Id == scenario.Id, Ct)).Status);
    }

    [Fact]
    public async Task End_is_forbidden_for_a_member_holding_only_EditScenario()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsync($"api/scenarios/{scenario.Id}/end", null, Ct));
        await using var db = NewContext();
        Assert.Equal(ScenarioStatus.active, (await db.Scenarios.SingleAsync(x => x.Id == scenario.Id, Ct)).Status);
    }

    [Fact]
    public async Task Delete_removes_the_scenario_for_a_member_holding_ManageScenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ManageScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/scenarios/{scenario.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.Scenarios.AnyAsync(x => x.Id == scenario.Id, Ct));
    }

    [Fact]
    public async Task Delete_removes_the_scenario_for_a_caller_holding_ManageScenarios()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/scenarios/{scenario.Id}", Ct));
    }

    [Fact]
    public async Task Delete_broadcasts_the_id_to_the_scenarios_group()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);

        await AssertStatus(HttpStatusCode.NoContent, await RootClient.DeleteAsync($"api/scenarios/{scenario.Id}", Ct));

        var sent = Assert.Single(Factory.Hub<EngineHub>().ToGroup(scenario.Id), x => x.Method == EngineMethods.ScenarioDeleted);
        Assert.Equal(scenario.Id, (Guid)sent.Argument);
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_member_holding_only_EditScenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/scenarios/{scenario.Id}", Ct));
        await using var db = NewContext();
        Assert.True(await db.Scenarios.AnyAsync(x => x.Id == scenario.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_ManageScenario_only_on_another_scenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().OnNewScenario(ScenarioPermission.ManageScenario).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/scenarios/{scenario.Id}", Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_only_EditScenarios()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/scenarios/{scenario.Id}", Ct));
    }

    /// <summary>A scenario is linked to a template the caller may not view when the update body names it.</summary>
    [Fact]
    public async Task Update_links_the_scenario_to_a_template_the_caller_may_not_view()
    {
        var scenario = TestData.Scenario("Mine");
        var template = TestData.ScenarioTemplate("Theirs");
        await Seed(scenario, template);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/scenarios/{scenario.Id}", new { name = "Mine", status = "ready", scenarioTemplateId = template.Id }, Ct));

        await using var db = NewContext();
        Assert.Equal(template.Id, (await db.Scenarios.SingleAsync(x => x.Id == scenario.Id, Ct)).ScenarioTemplateId);
    }
}
