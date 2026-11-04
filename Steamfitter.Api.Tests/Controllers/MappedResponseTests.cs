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
using Steamfitter.Api.Tests.Support;
using SAVM = Steamfitter.Api.ViewModels;
using TaskStatus = Steamfitter.Api.Data.TaskStatus;

namespace Steamfitter.Api.Tests.Controllers;

/// <summary>
/// What the mappings between entities and view models preserve, over HTTP: how stored values that are
/// missing or unreadable are returned, which members a write leaves alone, and what a copy carries.
/// </summary>
public class MappedResponseTests(DatabaseFixture fixture, SteamfitterAppFactory factory) : ApiTestBase(fixture, factory)
{
    // ---- Tasks ----------------------------------------------------------------------------------

    /// <summary>
    /// A task's parameters are read from its stored input string: none for an empty one, and an input
    /// string that is not JSON is returned under <c>BadInputString</c>. Tasks have no VM list of their own.
    /// </summary>
    [Fact]
    public async Task A_tasks_parameters_are_read_from_its_input_string()
    {
        var scenario = TestData.Scenario();
        var empty = TestData.ScenarioTask(scenario.Id, "Empty");
        empty.InputString = null;
        var unreadable = TestData.ScenarioTask(scenario.Id, "Unreadable");
        unreadable.InputString = "not json";
        var readable = TestData.ScenarioTask(scenario.Id, "Readable");
        readable.InputString = """{"Moid":"{moid}"}""";
        await Seed(scenario, empty, unreadable, readable);

        var emptyTask = await ReadAsync<SAVM.Task>(await RootClient.GetAsync($"api/tasks/{empty.Id}", Ct));
        var unreadableTask = await ReadAsync<SAVM.Task>(await RootClient.GetAsync($"api/tasks/{unreadable.Id}", Ct));
        var readableTask = await ReadAsync<SAVM.Task>(await RootClient.GetAsync($"api/tasks/{readable.Id}", Ct));

        Assert.Empty(emptyTask.ActionParameters);
        Assert.Equal("not json", unreadableTask.ActionParameters["BadInputString"]);
        Assert.Equal("{moid}", readableTask.ActionParameters["Moid"]);
        Assert.Null(readableTask.VmList);
    }

    /// <summary>A task created without parameters stores them as the JSON <c>null</c>, and is returned with none.</summary>
    [Fact]
    public async Task A_task_created_without_parameters_stores_a_json_null()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);

        var created = await ReadAsync<SAVM.Task>(await RootClient.PostAsJsonAsync("api/tasks",
            new { scenarioId = scenario.Id, name = "No parameters", action = "send_email", actionParameters = (object)null }, Ct));

        Assert.Empty(created.ActionParameters);
        await using var db = NewContext();
        Assert.Equal("null", (await db.Tasks.SingleAsync(x => x.Id == created.Id, Ct)).InputString);
    }

    /// <summary>A task listed as a summary carries the summary's members and nothing else.</summary>
    [Fact]
    public async Task A_task_listed_as_a_summary_carries_only_the_summarys_members()
    {
        var viewId = Guid.NewGuid();
        var scenario = TestData.Scenario(viewId: viewId);
        var task = TestData.ScenarioTask(scenario.Id, "Executable");
        task.UserExecutable = true;
        task.Repeatable = true;
        task.Score = 5;
        task.Status = TaskStatus.succeeded;
        task.TotalStatus = TaskStatus.succeeded;
        task.TotalScore = 7;
        task.TotalScoreEarned = 6;
        task.ApiUrl = "stackstorm";
        task.VmMask = "mask";
        task.ExpirationSeconds = 30;
        await Seed(scenario, task);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewTasks]).SeedAsync();

        var listed = Assert.Single(await ReadAsync<List<SAVM.Task>>(await Client(actor).GetAsync($"api/views/{viewId}/tasks", Ct)));

        Assert.Equal((task.Id, "Executable", "A task", scenario.Id), (listed.Id, listed.Name, listed.Description, listed.ScenarioId));
        Assert.Equal((5, 5, TaskStatus.succeeded), (listed.Score, listed.ScoreEarned, listed.Status));
        Assert.Equal((7, 6, TaskStatus.succeeded), (listed.TotalScore, listed.TotalScoreEarned, listed.TotalStatus));
        Assert.Equal((true, true, true), (listed.UserExecutable, listed.Repeatable, listed.Executable));
        Assert.Equal((null, null, 0, default(TaskAction)), (listed.ApiUrl, listed.VmMask, listed.ExpirationSeconds, listed.Action));
        Assert.Null(listed.ActionParameters);
    }

    /// <summary>A copied task carries every member of the original but its id, location, owner and status.</summary>
    [Fact]
    public async Task A_copied_task_carries_the_originals_members()
    {
        var source = TestData.Scenario("Source");
        var target = TestData.Scenario("Target");
        var task = TestData.ScenarioTask(source.Id, "Original");
        task.Action = TaskAction.guest_process_run;
        task.ApiUrl = "stackstorm";
        task.VmMask = "mask";
        task.InputString = """{"CommandText":"whoami"}""";
        task.ExpectedOutput = "root";
        task.ExpirationSeconds = 30;
        task.DelaySeconds = 2;
        task.IntervalSeconds = 3;
        task.Iterations = 4;
        task.IterationTermination = TaskIterationTermination.UntilSuccess;
        task.CurrentIteration = 1;
        task.Score = 5;
        task.UserExecutable = true;
        task.Repeatable = true;
        task.Status = TaskStatus.succeeded;
        await Seed(source, target, task);

        await AssertStatus(HttpStatusCode.OK, await RootClient.PostAsJsonAsync($"api/tasks/{task.Id}/copy", new { id = target.Id, locationType = "scenario" }, Ct));

        await using var db = NewContext();
        var copy = await db.Tasks.SingleAsync(x => x.ScenarioId == target.Id, Ct);
        Assert.NotEqual(task.Id, copy.Id);
        Assert.Equal(("Original", "A task", TaskAction.guest_process_run, "stackstorm", "mask"), (copy.Name, copy.Description, copy.Action, copy.ApiUrl, copy.VmMask));
        Assert.Equal((task.InputString, "root", 30, 2, 3, 4), (copy.InputString, copy.ExpectedOutput, copy.ExpirationSeconds, copy.DelaySeconds, copy.IntervalSeconds, copy.Iterations));
        Assert.Equal((TaskIterationTermination.UntilSuccess, 1, 5, true, true), (copy.IterationTermination, copy.CurrentIteration, copy.Score, copy.UserExecutable, copy.Repeatable));
        Assert.Equal((TaskStatus.none, Root.Id, (Guid?)null), (copy.Status, copy.UserId.Value, copy.TriggerTaskId));
        Assert.Equal(source.Id, (await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct)).ScenarioId);
    }

    // ---- Results --------------------------------------------------------------------------------

    /// <summary>A result's parameters are read from its input string, and one without any is returned under <c>BadInputString</c>.</summary>
    [Fact]
    public async Task A_results_parameters_are_read_from_its_input_string()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id);
        var readable = TestData.Result(task.Id);
        readable.InputString = """{"Moid":"vm-1"}""";
        var missing = TestData.Result(task.Id);
        missing.InputString = null;
        await Seed(scenario, task, readable, missing);

        var readableResult = await ReadAsync<SAVM.Result>(await RootClient.GetAsync($"api/results/{readable.Id}", Ct));
        var missingResult = await ReadAsync<SAVM.Result>(await RootClient.GetAsync($"api/results/{missing.Id}", Ct));

        Assert.Equal("vm-1", readableResult.ActionParameters["Moid"]);
        Assert.Null(Assert.Single(missingResult.ActionParameters, x => x.Key == "BadInputString").Value);
    }

    // ---- Scenarios and templates ----------------------------------------------------------------

    /// <summary>A scenario read without credentials has an empty list of them; its users are not part of the mapping.</summary>
    [Fact]
    public async Task A_scenario_without_credentials_returns_an_empty_list()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);

        var read = await ReadAsync<SAVM.Scenario>(await RootClient.GetAsync($"api/scenarios/{scenario.Id}", Ct));

        Assert.Empty(read.VmCredentials);
        Assert.Null(read.Users);
    }

    /// <summary>A copied scenario carries the original's members, its template included.</summary>
    [Fact]
    public async Task A_copied_scenario_carries_the_originals_members()
    {
        var template = TestData.ScenarioTemplate();
        var scenario = TestData.Scenario("Original", viewId: null, scenarioTemplateId: template.Id);
        scenario.Description = "Described";
        scenario.View = "A view";
        scenario.StartDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        scenario.EndDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await Seed(template, scenario);

        var copy = await ReadAsync<SAVM.Scenario>(await RootClient.PostAsync($"api/scenarios/{scenario.Id}/copy", null, Ct));

        await using var db = NewContext();
        var stored = await db.Scenarios.SingleAsync(x => x.Id == copy.Id, Ct);
        Assert.Equal((template.Id, "Described", "A view"), (stored.ScenarioTemplateId, stored.Description, stored.View));
        Assert.Equal((scenario.StartDate, scenario.EndDate, true), (stored.StartDate, stored.EndDate, stored.OnDemand));
        Assert.Single(db.ScenarioTemplates.Where(x => x.Id == template.Id));
    }

    /// <summary>A template read without credentials has an empty list of them, and earns no score.</summary>
    [Fact]
    public async Task A_template_without_credentials_returns_an_empty_list()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);

        var read = await ReadAsync<SAVM.ScenarioTemplate>(await RootClient.GetAsync($"api/scenarioTemplates/{template.Id}", Ct));

        Assert.Empty(read.VmCredentials);
        Assert.Equal(0, read.ScoreEarned);
    }

    /// <summary>A copied template carries the original's description and duration.</summary>
    [Fact]
    public async Task A_copied_template_carries_the_originals_members()
    {
        var template = TestData.ScenarioTemplate("Original");
        template.Description = "Described";
        template.DurationHours = 6;
        await Seed(template);

        var copy = await ReadAsync<SAVM.ScenarioTemplate>(await RootClient.PostAsync($"api/scenarioTemplates/{template.Id}/copy", null, Ct));

        await using var db = NewContext();
        var stored = await db.ScenarioTemplates.SingleAsync(x => x.Id == copy.Id, Ct);
        Assert.Equal(("Described", 6), (stored.Description, stored.DurationHours));
    }

    // ---- Users, groups and roles ----------------------------------------------------------------

    [Fact]
    public async Task A_users_role_id_is_returned_hyphenated_and_read_in_any_guid_format()
    {
        var id = Guid.NewGuid();

        var created = await ReadAsync<SAVM.User>(await RootClient.PostAsJsonAsync("api/users",
            new { id, name = "Formatted", roleId = TestData.Roles.Observer.ToString("N").ToUpperInvariant() }, Ct));

        Assert.Equal(TestData.Roles.Observer.ToString(), created.RoleId);
        await using var db = NewContext();
        Assert.Equal(TestData.Roles.Observer, (await db.Users.SingleAsync(x => x.Id == id, Ct)).RoleId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public async Task A_users_role_id_that_is_not_a_guid_is_a_server_error(string roleId)
    {
        var id = Guid.NewGuid();

        await AssertJsonError(HttpStatusCode.InternalServerError, await RootClient.PostAsJsonAsync("api/users", new { id, name = "Bad role", roleId }, Ct));

        await using var db = NewContext();
        Assert.False(await db.Users.AnyAsync(x => x.Id == id, Ct));
    }

    [Fact]
    public async Task An_update_without_a_role_clears_the_users_role()
    {
        var user = TestData.User(name: $"User {Guid.NewGuid():N}", roleId: TestData.Roles.Observer);
        await Seed(user);

        var updated = await ReadAsync<SAVM.User>(await RootClient.PutAsJsonAsync($"api/users/{user.Id}", new { id = user.Id, name = user.Name }, Ct));

        Assert.Null(updated.RoleId);
        await using var db = NewContext();
        Assert.Null((await db.Users.SingleAsync(x => x.Id == user.Id, Ct)).RoleId);
    }

    /// <summary>The view model has no description, so an update keeps the stored one.</summary>
    [Fact]
    public async Task An_update_keeps_the_groups_description()
    {
        var group = TestData.Group($"Group {Guid.NewGuid():N}");
        group.Description = "Kept";
        await Seed(group);

        await AssertStatus(HttpStatusCode.OK, await RootClient.PutAsJsonAsync($"api/groups/{group.Id}", new { id = group.Id, name = "Renamed" }, Ct));

        await using var db = NewContext();
        var stored = await db.Groups.SingleAsync(x => x.Id == group.Id, Ct);
        Assert.Equal(("Renamed", "Kept"), (stored.Name, stored.Description));
    }

    /// <summary>System role permissions are an empty list, never null, and the description is kept on update.</summary>
    [Fact]
    public async Task System_role_permissions_are_an_empty_list_rather_than_null()
    {
        var created = await ReadAsync<SAVM.SystemRole>(await RootClient.PostAsJsonAsync("api/system-roles",
            new { name = $"No permissions {Guid.NewGuid():N}", permissions = (object)null }, Ct));
        var stored = TestData.SystemRole($"Stored {Guid.NewGuid():N}");
        stored.Permissions = null;
        stored.Description = "Kept";
        await Seed(stored);

        var read = await ReadAsync<SAVM.SystemRole>(await RootClient.GetAsync($"api/system-roles/{stored.Id}", Ct));
        var updated = await ReadAsync<SAVM.SystemRole>(await RootClient.PutAsJsonAsync($"api/system-roles/{stored.Id}",
            new { id = stored.Id, name = stored.Name, permissions = (object)null }, Ct));

        Assert.Empty(created.Permissions);
        Assert.Empty(read.Permissions);
        Assert.Empty(updated.Permissions);
        await using var db = NewContext();
        Assert.Empty((await db.SystemRoles.SingleAsync(x => x.Id == created.Id, Ct)).Permissions);
        var saved = await db.SystemRoles.SingleAsync(x => x.Id == stored.Id, Ct);
        Assert.Empty(saved.Permissions);
        Assert.Equal("Kept", saved.Description);
    }

    [Fact]
    public async Task Scenario_and_template_role_permissions_are_an_empty_list_rather_than_null()
    {
        var scenarioRole = TestData.ScenarioRole();
        scenarioRole.Permissions = null;
        var templateRole = TestData.ScenarioTemplateRole();
        templateRole.Permissions = null;
        await Seed(scenarioRole, templateRole);

        var readScenarioRole = await ReadAsync<SAVM.ScenarioRole>(await RootClient.GetAsync($"api/scenario-roles/{scenarioRole.Id}", Ct));
        var readTemplateRole = await ReadAsync<SAVM.ScenarioTemplateRole>(await RootClient.GetAsync($"api/scenarioTemplate-roles/{templateRole.Id}", Ct));

        Assert.Empty(readScenarioRole.Permissions);
        Assert.Empty(readTemplateRole.Permissions);
    }
}
