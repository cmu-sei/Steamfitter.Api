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
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Hubs;
using Steamfitter.Api.Tests.Support;
using SAVM = Steamfitter.Api.ViewModels;
using TaskStatus = Steamfitter.Api.Data.TaskStatus;

namespace Steamfitter.Api.Tests.Controllers;

/// <summary>
/// <c>TaskController</c>: task reads, writes and execution. Execution only queues the task on the
/// in-process <c>TaskExecutionQueue</c> (the hosted runner is removed here; <c>TaskExecutionServiceTests</c>
/// drives it), so these tests read what the request stored.
/// </summary>
public class TaskControllerTests(DatabaseFixture fixture, SteamfitterAppFactory factory)
    : ApiTestBase(fixture, factory)
{
    /// <summary>The title of the <c>ForbiddenException</c> the controllers' permission checks throw, which tells their refusal apart from the service's own 403s.</summary>
    private const string InsufficientPermissions = "Insufficient Permissions";

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task An_unauthenticated_request_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync($"api/tasks/{Guid.NewGuid()}", Ct));
    }

    [Fact]
    public async Task GetByScenarioTemplateId_returns_the_templates_tasks_to_a_member_holding_ViewScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        var other = TestData.ScenarioTemplate("Other");
        var task = TestData.TemplateTask(template.Id);
        await Seed(template, other, task, TestData.TemplateTask(other.Id));
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate]).SeedAsync();

        var tasks = await ReadAsync<List<SAVM.Task>>(await Client(actor).GetAsync($"api/scenarioTemplates/{template.Id}/tasks", Ct));

        Assert.Equal([task.Id], tasks.Select(x => x.Id));
    }

    [Fact]
    public async Task GetByScenarioTemplateId_is_forbidden_for_a_caller_holding_ViewScenarioTemplate_only_on_another_template()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().OnNewScenarioTemplate(ScenarioTemplatePermission.ViewScenarioTemplate).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/scenarioTemplates/{template.Id}/tasks", Ct));
    }

    [Fact]
    public async Task GetByScenarioTemplateId_is_forbidden_for_a_caller_holding_only_ViewScenarios()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/scenarioTemplates/{template.Id}/tasks", Ct));
    }

    [Fact]
    public async Task GetByScenarioTemplateId_returns_the_templates_tasks_to_a_caller_holding_ViewScenarioTemplates()
    {
        var template = TestData.ScenarioTemplate();
        var task = TestData.TemplateTask(template.Id);
        await Seed(template, task);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        var tasks = await ReadAsync<List<SAVM.Task>>(await Client(actor).GetAsync($"api/scenarioTemplates/{template.Id}/tasks", Ct));

        Assert.Equal([task.Id], tasks.Select(x => x.Id));
    }

    [Fact]
    public async Task GetByScenarioId_returns_the_scenarios_tasks_to_a_member_holding_ViewScenario()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        var tasks = await ReadAsync<List<SAVM.Task>>(await Client(actor).GetAsync($"api/scenarios/{scenario.Id}/tasks", Ct));

        Assert.Equal([task.Id], tasks.Select(x => x.Id));
    }

    [Fact]
    public async Task GetByScenarioId_is_forbidden_for_a_member_holding_only_ViewTasks()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewTasks]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/scenarios/{scenario.Id}/tasks", Ct));
    }

    [Fact]
    public async Task GetByScenarioId_is_forbidden_for_a_caller_holding_ViewScenario_only_on_another_scenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().OnNewScenario(ScenarioPermission.ViewScenario).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/scenarios/{scenario.Id}/tasks", Ct));
    }

    [Fact]
    public async Task GetByScenarioId_returns_the_scenarios_tasks_to_a_caller_holding_ViewScenarios()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        var tasks = await ReadAsync<List<SAVM.Task>>(await Client(actor).GetAsync($"api/scenarios/{scenario.Id}/tasks", Ct));

        Assert.Equal([task.Id], tasks.Select(x => x.Id));
    }

    [Fact]
    public async Task GetByScenarioId_is_forbidden_for_a_caller_holding_only_ViewScenarioTemplates()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario, TestData.ScenarioTask(scenario.Id));
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/scenarios/{scenario.Id}/tasks", Ct));
    }

    [Fact]
    public async Task GetByViewId_gives_a_member_holding_only_ViewTasks_the_user_executable_tasks_as_summaries()
    {
        var viewId = Guid.NewGuid();
        var scenario = TestData.Scenario(viewId: viewId);
        var executable = TestData.ScenarioTask(scenario.Id, "Executable");
        executable.UserExecutable = true;
        executable.InputString = """{"Secret":"value"}""";
        await Seed(scenario, executable, TestData.ScenarioTask(scenario.Id, "Hidden"));
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewTasks]).SeedAsync();

        var tasks = await ReadAsync<List<SAVM.Task>>(await Client(actor).GetAsync($"api/views/{viewId}/tasks", Ct));

        var listed = Assert.Single(tasks);
        Assert.Equal((executable.Id, null), (listed.Id, listed.ActionParameters));
    }

    [Fact]
    public async Task GetByViewId_gives_a_member_holding_ViewTasks_and_ViewScenario_the_tasks_with_their_parameters()
    {
        var viewId = Guid.NewGuid();
        var scenario = TestData.Scenario(viewId: viewId);
        var executable = TestData.ScenarioTask(scenario.Id, "Executable");
        executable.UserExecutable = true;
        executable.InputString = """{"Secret":"value"}""";
        await Seed(scenario, executable);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewTasks, ScenarioPermission.ViewScenario]).SeedAsync();

        var tasks = await ReadAsync<List<SAVM.Task>>(await Client(actor).GetAsync($"api/views/{viewId}/tasks", Ct));

        Assert.Equal("value", Assert.Single(tasks).ActionParameters["Secret"]);
    }

    [Fact]
    public async Task GetByViewId_is_forbidden_for_a_member_holding_only_ViewScenario()
    {
        var viewId = Guid.NewGuid();
        var scenario = TestData.Scenario(viewId: viewId);
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/views/{viewId}/tasks", Ct));
    }

    [Fact]
    public async Task GetByViewId_is_forbidden_for_a_caller_holding_ViewTasks_only_on_a_scenario_of_another_view()
    {
        var viewId = Guid.NewGuid();
        var scenario = TestData.Scenario(viewId: viewId);
        var executable = TestData.ScenarioTask(scenario.Id, "Executable");
        executable.UserExecutable = true;
        var elsewhere = TestData.Scenario("Elsewhere", viewId: Guid.NewGuid());
        await Seed(scenario, executable, elsewhere);
        var actor = await Actor().OnScenario(elsewhere.Id, permissions: [ScenarioPermission.ViewTasks]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/views/{viewId}/tasks", Ct));
    }

    [Fact]
    public async Task GetByViewId_gives_a_caller_holding_ViewScenarios_the_tasks_with_their_parameters()
    {
        var viewId = Guid.NewGuid();
        var scenario = TestData.Scenario(viewId: viewId);
        var executable = TestData.ScenarioTask(scenario.Id, "Executable");
        executable.UserExecutable = true;
        executable.InputString = """{"Secret":"value"}""";
        await Seed(scenario, executable);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        var tasks = await ReadAsync<List<SAVM.Task>>(await Client(actor).GetAsync($"api/views/{viewId}/tasks", Ct));

        Assert.Equal("value", Assert.Single(tasks).ActionParameters["Secret"]);
    }

    [Fact]
    public async Task GetByViewId_is_forbidden_for_a_caller_holding_only_ViewScenarioTemplates()
    {
        var viewId = Guid.NewGuid();
        var scenario = TestData.Scenario(viewId: viewId);
        await Seed(scenario);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/views/{viewId}/tasks", Ct));
    }

    [Fact]
    public async Task Get_returns_a_scenario_task_to_a_member_holding_ViewScenario()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id, "Readable");
        await Seed(scenario, task);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        var read = await ReadAsync<SAVM.Task>(await Client(actor).GetAsync($"api/tasks/{task.Id}", Ct));

        Assert.Equal("Readable", read.Name);
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_ViewScenario_only_on_another_scenario()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);
        var actor = await Actor().OnNewScenario(ScenarioPermission.ViewScenario).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/tasks/{task.Id}", Ct));
    }

    [Fact]
    public async Task Get_returns_a_scenario_task_to_a_caller_holding_ViewScenarios()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id, "Readable");
        await Seed(scenario, task);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        var read = await ReadAsync<SAVM.Task>(await Client(actor).GetAsync($"api/tasks/{task.Id}", Ct));

        Assert.Equal("Readable", read.Name);
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_only_ViewScenarioTemplates()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/tasks/{task.Id}", Ct));
    }

    [Fact]
    public async Task Get_of_a_template_task_is_forbidden_for_a_member_of_its_template_without_ViewScenarios()
    {
        var template = TestData.ScenarioTemplate();
        var task = TestData.TemplateTask(template.Id);
        await Seed(template, task);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/tasks/{task.Id}", Ct));
    }

    [Fact]
    public async Task Get_of_a_task_that_is_not_there_is_not_found_for_a_member_of_a_scenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.NotFound, await Client(actor).GetAsync($"api/tasks/{Guid.NewGuid()}", Ct));
    }

    [Fact]
    public async Task Create_persists_a_scenario_task_for_a_member_holding_ManageScenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ManageScenario]).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync("api/tasks", TaskForm("Created", scenarioId: scenario.Id), Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var created = await ReadAsync<SAVM.Task>(response);
        await using var db = NewContext();
        var stored = await db.Tasks.SingleAsync(x => x.Id == created.Id, Ct);
        Assert.Equal(("Created", scenario.Id, actor.Id, """{"Subject":"hello"}"""), (stored.Name, stored.ScenarioId.Value, stored.UserId.Value, stored.InputString));
    }

    /// <summary>The scenario's own group gets the summary, the administrators the full task.</summary>
    [Fact]
    public async Task Create_broadcasts_a_scenario_task_to_its_scenario_and_the_scenario_administrators()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);

        var created = await ReadAsync<SAVM.Task>(await RootClient.PostAsJsonAsync("api/tasks", TaskForm("Broadcast", scenarioId: scenario.Id), Ct));

        var toScenario = Assert.Single(Factory.Hub<EngineHub>().ToGroup(scenario.Id), x => x.Method == EngineMethods.TaskCreated);
        var summary = Assert.IsType<SAVM.Task>(toScenario.Argument);
        Assert.Equal((created.Id, null), (summary.Id, summary.ActionParameters));
        Assert.Contains(Factory.Hub<EngineHub>().ToGroup(EngineHub.SCENARIO_GROUP),
            x => x.Method == EngineMethods.TaskCreated && ((SAVM.Task)x.Argument).Id == created.Id && ((SAVM.Task)x.Argument).ActionParameters["Subject"] == "hello");
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_member_holding_only_EditScenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/tasks", TaskForm("Refused", scenarioId: scenario.Id), Ct));
        await using var db = NewContext();
        Assert.False(await db.Tasks.AnyAsync(x => x.Name == "Refused", Ct));
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_ManageScenario_only_on_another_scenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().OnNewScenario(ScenarioPermission.ManageScenario).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/tasks", TaskForm("Refused", scenarioId: scenario.Id), Ct));
    }

    [Fact]
    public async Task Create_of_a_scenario_task_is_allowed_for_a_caller_holding_ManageScenarios()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.Created, await Client(actor).PostAsJsonAsync("api/tasks", TaskForm("Allowed", scenarioId: scenario.Id), Ct));
    }

    [Fact]
    public async Task Create_of_a_scenario_task_is_forbidden_for_a_caller_holding_only_ManageScenarioTemplates()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/tasks", TaskForm("Refused", scenarioId: scenario.Id), Ct));

        await using var db = NewContext();
        Assert.False(await db.Tasks.AnyAsync(x => x.ScenarioId == scenario.Id, Ct));
    }

    [Fact]
    public async Task Create_persists_a_template_task_for_a_member_holding_ManageScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ManageScenarioTemplate]).SeedAsync();

        var created = await ReadAsync<SAVM.Task>(await Client(actor).PostAsJsonAsync("api/tasks", TaskForm("Template Task", scenarioTemplateId: template.Id), Ct));

        await using var db = NewContext();
        Assert.Equal(template.Id, (await db.Tasks.SingleAsync(x => x.Id == created.Id, Ct)).ScenarioTemplateId);
        Assert.Contains(Factory.Hub<EngineHub>().ToGroup(template.Id), x => x.Method == EngineMethods.TaskCreated);
    }

    [Fact]
    public async Task Create_of_a_template_task_is_forbidden_for_a_member_holding_only_EditScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/tasks", TaskForm("Refused", scenarioTemplateId: template.Id), Ct));
    }

    [Fact]
    public async Task Create_persists_a_template_task_for_a_caller_holding_ManageScenarioTemplates()
    {
        // Gate: Steamfitter.Api/Controllers/TaskController.cs#TaskController.Create:2
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageScenarioTemplates).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync("api/tasks", TaskForm("System Template Task", scenarioTemplateId: template.Id), Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var created = await ReadAsync<SAVM.Task>(response);
        await using var db = NewContext();
        Assert.Equal(template.Id, (await db.Tasks.SingleAsync(x => x.Id == created.Id, Ct)).ScenarioTemplateId);
    }

    [Fact]
    public async Task Create_of_a_template_task_is_forbidden_for_a_caller_holding_only_EditScenarioTemplates()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/tasks", TaskForm("Refused", scenarioTemplateId: template.Id), Ct));
        await using var db = NewContext();
        Assert.False(await db.Tasks.AnyAsync(x => x.ScenarioTemplateId == template.Id, Ct));
    }

    [Fact]
    public async Task Create_of_a_task_with_neither_a_scenario_nor_a_template_is_forbidden_even_for_an_administrator()
    {
        var name = $"Orphan {Guid.NewGuid()}";

        await AssertStatus(HttpStatusCode.Forbidden, await RootClient.PostAsJsonAsync("api/tasks", TaskForm(name), Ct));

        await using var db = NewContext();
        Assert.False(await db.Tasks.AnyAsync(x => x.Name == name, Ct));
    }

    [Fact]
    public async Task Create_of_a_task_with_both_a_scenario_and_a_template_is_forbidden()
    {
        var scenario = TestData.Scenario();
        var template = TestData.ScenarioTemplate();
        await Seed(scenario, template);
        var actor = await Actor()
            .OnScenario(scenario.Id, permissions: [ScenarioPermission.ManageScenario])
            .OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ManageScenarioTemplate])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/tasks", TaskForm("Both", scenarioId: scenario.Id, scenarioTemplateId: template.Id), Ct));

        await using var db = NewContext();
        Assert.False(await db.Tasks.AnyAsync(x => x.ScenarioId == scenario.Id || x.ScenarioTemplateId == template.Id, Ct));
    }

    [Fact]
    public async Task Create_under_a_trigger_task_of_another_scenario_is_not_found()
    {
        var mine = TestData.Scenario("Mine");
        var theirs = TestData.Scenario("Theirs");
        var theirStart = TestData.ScenarioTask(theirs.Id, "Their start");
        await Seed(mine, theirs, theirStart);
        var actor = await Actor().OnScenario(mine.Id, permissions: [ScenarioPermission.ManageScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.NotFound, await Client(actor).PostAsJsonAsync("api/tasks", TaskForm("Planted", scenarioId: mine.Id, triggerTaskId: theirStart.Id), Ct));

        await using var db = NewContext();
        Assert.False(await db.Tasks.AnyAsync(x => x.TriggerTaskId == theirStart.Id || x.ScenarioId == mine.Id, Ct));
    }

    /// <summary>Blueprint pushes a chain of tasks, each triggered by the one it created before it in the same scenario.</summary>
    [Fact]
    public async Task Create_under_a_trigger_task_of_the_same_scenario_chains_it()
    {
        var scenario = TestData.Scenario();
        var start = TestData.ScenarioTask(scenario.Id, "Start");
        await Seed(scenario, start);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ManageScenario]).SeedAsync();

        var created = await ReadAsync<SAVM.Task>(await Client(actor).PostAsJsonAsync("api/tasks", TaskForm("Next", scenarioId: scenario.Id, triggerTaskId: start.Id), Ct));

        await using var db = NewContext();
        Assert.Equal(start.Id, (await db.Tasks.SingleAsync(x => x.Id == created.Id, Ct)).TriggerTaskId);
    }

    /// <summary>A form with no action parameters is answered with a 500.</summary>
    [Fact]
    public async Task Create_answers_a_form_without_action_parameters_with_a_server_error()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);

        var response = await RootClient.PostAsJsonAsync("api/tasks", new { name = "No parameters", scenarioId = scenario.Id, action = "send_email", apiUrl = "email", vmMask = "" }, Ct);

        var error = await AssertJsonError(HttpStatusCode.InternalServerError, response);
        Assert.Equal("Object reference not set to an instance of an object.", error.Detail);
    }

    [Fact]
    public async Task Copy_copies_a_task_into_another_scenario_for_a_member_who_may_view_the_source_and_edit_the_target()
    {
        var source = TestData.Scenario("Source");
        var target = TestData.Scenario("Target");
        var task = TestData.ScenarioTask(source.Id, "Copied");
        await Seed(source, target, task);
        var actor = await Actor()
            .OnScenario(source.Id, permissions: [ScenarioPermission.ViewScenario])
            .OnScenario(target.Id, permissions: [ScenarioPermission.EditScenario])
            .SeedAsync();

        var response = await Client(actor).PostAsJsonAsync($"api/tasks/{task.Id}/copy", new { id = target.Id, locationType = "scenario" }, Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        await using var db = NewContext();
        Assert.Equal(["Copied"], await db.Tasks.Where(x => x.ScenarioId == target.Id).Select(x => x.Name).ToListAsync(Ct));
    }

    [Fact]
    public async Task Copy_is_forbidden_for_a_member_who_may_edit_the_target_but_holds_ViewScenario_only_on_another_scenario()
    {
        var source = TestData.Scenario("Source");
        var target = TestData.Scenario("Target");
        var task = TestData.ScenarioTask(source.Id);
        await Seed(source, target, task);
        var actor = await Actor()
            .OnNewScenario(ScenarioPermission.ViewScenario)
            .OnScenario(target.Id, permissions: [ScenarioPermission.EditScenario])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/tasks/{task.Id}/copy", new { id = target.Id, locationType = "scenario" }, Ct));
        await using var db = NewContext();
        Assert.False(await db.Tasks.AnyAsync(x => x.ScenarioId == target.Id, Ct));
    }

    [Fact]
    public async Task Copy_is_forbidden_for_a_member_who_may_view_the_source_but_holds_only_ViewScenario_on_the_target()
    {
        var source = TestData.Scenario("Source");
        var target = TestData.Scenario("Target");
        var task = TestData.ScenarioTask(source.Id);
        await Seed(source, target, task);
        var actor = await Actor()
            .OnScenario(source.Id, permissions: [ScenarioPermission.ViewScenario])
            .OnScenario(target.Id, permissions: [ScenarioPermission.ViewScenario])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/tasks/{task.Id}/copy", new { id = target.Id, locationType = "scenario" }, Ct));
    }

    [Fact]
    public async Task Copy_copies_a_task_into_another_scenario_for_a_caller_holding_ViewScenarios_who_may_edit_the_target()
    {
        var source = TestData.Scenario("Source");
        var target = TestData.Scenario("Target");
        var task = TestData.ScenarioTask(source.Id, "Copied");
        await Seed(source, target, task);
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.ViewScenarios)
            .OnScenario(target.Id, permissions: [ScenarioPermission.EditScenario])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PostAsJsonAsync($"api/tasks/{task.Id}/copy", new { id = target.Id, locationType = "scenario" }, Ct));

        await using var db = NewContext();
        Assert.Equal(["Copied"], await db.Tasks.Where(x => x.ScenarioId == target.Id).Select(x => x.Name).ToListAsync(Ct));
    }

    [Fact]
    public async Task Copy_is_forbidden_for_a_member_who_may_edit_the_target_but_holds_only_ViewScenarioTemplates()
    {
        var source = TestData.Scenario("Source");
        var target = TestData.Scenario("Target");
        var task = TestData.ScenarioTask(source.Id);
        await Seed(source, target, task);
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.ViewScenarioTemplates)
            .OnScenario(target.Id, permissions: [ScenarioPermission.EditScenario])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/tasks/{task.Id}/copy", new { id = target.Id, locationType = "scenario" }, Ct));
        await using var db = NewContext();
        Assert.False(await db.Tasks.AnyAsync(x => x.ScenarioId == target.Id, Ct));
    }

    [Fact]
    public async Task Copy_into_a_scenario_is_forbidden_for_a_member_who_may_view_the_source_but_holds_only_EditScenarioTemplates()
    {
        var source = TestData.Scenario("Source");
        var target = TestData.Scenario("Target");
        var task = TestData.ScenarioTask(source.Id);
        await Seed(source, target, task);
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.EditScenarioTemplates)
            .OnScenario(source.Id, permissions: [ScenarioPermission.ViewScenario])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/tasks/{task.Id}/copy", new { id = target.Id, locationType = "scenario" }, Ct));
        await using var db = NewContext();
        Assert.False(await db.Tasks.AnyAsync(x => x.ScenarioId == target.Id, Ct));
    }

    [Fact]
    public async Task CreateFromResult_creates_a_task_from_the_results_command_for_a_member_who_may_view_and_edit()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id);
        var result = TestData.Result(task.Id);
        await Seed(scenario, task, result);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario, ScenarioPermission.EditScenario]).SeedAsync();

        var created = await ReadAsync<SAVM.Task>(await Client(actor).PostAsJsonAsync($"api/tasks/copyfromresult/{result.Id}", new { id = scenario.Id, locationType = "scenario" }, Ct));

        await using var db = NewContext();
        var stored = await db.Tasks.SingleAsync(x => x.Id == created.Id, Ct);
        Assert.Equal(("New Task", scenario.Id, TaskAction.send_email), (stored.Name, stored.ScenarioId.Value, stored.Action));
    }

    [Fact]
    public async Task CreateFromResult_is_forbidden_for_a_member_holding_only_ViewScenario()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id);
        var result = TestData.Result(task.Id);
        await Seed(scenario, task, result);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/tasks/copyfromresult/{result.Id}", new { id = scenario.Id, locationType = "scenario" }, Ct));
    }

    [Fact]
    public async Task CreateFromResult_creates_a_task_in_another_scenario_for_a_caller_holding_ViewScenarios_who_may_edit_the_target()
    {
        var source = TestData.Scenario("Source");
        var target = TestData.Scenario("Target");
        var task = TestData.ScenarioTask(source.Id);
        var result = TestData.Result(task.Id);
        await Seed(source, target, task, result);
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.ViewScenarios)
            .OnScenario(target.Id, permissions: [ScenarioPermission.EditScenario])
            .SeedAsync();

        var created = await ReadAsync<SAVM.Task>(await Client(actor).PostAsJsonAsync($"api/tasks/copyfromresult/{result.Id}", new { id = target.Id, locationType = "scenario" }, Ct));

        await using var db = NewContext();
        Assert.Equal(target.Id, (await db.Tasks.SingleAsync(x => x.Id == created.Id, Ct)).ScenarioId);
    }

    [Fact]
    public async Task CreateFromResult_is_forbidden_for_a_member_who_may_edit_the_target_but_holds_only_ViewScenarioTemplates()
    {
        var source = TestData.Scenario("Source");
        var target = TestData.Scenario("Target");
        var task = TestData.ScenarioTask(source.Id);
        var result = TestData.Result(task.Id);
        await Seed(source, target, task, result);
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.ViewScenarioTemplates)
            .OnScenario(target.Id, permissions: [ScenarioPermission.EditScenario])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/tasks/copyfromresult/{result.Id}", new { id = target.Id, locationType = "scenario" }, Ct));
        await using var db = NewContext();
        Assert.False(await db.Tasks.AnyAsync(x => x.ScenarioId == target.Id, Ct));
    }

    [Fact]
    public async Task CreateFromResult_is_forbidden_for_a_member_who_may_edit_the_target_but_holds_ViewScenario_only_on_another_scenario()
    {
        var source = TestData.Scenario("Source");
        var target = TestData.Scenario("Target");
        var task = TestData.ScenarioTask(source.Id);
        var result = TestData.Result(task.Id);
        await Seed(source, target, task, result);
        var actor = await Actor()
            .OnNewScenario(ScenarioPermission.ViewScenario)
            .OnScenario(target.Id, permissions: [ScenarioPermission.EditScenario])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/tasks/copyfromresult/{result.Id}", new { id = target.Id, locationType = "scenario" }, Ct));
        await using var db = NewContext();
        Assert.False(await db.Tasks.AnyAsync(x => x.ScenarioId == target.Id, Ct));
    }

    [Fact]
    public async Task CreateAndExecute_creates_the_task_and_answers_with_no_results_for_a_member_holding_EditScenario_and_ExecuteScenario()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario, ScenarioPermission.ExecuteScenario]).SeedAsync();

        var results = await ReadAsync<List<SAVM.Result>>(await Client(actor).PostAsJsonAsync("api/tasks/execute", TaskForm("Run now", scenarioId: scenario.Id), Ct));

        Assert.Empty(results);
        await using var db = NewContext();
        Assert.Equal(TaskStatus.pending, (await db.Tasks.SingleAsync(x => x.Name == "Run now", Ct)).Status);
    }

    [Fact]
    public async Task CreateAndExecute_is_forbidden_for_a_member_holding_only_ExecuteScenario()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ExecuteScenario]).SeedAsync();

        var error = await AssertJsonError(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/tasks/execute", TaskForm("Refused", scenarioId: scenario.Id), Ct));

        Assert.Equal(InsufficientPermissions, error.Title);
    }

    [Fact]
    public async Task CreateAndExecute_is_forbidden_for_a_member_holding_only_EditScenario()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        var error = await AssertJsonError(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/tasks/execute", TaskForm("Refused", scenarioId: scenario.Id), Ct));

        Assert.Equal(InsufficientPermissions, error.Title);
        await using var db = NewContext();
        Assert.False(await db.Tasks.AnyAsync(x => x.Name == "Refused", Ct));
    }

    [Fact]
    public async Task CreateAndExecute_creates_the_task_for_a_member_holding_EditScenario_and_the_system_ExecuteScenarios()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        await Seed(scenario);
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.ExecuteScenarios)
            .OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario])
            .SeedAsync();

        var results = await ReadAsync<List<SAVM.Result>>(await Client(actor).PostAsJsonAsync("api/tasks/execute", TaskForm("Run by system", scenarioId: scenario.Id), Ct));

        Assert.Empty(results);
        await using var db = NewContext();
        Assert.Equal(TaskStatus.pending, (await db.Tasks.SingleAsync(x => x.Name == "Run by system", Ct)).Status);
    }

    [Fact]
    public async Task CreateAndExecute_is_forbidden_for_a_caller_holding_only_EditScenarios()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        await Seed(scenario);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarios).SeedAsync();

        var error = await AssertJsonError(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/tasks/execute", TaskForm("Refused", scenarioId: scenario.Id), Ct));

        Assert.Equal(InsufficientPermissions, error.Title);
        await using var db = NewContext();
        Assert.False(await db.Tasks.AnyAsync(x => x.ScenarioId == scenario.Id, Ct));
    }

    [Fact]
    public async Task CreateAndExecute_of_a_form_without_a_scenario_is_forbidden()
    {
        await AssertStatus(HttpStatusCode.Forbidden, await RootClient.PostAsJsonAsync("api/tasks/execute", TaskForm("No scenario"), Ct));
    }

    [Fact]
    public async Task Execute_resets_the_task_to_pending_for_a_member_holding_ExecuteScenario()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ExecuteScenario]).SeedAsync();

        var results = await ReadAsync<List<SAVM.Result>>(await Client(actor).PostAsync($"api/tasks/{task.Id}/execute", null, Ct));

        Assert.Empty(results);
        await using var db = NewContext();
        var stored = await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct);
        Assert.Equal((TaskStatus.pending, actor.Id), (stored.Status, stored.UserId.Value));
    }

    [Fact]
    public async Task Execute_is_forbidden_for_a_member_holding_only_EditScenario()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        var error = await AssertJsonError(HttpStatusCode.Forbidden, await Client(actor).PostAsync($"api/tasks/{task.Id}/execute", null, Ct));

        Assert.Equal(InsufficientPermissions, error.Title);
        await using var db = NewContext();
        Assert.Equal(TaskStatus.none, (await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct)).Status);
    }

    [Fact]
    public async Task Execute_is_forbidden_for_a_caller_holding_ExecuteScenario_only_on_another_scenario()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);
        var actor = await Actor().OnNewScenario(ScenarioPermission.ExecuteScenario).SeedAsync();

        var error = await AssertJsonError(HttpStatusCode.Forbidden, await Client(actor).PostAsync($"api/tasks/{task.Id}/execute", null, Ct));

        Assert.Equal(InsufficientPermissions, error.Title);
    }

    [Fact]
    public async Task Execute_is_forbidden_for_a_caller_holding_only_EditScenarios()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarios).SeedAsync();

        var error = await AssertJsonError(HttpStatusCode.Forbidden, await Client(actor).PostAsync($"api/tasks/{task.Id}/execute", null, Ct));

        Assert.Equal(InsufficientPermissions, error.Title);
    }

    [Fact]
    public async Task Execute_of_a_task_that_succeeded_and_is_not_repeatable_is_forbidden()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        var task = TestData.ScenarioTask(scenario.Id);
        task.Status = TaskStatus.succeeded;
        task.TotalStatus = TaskStatus.succeeded;
        await Seed(scenario, task);

        var error = await AssertJsonError(HttpStatusCode.Forbidden, await RootClient.PostAsync($"api/tasks/{task.Id}/execute", null, Ct));

        Assert.Equal("Task cannot be executed in it's current state", error.Title);
    }

    [Fact]
    public async Task ExecuteWithSubstitutions_answers_with_the_task_id_for_a_member_holding_ExecuteScenario()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        var task = TestData.ScenarioTask(scenario.Id);
        task.InputString = """{"Subject":"{who}"}""";
        await Seed(scenario, task);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ExecuteScenario]).SeedAsync();

        var id = await ReadAsync<Guid>(await Client(actor).PostAsJsonAsync($"api/tasks/{task.Id}/execute/substitutions", new Dictionary<string, string> { ["who"] = "world" }, Ct));

        Assert.Equal(task.Id, id);
        await using var db = NewContext();
        var stored = await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct);
        Assert.Equal((TaskStatus.pending, actor.Id), (stored.Status, stored.UserId.Value));
    }

    [Theory]
    [InlineData("moid")]
    [InlineData("Moid")]
    [InlineData("VmName")]
    [InlineData("vmname")]
    public async Task ExecuteWithSubstitutions_of_the_vm_target_is_forbidden_and_leaves_the_task_alone(string key)
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        var task = TestData.ScenarioTask(scenario.Id);
        task.InputString = """{"Moid":"{moid}","VmName":"{VmName}"}""";
        await Seed(scenario, task);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ExecuteScenario]).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync(
            $"api/tasks/{task.Id}/execute/substitutions", new Dictionary<string, string> { [key] = Guid.NewGuid().ToString() }, Ct);

        await AssertStatus(HttpStatusCode.Forbidden, response);
        await using var db = NewContext();
        Assert.Equal(TaskStatus.none, (await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct)).Status);
    }

    [Fact]
    public async Task ExecuteForGrade_with_a_substitution_of_the_vm_target_is_forbidden_and_leaves_the_task_alone()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        var start = TestData.ScenarioTask(scenario.Id, "Start");
        var graded = TestData.ScenarioTask(scenario.Id, "Graded");
        await Seed(scenario, start, graded);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ExecuteScenario]).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync(
            "api/tasks/execute/graded",
            new { scenarioId = scenario.Id, startTaskName = "Start", gradedTaskName = "Graded", taskSubstitutions = new Dictionary<string, string> { ["moid"] = Guid.NewGuid().ToString() } },
            Ct);

        await AssertStatus(HttpStatusCode.Forbidden, response);
        await using var db = NewContext();
        Assert.Equal(TaskStatus.none, (await db.Tasks.SingleAsync(x => x.Id == start.Id, Ct)).Status);
    }

    [Fact]
    public async Task ExecuteWithSubstitutions_is_forbidden_for_a_member_holding_only_EditScenario()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        var error = await AssertJsonError(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/tasks/{task.Id}/execute/substitutions", new Dictionary<string, string>(), Ct));

        Assert.Equal(InsufficientPermissions, error.Title);
    }

    [Fact]
    public async Task ExecuteWithSubstitutions_is_forbidden_for_a_caller_holding_only_EditScenarios()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarios).SeedAsync();

        var error = await AssertJsonError(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/tasks/{task.Id}/execute/substitutions", new Dictionary<string, string>(), Ct));

        Assert.Equal(InsufficientPermissions, error.Title);
        await using var db = NewContext();
        Assert.Equal(TaskStatus.none, (await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct)).Status);
    }

    [Fact]
    public async Task ExecuteForGrade_answers_with_the_graded_task_for_a_member_holding_ExecuteScenario()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        var start = TestData.ScenarioTask(scenario.Id, "Start");
        var graded = TestData.ScenarioTask(scenario.Id, "Graded");
        await Seed(scenario, start, graded);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ExecuteScenario]).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync(
            "api/tasks/execute/graded",
            new { scenarioId = scenario.Id, startTaskName = "Start", gradedTaskName = "Graded", taskSubstitutions = new Dictionary<string, string>() },
            Ct);

        Assert.Equal(graded.Id, (await ReadAsync<GradeCheck>(response)).GradedTaskId);
    }

    [Fact]
    public async Task ExecuteForGrade_is_forbidden_for_a_member_holding_only_EditScenario()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        await Seed(scenario, TestData.ScenarioTask(scenario.Id, "Start"), TestData.ScenarioTask(scenario.Id, "Graded"));
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        var error = await AssertJsonError(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync(
            "api/tasks/execute/graded",
            new { scenarioId = scenario.Id, startTaskName = "Start", gradedTaskName = "Graded", taskSubstitutions = new Dictionary<string, string>() },
            Ct));

        Assert.Equal(InsufficientPermissions, error.Title);
    }

    [Fact]
    public async Task ExecuteForGrade_answers_with_the_graded_task_for_a_caller_holding_ExecuteScenarios()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        var start = TestData.ScenarioTask(scenario.Id, "Start");
        var graded = TestData.ScenarioTask(scenario.Id, "Graded");
        await Seed(scenario, start, graded);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ExecuteScenarios).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync(
            "api/tasks/execute/graded",
            new { scenarioId = scenario.Id, startTaskName = "Start", gradedTaskName = "Graded", taskSubstitutions = new Dictionary<string, string>() },
            Ct);

        Assert.Equal(graded.Id, (await ReadAsync<GradeCheck>(response)).GradedTaskId);
    }

    [Fact]
    public async Task ExecuteForGrade_is_forbidden_for_a_caller_holding_only_EditScenarios()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        await Seed(scenario, TestData.ScenarioTask(scenario.Id, "Start"), TestData.ScenarioTask(scenario.Id, "Graded"));
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarios).SeedAsync();

        var error = await AssertJsonError(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync(
            "api/tasks/execute/graded",
            new { scenarioId = scenario.Id, startTaskName = "Start", gradedTaskName = "Graded", taskSubstitutions = new Dictionary<string, string>() },
            Ct));

        Assert.Equal(InsufficientPermissions, error.Title);
    }

    [Fact]
    public async Task Update_saves_a_scenario_task_for_a_member_holding_EditScenario()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id, "Before");
        await Seed(scenario, task);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}", TaskForm("After", scenarioId: scenario.Id), Ct));

        await using var db = NewContext();
        var stored = await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct);
        Assert.Equal(("After", actor.Id), (stored.Name, stored.ModifiedBy.Value));
    }

    [Fact]
    public async Task Update_broadcasts_the_summary_to_the_tasks_scenario()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id, "Before");
        await Seed(scenario, task);

        await AssertStatus(HttpStatusCode.OK, await RootClient.PutAsJsonAsync($"api/tasks/{task.Id}", TaskForm("After", scenarioId: scenario.Id), Ct));

        var sent = Assert.Single(Factory.Hub<EngineHub>().ToGroup(scenario.Id), x => x.Method == EngineMethods.TaskUpdated);
        Assert.Equal("After", Assert.IsType<SAVM.Task>(sent.Argument).Name);
    }

    [Fact]
    public async Task Update_saves_a_template_task_for_a_member_holding_EditScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        var task = TestData.TemplateTask(template.Id, "Before");
        await Seed(template, task);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}", TaskForm("After", scenarioTemplateId: template.Id), Ct));
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_member_holding_only_ViewScenario()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id, "Before");
        await Seed(scenario, task);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}", TaskForm("After", scenarioId: scenario.Id), Ct));
        await using var db = NewContext();
        Assert.Equal("Before", (await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_EditScenario_only_on_another_scenario()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id, "Before");
        await Seed(scenario, task);
        var actor = await Actor().OnNewScenario(ScenarioPermission.EditScenario).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}", TaskForm("After", scenarioId: scenario.Id), Ct));
    }

    [Fact]
    public async Task Update_of_a_template_task_is_forbidden_for_a_member_holding_only_ViewScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        var task = TestData.TemplateTask(template.Id, "Before");
        await Seed(template, task);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}", TaskForm("After", scenarioTemplateId: template.Id), Ct));
    }

    [Fact]
    public async Task Update_saves_a_template_task_for_a_caller_holding_EditScenarioTemplates()
    {
        var template = TestData.ScenarioTemplate();
        var task = TestData.TemplateTask(template.Id, "Before");
        await Seed(template, task);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}", TaskForm("After", scenarioTemplateId: template.Id), Ct));

        await using var db = NewContext();
        Assert.Equal("After", (await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_of_a_template_task_is_forbidden_for_a_caller_holding_only_EditScenarios()
    {
        var template = TestData.ScenarioTemplate();
        var task = TestData.TemplateTask(template.Id, "Before");
        await Seed(template, task);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}", TaskForm("After", scenarioTemplateId: template.Id), Ct));
        await using var db = NewContext();
        Assert.Equal("Before", (await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct)).Name);
    }

    [Fact]
    public async Task Move_moves_the_task_and_its_subtasks_for_a_member_who_may_edit_both_scenarios()
    {
        var source = TestData.Scenario("Source");
        var target = TestData.Scenario("Target");
        var task = TestData.ScenarioTask(source.Id, "Parent");
        var child = TestData.ScenarioTask(source.Id, "Child", triggerTaskId: task.Id);
        await Seed(source, target, task, child);
        var actor = await Actor()
            .OnScenario(source.Id, permissions: [ScenarioPermission.EditScenario])
            .OnScenario(target.Id, permissions: [ScenarioPermission.EditScenario])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}/move", new { id = target.Id, locationType = "scenario" }, Ct));

        await using var db = NewContext();
        Assert.Equal(["Child", "Parent"], await db.Tasks.Where(x => x.ScenarioId == target.Id).Select(x => x.Name).OrderBy(x => x).ToListAsync(Ct));
    }

    [Fact]
    public async Task Move_leaves_a_subtask_of_another_scenario_where_it_is()
    {
        var source = TestData.Scenario("Source");
        var target = TestData.Scenario("Target");
        var theirs = TestData.Scenario("Theirs");
        var task = TestData.ScenarioTask(source.Id, "Parent");
        var foreign = TestData.ScenarioTask(theirs.Id, "Foreign child", triggerTaskId: task.Id);
        await Seed(source, target, theirs, task, foreign);
        var actor = await Actor()
            .OnScenario(source.Id, permissions: [ScenarioPermission.EditScenario])
            .OnScenario(target.Id, permissions: [ScenarioPermission.EditScenario])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}/move", new { id = target.Id, locationType = "scenario" }, Ct));

        await using var db = NewContext();
        Assert.Equal(theirs.Id, (await db.Tasks.SingleAsync(x => x.Id == foreign.Id, Ct)).ScenarioId);
    }

    [Fact]
    public async Task Copy_copies_only_the_subtasks_of_the_source_scenario()
    {
        var source = TestData.Scenario("Source");
        var target = TestData.Scenario("Target");
        var theirs = TestData.Scenario("Theirs");
        var task = TestData.ScenarioTask(source.Id, "Parent");
        var child = TestData.ScenarioTask(source.Id, "Child", triggerTaskId: task.Id);
        var foreign = TestData.ScenarioTask(theirs.Id, "Foreign child", triggerTaskId: task.Id);
        await Seed(source, target, theirs, task, child, foreign);
        var actor = await Actor()
            .OnScenario(source.Id, permissions: [ScenarioPermission.ViewScenario])
            .OnScenario(target.Id, permissions: [ScenarioPermission.EditScenario])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PostAsJsonAsync($"api/tasks/{task.Id}/copy", new { id = target.Id, locationType = "scenario" }, Ct));

        await using var db = NewContext();
        Assert.Equal(["Child", "Parent"], await db.Tasks.Where(x => x.ScenarioId == target.Id).Select(x => x.Name).OrderBy(x => x).ToListAsync(Ct));
    }

    [Fact]
    public async Task Move_is_forbidden_for_a_member_who_may_edit_the_target_but_holds_only_ViewScenario_on_the_source()
    {
        var source = TestData.Scenario("Source");
        var target = TestData.Scenario("Target");
        var task = TestData.ScenarioTask(source.Id);
        await Seed(source, target, task);
        var actor = await Actor()
            .OnScenario(source.Id, permissions: [ScenarioPermission.ViewScenario])
            .OnScenario(target.Id, permissions: [ScenarioPermission.EditScenario])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}/move", new { id = target.Id, locationType = "scenario" }, Ct));
        await using var db = NewContext();
        Assert.Equal(source.Id, (await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct)).ScenarioId);
    }

    [Fact]
    public async Task Move_is_forbidden_for_a_member_who_may_edit_the_source_but_holds_only_ViewScenario_on_the_target()
    {
        var source = TestData.Scenario("Source");
        var target = TestData.Scenario("Target");
        var task = TestData.ScenarioTask(source.Id);
        await Seed(source, target, task);
        var actor = await Actor()
            .OnScenario(source.Id, permissions: [ScenarioPermission.EditScenario])
            .OnScenario(target.Id, permissions: [ScenarioPermission.ViewScenario])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}/move", new { id = target.Id, locationType = "scenario" }, Ct));
    }

    [Fact]
    public async Task Delete_removes_the_task_for_a_member_holding_EditScenario()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/tasks/{task.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.Tasks.AnyAsync(x => x.Id == task.Id, Ct));
    }

    [Fact]
    public async Task Delete_broadcasts_the_id_to_the_tasks_scenario_and_the_scenario_administrators()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);

        await AssertStatus(HttpStatusCode.NoContent, await RootClient.DeleteAsync($"api/tasks/{task.Id}", Ct));

        Assert.Equal(task.Id, (Guid)Assert.Single(Factory.Hub<EngineHub>().ToGroup(scenario.Id), x => x.Method == EngineMethods.TaskDeleted).Argument);
        Assert.Contains(Factory.Hub<EngineHub>().ToGroup(EngineHub.SCENARIO_GROUP), x => x.Method == EngineMethods.TaskDeleted && (Guid)x.Argument == task.Id);
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_member_holding_only_ViewScenario()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/tasks/{task.Id}", Ct));
        await using var db = NewContext();
        Assert.True(await db.Tasks.AnyAsync(x => x.Id == task.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_EditScenario_only_on_another_scenario()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);
        var actor = await Actor().OnNewScenario(ScenarioPermission.EditScenario).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/tasks/{task.Id}", Ct));
    }

    [Fact]
    public async Task GetAvailableCommands_serves_the_action_catalog_to_a_caller_holding_no_permission()
    {
        var actor = await Actor().SeedAsync();

        var response = await Client(actor).GetAsync("api/tasks/commands", Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        Assert.Contains("send_email", await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task Execute_resets_the_task_to_pending_for_a_caller_holding_ExecuteScenarios()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ExecuteScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PostAsync($"api/tasks/{task.Id}/execute", null, Ct));

        await using var db = NewContext();
        Assert.Equal(TaskStatus.pending, (await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct)).Status);
    }

    [Fact]
    public async Task ExecuteWithSubstitutions_resets_the_task_to_pending_for_a_caller_holding_ExecuteScenarios()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ExecuteScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PostAsJsonAsync($"api/tasks/{task.Id}/execute/substitutions", new Dictionary<string, string>(), Ct));

        await using var db = NewContext();
        Assert.Equal(TaskStatus.pending, (await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct)).Status);
    }

    [Fact]
    public async Task Update_saves_a_scenario_task_for_a_caller_holding_EditScenarios()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id, "Before");
        await Seed(scenario, task);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}", TaskForm("After", scenarioId: scenario.Id), Ct));

        await using var db = NewContext();
        Assert.Equal("After", (await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_only_EditScenarioTemplates()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id, "Before");
        await Seed(scenario, task);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}", TaskForm("After", scenarioId: scenario.Id), Ct));
    }

    [Fact]
    public async Task Delete_removes_the_task_for_a_caller_holding_EditScenarios()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/tasks/{task.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.Tasks.AnyAsync(x => x.Id == task.Id, Ct));
    }

    [Fact]
    public async Task Move_moves_the_task_for_a_caller_holding_EditScenarios()
    {
        var source = TestData.Scenario("Source");
        var target = TestData.Scenario("Target");
        var task = TestData.ScenarioTask(source.Id);
        await Seed(source, target, task);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}/move", new { id = target.Id, locationType = "scenario" }, Ct));

        await using var db = NewContext();
        Assert.Equal(target.Id, (await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct)).ScenarioId);
    }

    [Fact]
    public async Task Move_moves_a_template_task_into_another_template_for_a_member_who_may_edit_both()
    {
        var source = TestData.ScenarioTemplate("Source");
        var target = TestData.ScenarioTemplate("Target");
        var task = TestData.TemplateTask(source.Id);
        await Seed(source, target, task);
        var actor = await Actor()
            .OnScenarioTemplate(source.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate])
            .OnScenarioTemplate(target.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}/move", new { id = target.Id, locationType = "scenarioTemplate" }, Ct));

        await using var db = NewContext();
        Assert.Equal(target.Id, (await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct)).ScenarioTemplateId);
    }

    [Fact]
    public async Task Move_into_a_template_is_forbidden_for_a_member_holding_only_ViewScenarioTemplate_on_it()
    {
        var source = TestData.ScenarioTemplate("Source");
        var target = TestData.ScenarioTemplate("Target");
        var task = TestData.TemplateTask(source.Id);
        await Seed(source, target, task);
        var actor = await Actor()
            .OnScenarioTemplate(source.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate])
            .OnScenarioTemplate(target.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}/move", new { id = target.Id, locationType = "scenarioTemplate" }, Ct));
        await using var db = NewContext();
        Assert.Equal(source.Id, (await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct)).ScenarioTemplateId);
    }

    [Fact]
    public async Task Move_moves_a_template_task_into_another_template_for_a_caller_holding_EditScenarioTemplates()
    {
        var source = TestData.ScenarioTemplate("Source");
        var target = TestData.ScenarioTemplate("Target");
        var task = TestData.TemplateTask(source.Id);
        await Seed(source, target, task);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}/move", new { id = target.Id, locationType = "scenarioTemplate" }, Ct));

        await using var db = NewContext();
        Assert.Equal(target.Id, (await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct)).ScenarioTemplateId);
    }

    [Fact]
    public async Task Move_into_a_template_is_forbidden_for_a_member_who_may_edit_the_source_but_holds_only_EditScenarios()
    {
        var source = TestData.ScenarioTemplate("Source");
        var target = TestData.ScenarioTemplate("Target");
        var task = TestData.TemplateTask(source.Id);
        await Seed(source, target, task);
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.EditScenarios)
            .OnScenarioTemplate(source.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}/move", new { id = target.Id, locationType = "scenarioTemplate" }, Ct));
        await using var db = NewContext();
        Assert.Equal(source.Id, (await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct)).ScenarioTemplateId);
    }

    [Fact]
    public async Task Copy_copies_a_template_task_into_another_template_for_a_member_who_may_view_the_source_and_edit_the_target()
    {
        var source = TestData.ScenarioTemplate("Source");
        var target = TestData.ScenarioTemplate("Target");
        var task = TestData.TemplateTask(source.Id, "Copied");
        await Seed(source, target, task);
        var actor = await Actor()
            .OnScenarioTemplate(source.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate])
            .OnScenarioTemplate(target.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PostAsJsonAsync($"api/tasks/{task.Id}/copy", new { id = target.Id, locationType = "scenarioTemplate" }, Ct));

        await using var db = NewContext();
        Assert.Equal(["Copied"], await db.Tasks.Where(x => x.ScenarioTemplateId == target.Id).Select(x => x.Name).ToListAsync(Ct));
    }

    [Fact]
    public async Task Copy_of_a_template_task_is_forbidden_for_a_member_holding_ViewScenarioTemplate_only_on_another_template()
    {
        var source = TestData.ScenarioTemplate("Source");
        var target = TestData.ScenarioTemplate("Target");
        var task = TestData.TemplateTask(source.Id);
        await Seed(source, target, task);
        var actor = await Actor()
            .OnNewScenarioTemplate(ScenarioTemplatePermission.ViewScenarioTemplate)
            .OnScenarioTemplate(target.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/tasks/{task.Id}/copy", new { id = target.Id, locationType = "scenarioTemplate" }, Ct));
        await using var db = NewContext();
        Assert.False(await db.Tasks.AnyAsync(x => x.ScenarioTemplateId == target.Id, Ct));
    }

    [Fact]
    public async Task Copy_into_a_template_is_forbidden_for_a_member_holding_only_ViewScenarioTemplate_on_it()
    {
        var source = TestData.ScenarioTemplate("Source");
        var target = TestData.ScenarioTemplate("Target");
        var task = TestData.TemplateTask(source.Id);
        await Seed(source, target, task);
        var actor = await Actor()
            .OnScenarioTemplate(source.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate])
            .OnScenarioTemplate(target.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/tasks/{task.Id}/copy", new { id = target.Id, locationType = "scenarioTemplate" }, Ct));
    }

    [Fact]
    public async Task Copy_copies_a_template_task_into_another_template_for_a_caller_holding_ViewScenarioTemplates_who_may_edit_the_target()
    {
        var source = TestData.ScenarioTemplate("Source");
        var target = TestData.ScenarioTemplate("Target");
        var task = TestData.TemplateTask(source.Id, "Copied");
        await Seed(source, target, task);
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.ViewScenarioTemplates)
            .OnScenarioTemplate(target.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PostAsJsonAsync($"api/tasks/{task.Id}/copy", new { id = target.Id, locationType = "scenarioTemplate" }, Ct));

        await using var db = NewContext();
        Assert.Equal(["Copied"], await db.Tasks.Where(x => x.ScenarioTemplateId == target.Id).Select(x => x.Name).ToListAsync(Ct));
    }

    [Fact]
    public async Task Copy_of_a_template_task_is_forbidden_for_a_member_who_may_edit_the_target_but_holds_only_ViewScenarios()
    {
        var source = TestData.ScenarioTemplate("Source");
        var target = TestData.ScenarioTemplate("Target");
        var task = TestData.TemplateTask(source.Id);
        await Seed(source, target, task);
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.ViewScenarios)
            .OnScenarioTemplate(target.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/tasks/{task.Id}/copy", new { id = target.Id, locationType = "scenarioTemplate" }, Ct));
        await using var db = NewContext();
        Assert.False(await db.Tasks.AnyAsync(x => x.ScenarioTemplateId == target.Id, Ct));
    }

    [Fact]
    public async Task CreateFromResult_creates_a_template_task_from_a_template_tasks_result_for_a_member_who_may_view_and_edit()
    {
        var template = TestData.ScenarioTemplate();
        var task = TestData.TemplateTask(template.Id);
        var result = TestData.Result(task.Id);
        await Seed(template, task, result);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate, ScenarioTemplatePermission.EditScenarioTemplate]).SeedAsync();

        var created = await ReadAsync<SAVM.Task>(await Client(actor).PostAsJsonAsync($"api/tasks/copyfromresult/{result.Id}", new { id = template.Id, locationType = "scenarioTemplate" }, Ct));

        await using var db = NewContext();
        Assert.Equal(template.Id, (await db.Tasks.SingleAsync(x => x.Id == created.Id, Ct)).ScenarioTemplateId);
    }

    [Fact]
    public async Task CreateFromResult_of_a_template_tasks_result_is_forbidden_for_a_member_holding_ViewScenarioTemplate_only_on_another_template()
    {
        var template = TestData.ScenarioTemplate();
        var target = TestData.ScenarioTemplate("Target");
        var task = TestData.TemplateTask(template.Id);
        var result = TestData.Result(task.Id);
        await Seed(template, target, task, result);
        var actor = await Actor()
            .OnNewScenarioTemplate(ScenarioTemplatePermission.ViewScenarioTemplate)
            .OnScenarioTemplate(target.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/tasks/copyfromresult/{result.Id}", new { id = target.Id, locationType = "scenarioTemplate" }, Ct));
    }

    [Fact]
    public async Task CreateFromResult_into_a_template_is_forbidden_for_a_member_holding_only_ViewScenarioTemplate_on_it()
    {
        var template = TestData.ScenarioTemplate();
        var task = TestData.TemplateTask(template.Id);
        var result = TestData.Result(task.Id);
        await Seed(template, task, result);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/tasks/copyfromresult/{result.Id}", new { id = template.Id, locationType = "scenarioTemplate" }, Ct));
    }

    [Fact]
    public async Task CreateFromResult_creates_a_template_task_for_a_caller_holding_ViewScenarioTemplates_who_may_edit_the_target()
    {
        var template = TestData.ScenarioTemplate();
        var target = TestData.ScenarioTemplate("Target");
        var task = TestData.TemplateTask(template.Id);
        var result = TestData.Result(task.Id);
        await Seed(template, target, task, result);
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.ViewScenarioTemplates)
            .OnScenarioTemplate(target.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate])
            .SeedAsync();

        var created = await ReadAsync<SAVM.Task>(await Client(actor).PostAsJsonAsync($"api/tasks/copyfromresult/{result.Id}", new { id = target.Id, locationType = "scenarioTemplate" }, Ct));

        await using var db = NewContext();
        Assert.Equal(target.Id, (await db.Tasks.SingleAsync(x => x.Id == created.Id, Ct)).ScenarioTemplateId);
    }

    [Fact]
    public async Task CreateFromResult_of_a_template_tasks_result_is_forbidden_for_a_member_who_may_edit_the_target_but_holds_only_ViewScenarios()
    {
        var template = TestData.ScenarioTemplate();
        var target = TestData.ScenarioTemplate("Target");
        var task = TestData.TemplateTask(template.Id);
        var result = TestData.Result(task.Id);
        await Seed(template, target, task, result);
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.ViewScenarios)
            .OnScenarioTemplate(target.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate])
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/tasks/copyfromresult/{result.Id}", new { id = target.Id, locationType = "scenarioTemplate" }, Ct));
        await using var db = NewContext();
        Assert.False(await db.Tasks.AnyAsync(x => x.ScenarioTemplateId == target.Id, Ct));
    }

    /// <summary>The edit body carries no parent: a task stays in its scenario and under its trigger whatever the body names.</summary>
    [Fact]
    public async Task Update_keeps_the_task_in_its_scenario_and_under_its_trigger_whatever_the_body_names()
    {
        var mine = TestData.Scenario("Mine");
        var theirs = TestData.Scenario("Theirs");
        var trigger = TestData.ScenarioTask(mine.Id, "Trigger");
        var task = TestData.ScenarioTask(mine.Id, "Kept", triggerTaskId: trigger.Id);
        var theirStart = TestData.ScenarioTask(theirs.Id, "Their start");
        await Seed(mine, theirs, trigger, task, theirStart);
        var actor = await Actor().OnScenario(mine.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        var body = new Dictionary<string, object>
        {
            ["name"] = "Renamed",
            ["scenarioId"] = theirs.Id,
            ["triggerTaskId"] = theirStart.Id,
            ["userId"] = Guid.NewGuid(),
            ["currentIteration"] = 7,
            ["action"] = "send_email",
            ["apiUrl"] = "email",
            ["vmMask"] = "",
            ["actionParameters"] = new Dictionary<string, string> { ["Subject"] = "hello" },
            ["triggerCondition"] = "Success",
            ["expectedOutput"] = ""
        };
        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}", body, Ct));

        await using var db = NewContext();
        var stored = await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct);
        Assert.Equal(("Renamed", mine.Id, (Guid?)trigger.Id, task.UserId, 0), (stored.Name, stored.ScenarioId.Value, stored.TriggerTaskId, stored.UserId, stored.CurrentIteration));
        Assert.False(await db.Tasks.AnyAsync(x => x.TriggerTaskId == theirStart.Id, Ct));
    }

    [Fact]
    public async Task Update_of_a_task_with_neither_a_scenario_nor_a_template_is_forbidden()
    {
        var scenario = TestData.Scenario();
        var orphan = TestData.ScenarioTask(scenario.Id, "Orphan");
        orphan.ScenarioId = null;
        await Seed(scenario, orphan);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/tasks/{orphan.Id}", TaskForm("Taken"), Ct));

        await using var db = NewContext();
        Assert.Equal("Orphan", (await db.Tasks.SingleAsync(x => x.Id == orphan.Id, Ct)).Name);
    }

    [Fact]
    public async Task Delete_of_a_task_with_neither_a_scenario_nor_a_template_is_forbidden()
    {
        var scenario = TestData.Scenario();
        var orphan = TestData.ScenarioTask(scenario.Id, "Orphan");
        orphan.ScenarioId = null;
        await Seed(scenario, orphan);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/tasks/{orphan.Id}", Ct));

        await using var db = NewContext();
        Assert.True(await db.Tasks.AnyAsync(x => x.Id == orphan.Id, Ct));
    }

    [Fact]
    public async Task Delete_detaches_a_subtask_of_another_scenario_and_makes_it_manual()
    {
        var mine = TestData.Scenario("Mine");
        var theirs = TestData.Scenario("Theirs");
        var task = TestData.ScenarioTask(mine.Id, "Deleted");
        var child = TestData.ScenarioTask(mine.Id, "Deleted child", triggerTaskId: task.Id);
        var foreign = TestData.ScenarioTask(theirs.Id, "Foreign grandchild", triggerTaskId: child.Id);
        await Seed(mine, theirs, task, child, foreign);
        var actor = await Actor().OnScenario(mine.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/tasks/{task.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.Tasks.AnyAsync(x => x.Id == task.Id || x.Id == child.Id, Ct));
        var kept = await db.Tasks.SingleAsync(x => x.Id == foreign.Id, Ct);
        Assert.Equal((theirs.Id, (Guid?)null, TaskTrigger.Manual), (kept.ScenarioId.Value, kept.TriggerTaskId, kept.TriggerCondition));
    }

    [Fact]
    public async Task GetByViewId_returns_only_the_tasks_of_the_scenarios_of_the_view_the_member_may_view()
    {
        var viewId = Guid.NewGuid();
        var mine = TestData.Scenario("Mine", viewId: viewId);
        var myTask = TestData.ScenarioTask(mine.Id, "Mine");
        myTask.UserExecutable = true;
        await Seed(mine, myTask);
        var theirs = TestData.Scenario("Theirs", viewId: viewId);
        var theirTask = TestData.ScenarioTask(theirs.Id, "Theirs");
        theirTask.UserExecutable = true;
        await Seed(theirs, theirTask);
        var actor = await Actor().OnScenario(mine.Id, permissions: [ScenarioPermission.ViewTasks, ScenarioPermission.ViewScenario]).SeedAsync();

        var tasks = await ReadAsync<List<SAVM.Task>>(await Client(actor).GetAsync($"api/views/{viewId}/tasks", Ct));

        Assert.Equal([myTask.Id], tasks.Select(x => x.Id));
    }

    /// <summary>Each scenario of the view decides for itself whether its tasks come in full or as summaries.</summary>
    [Fact]
    public async Task GetByViewId_gives_full_tasks_where_the_member_may_view_the_scenario_and_summaries_elsewhere()
    {
        var viewId = Guid.NewGuid();
        var full = TestData.Scenario("Full", viewId: viewId);
        var summary = TestData.Scenario("Summary", viewId: viewId);
        var fullTask = TestData.ScenarioTask(full.Id, "Full");
        fullTask.UserExecutable = true;
        fullTask.InputString = """{"Secret":"full"}""";
        var summaryTask = TestData.ScenarioTask(summary.Id, "Summary");
        summaryTask.UserExecutable = true;
        summaryTask.InputString = """{"Secret":"summary"}""";
        await Seed(full, summary, fullTask, summaryTask);
        var actor = await Actor()
            .OnScenario(full.Id, permissions: [ScenarioPermission.ViewTasks, ScenarioPermission.ViewScenario])
            .OnScenario(summary.Id, permissions: [ScenarioPermission.ViewTasks])
            .SeedAsync();

        var tasks = await ReadAsync<List<SAVM.Task>>(await Client(actor).GetAsync($"api/views/{viewId}/tasks", Ct));

        Assert.Equal("full", tasks.Single(x => x.Id == fullTask.Id).ActionParameters["Secret"]);
        Assert.Null(tasks.Single(x => x.Id == summaryTask.Id).ActionParameters);
    }

    private static object TaskForm(string name, Guid? scenarioId = null, Guid? scenarioTemplateId = null, Guid? triggerTaskId = null) => new
    {
        name,
        scenarioId,
        scenarioTemplateId,
        triggerTaskId,
        action = "send_email",
        apiUrl = "email",
        vmMask = "",
        actionParameters = new Dictionary<string, string> { ["Subject"] = "hello" },
        triggerCondition = "Manual",
        expectedOutput = ""
    };

    private sealed record GradeCheck(Guid GradedTaskId, DateTime ExecutionStartTime);
}
