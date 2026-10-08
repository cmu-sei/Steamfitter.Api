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
using TaskStatus = Steamfitter.Api.Data.TaskStatus;

namespace Steamfitter.Api.Tests.Controllers;

/// <summary><c>ResultController</c>: results by scenario, task, view, user and VM, and result CRUD.</summary>
public class ResultControllerTests(DatabaseFixture fixture, SteamfitterAppFactory factory)
    : ApiTestBase(fixture, factory)
{
    /// <summary>The title of the <c>ForbiddenException</c> the controllers' permission checks throw.</summary>
    private const string InsufficientPermissions = "Insufficient Permissions";

    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task An_unauthenticated_request_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync("api/me/results", Ct));
    }

    [Fact]
    public async Task GetByScenarioId_returns_the_scenarios_results_to_a_member_holding_ViewScenario()
    {
        var (scenario, task) = await SeedScenarioTask();
        var result = TestData.Result(task.Id);
        await Seed(result);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        var results = await ReadAsync<List<SAVM.Result>>(await Client(actor).GetAsync($"api/scenarios/{scenario.Id}/results", Ct));

        Assert.Equal([result.Id], results.Select(x => x.Id));
    }

    [Fact]
    public async Task GetByScenarioId_is_forbidden_for_a_member_holding_only_ViewTasks()
    {
        var (scenario, _) = await SeedScenarioTask();
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewTasks]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/scenarios/{scenario.Id}/results", Ct));
    }

    [Fact]
    public async Task GetByScenarioId_is_forbidden_for_a_caller_holding_ViewScenario_only_on_another_scenario()
    {
        var (scenario, _) = await SeedScenarioTask();
        var actor = await Actor().OnNewScenario(ScenarioPermission.ViewScenario).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/scenarios/{scenario.Id}/results", Ct));
    }

    [Fact]
    public async Task GetByTaskId_returns_the_tasks_results_to_a_member_holding_ViewScenario()
    {
        var (scenario, task) = await SeedScenarioTask();
        var result = TestData.Result(task.Id);
        await Seed(result);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        var results = await ReadAsync<List<SAVM.Result>>(await Client(actor).GetAsync($"api/tasks/{task.Id}/results", Ct));

        Assert.Equal([result.Id], results.Select(x => x.Id));
    }

    [Fact]
    public async Task GetByTaskId_is_forbidden_for_a_caller_holding_ViewScenario_only_on_another_scenario()
    {
        var (_, task) = await SeedScenarioTask();
        var actor = await Actor().OnNewScenario(ScenarioPermission.ViewScenario).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/tasks/{task.Id}/results", Ct));
    }

    [Fact]
    public async Task GetByViewId_returns_the_views_results_to_a_member_of_its_scenario_holding_ViewScenario()
    {
        var viewId = Guid.NewGuid();
        var (scenario, task) = await SeedScenarioTask(viewId);
        var result = TestData.Result(task.Id);
        await Seed(result);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        var results = await ReadAsync<List<SAVM.Result>>(await Client(actor).GetAsync($"api/views/{viewId}/results", Ct));

        Assert.Equal([result.Id], results.Select(x => x.Id));
    }

    [Fact]
    public async Task GetByViewId_is_forbidden_for_a_member_of_its_scenario_holding_only_ViewTasks()
    {
        var viewId = Guid.NewGuid();
        var (scenario, _) = await SeedScenarioTask(viewId);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewTasks]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/views/{viewId}/results", Ct));
    }

    [Fact]
    public async Task GetByViewId_is_forbidden_for_a_caller_holding_ViewScenario_only_on_a_scenario_of_another_view()
    {
        var viewId = Guid.NewGuid();
        var (_, task) = await SeedScenarioTask(viewId);
        await Seed(TestData.Result(task.Id));
        var (elsewhere, _) = await SeedScenarioTask(Guid.NewGuid());
        var actor = await Actor().OnScenario(elsewhere.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/views/{viewId}/results", Ct));
    }

    [Fact]
    public async Task GetByUserId_returns_the_users_results_to_a_caller_holding_ViewScenarios()
    {
        var user = TestData.User();
        var (_, task) = await SeedScenarioTask();
        var result = TestData.Result(task.Id, createdBy: user.Id);
        await Seed(user, result);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        var results = await ReadAsync<List<SAVM.Result>>(await Client(actor).GetAsync($"api/users/{user.Id}/results", Ct));

        Assert.Equal([result.Id], results.Select(x => x.Id));
    }

    [Fact]
    public async Task GetByUserId_is_forbidden_for_a_caller_holding_only_ViewUsers()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewUsers).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/users/{Guid.NewGuid()}/results", Ct));
    }

    [Fact]
    public async Task GetMine_returns_the_callers_own_results()
    {
        var (_, task) = await SeedScenarioTask();
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewGroups).SeedAsync();
        var mine = TestData.Result(task.Id, createdBy: actor.Id);
        await Seed(mine, TestData.Result(task.Id, createdBy: Root.Id));

        var results = await ReadAsync<List<SAVM.Result>>(await Client(actor).GetAsync("api/me/results", Ct));

        Assert.Equal([mine.Id], results.Select(x => x.Id));
    }

    [Fact]
    public async Task GetByVmId_returns_only_the_results_of_scenarios_a_member_belongs_to()
    {
        var vmId = Guid.NewGuid();
        var (mine, myTask) = await SeedScenarioTask();
        var (_, otherTask) = await SeedScenarioTask();
        var visible = TestData.Result(myTask.Id, vmId);
        await Seed(visible, TestData.Result(otherTask.Id, vmId));
        var actor = await Actor().OnScenario(mine.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        var results = await ReadAsync<List<SAVM.Result>>(await Client(actor).GetAsync($"api/vms/{vmId}/results", Ct));

        Assert.Equal([visible.Id], results.Select(x => x.Id));
    }

    [Fact]
    public async Task GetByVmId_returns_every_result_on_the_vm_to_a_caller_holding_ViewScenarios()
    {
        var vmId = Guid.NewGuid();
        var (_, task) = await SeedScenarioTask();
        var result = TestData.Result(task.Id, vmId);
        await Seed(result);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        var results = await ReadAsync<List<SAVM.Result>>(await Client(actor).GetAsync($"api/vms/{vmId}/results", Ct));

        Assert.Equal([result.Id], results.Select(x => x.Id));
    }

    [Fact]
    public async Task GetByVmId_is_forbidden_for_a_template_member_holding_only_ViewScenarioTemplates()
    {
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.ViewScenarioTemplates)
            .OnNewScenarioTemplate(ScenarioTemplatePermission.ViewScenarioTemplate)
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/vms/{Guid.NewGuid()}/results", Ct));
    }

    /// <summary>A member holding only ViewTasks on a scenario reads that scenario's results on a VM.</summary>
    [Fact]
    public async Task GetByVmId_returns_the_results_of_a_scenario_to_a_member_holding_only_ViewTasks_on_it()
    {
        var vmId = Guid.NewGuid();
        var (scenario, task) = await SeedScenarioTask();
        var result = TestData.Result(task.Id, vmId);
        await Seed(result);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewTasks]).SeedAsync();

        var results = await ReadAsync<List<SAVM.Result>>(await Client(actor).GetAsync($"api/vms/{vmId}/results", Ct));

        Assert.Equal([result.Id], results.Select(x => x.Id));
    }

    [Fact]
    public async Task Get_returns_the_result_to_a_member_holding_ViewScenario()
    {
        var (scenario, task) = await SeedScenarioTask();
        var result = TestData.Result(task.Id);
        await Seed(result);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        var read = await ReadAsync<SAVM.Result>(await Client(actor).GetAsync($"api/results/{result.Id}", Ct));

        Assert.Equal(result.Id, read.Id);
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_ViewScenario_only_on_another_scenario()
    {
        var (_, task) = await SeedScenarioTask();
        var result = TestData.Result(task.Id);
        await Seed(result);
        var actor = await Actor().OnNewScenario(ScenarioPermission.ViewScenario).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/results/{result.Id}", Ct));
    }

    [Fact]
    public async Task Create_persists_a_result_for_a_member_holding_EditScenario()
    {
        var (scenario, task) = await SeedScenarioTask();
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync("api/results", ResultForm(task.Id, "Created output"), Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var created = await ReadAsync<SAVM.Result>(response);
        await using var db = NewContext();
        var stored = await db.Results.SingleAsync(x => x.Id == created.Id, Ct);
        Assert.Equal(("Created output", actor.Id), (stored.ActualOutput, stored.CreatedBy));
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_member_holding_only_ViewScenario()
    {
        var (scenario, task) = await SeedScenarioTask();
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/results", ResultForm(task.Id, "Refused"), Ct));
        await using var db = NewContext();
        Assert.False(await db.Results.AnyAsync(x => x.ActualOutput == "Refused", Ct));
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_EditScenario_only_on_another_scenario()
    {
        var (_, task) = await SeedScenarioTask();
        var actor = await Actor().OnNewScenario(ScenarioPermission.EditScenario).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/results", ResultForm(task.Id, "Refused"), Ct));
    }

    [Fact]
    public async Task Update_saves_the_result_for_a_member_holding_EditScenario()
    {
        var (scenario, task) = await SeedScenarioTask();
        var result = TestData.Result(task.Id);
        await Seed(result);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/results/{result.Id}", ResultForm(task.Id, "Updated", result.Id), Ct));

        await using var db = NewContext();
        Assert.Equal("Updated", (await db.Results.SingleAsync(x => x.Id == result.Id, Ct)).ActualOutput);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_member_holding_only_ViewScenario()
    {
        var (scenario, task) = await SeedScenarioTask();
        var result = TestData.Result(task.Id);
        await Seed(result);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/results/{result.Id}", ResultForm(task.Id, "Updated", result.Id), Ct));
    }

    [Fact]
    public async Task Delete_removes_the_result_for_a_member_holding_ManageScenario()
    {
        var (scenario, task) = await SeedScenarioTask();
        var result = TestData.Result(task.Id);
        await Seed(result);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ManageScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/results/{result.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.Results.AnyAsync(x => x.Id == result.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_member_holding_only_EditScenario()
    {
        var (scenario, task) = await SeedScenarioTask();
        var result = TestData.Result(task.Id);
        await Seed(result);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/results/{result.Id}", Ct));
        await using var db = NewContext();
        Assert.True(await db.Results.AnyAsync(x => x.Id == result.Id, Ct));
    }

    [Fact]
    public async Task Create_persists_a_result_for_a_caller_holding_EditScenarios()
    {
        var (_, task) = await SeedScenarioTask();
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.Created, await Client(actor).PostAsJsonAsync("api/results", ResultForm(task.Id, "System created"), Ct));

        await using var db = NewContext();
        Assert.Equal(actor.Id, (await db.Results.SingleAsync(x => x.ActualOutput == "System created", Ct)).CreatedBy);
    }

    [Fact]
    public async Task Create_of_a_result_without_a_task_is_forbidden_for_a_caller_holding_EditScenarios()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarios).SeedAsync();

        var error = await AssertJsonError(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/results", new { actualOutput = "No task" }, Ct));

        Assert.Equal(InsufficientPermissions, error.Title);
        await using var db = NewContext();
        Assert.False(await db.Results.AnyAsync(x => x.ActualOutput == "No task", Ct));
    }

    [Fact]
    public async Task Update_saves_the_result_for_a_caller_holding_EditScenarios()
    {
        var (_, task) = await SeedScenarioTask();
        var result = TestData.Result(task.Id);
        await Seed(result);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/results/{result.Id}", ResultForm(task.Id, "Updated", result.Id), Ct));

        await using var db = NewContext();
        Assert.Equal("Updated", (await db.Results.SingleAsync(x => x.Id == result.Id, Ct)).ActualOutput);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_only_ViewScenarios()
    {
        var (_, task) = await SeedScenarioTask();
        var result = TestData.Result(task.Id);
        await Seed(result);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/results/{result.Id}", ResultForm(task.Id, "Updated", result.Id), Ct));
    }

    [Fact]
    public async Task Delete_removes_the_result_for_a_caller_holding_ManageScenarios()
    {
        var (_, task) = await SeedScenarioTask();
        var result = TestData.Result(task.Id);
        await Seed(result);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/results/{result.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.Results.AnyAsync(x => x.Id == result.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_only_EditScenarios()
    {
        var (_, task) = await SeedScenarioTask();
        var result = TestData.Result(task.Id);
        await Seed(result);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/results/{result.Id}", Ct));
    }

    /// <summary>A result is moved onto a task of a scenario the caller may not edit when the body names that task.</summary>
    [Fact]
    public async Task Update_moves_a_result_onto_a_task_of_another_scenario_the_body_names()
    {
        var (mine, myTask) = await SeedScenarioTask();
        var (_, theirTask) = await SeedScenarioTask();
        var result = TestData.Result(myTask.Id);
        await Seed(result);
        var actor = await Actor().OnScenario(mine.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/results/{result.Id}", ResultForm(theirTask.Id, "Moved", result.Id), Ct));

        await using var db = NewContext();
        Assert.Equal(theirTask.Id, (await db.Results.SingleAsync(x => x.Id == result.Id, Ct)).TaskId);
    }

    /// <summary>An update body without the result's id is answered with a 500.</summary>
    [Fact]
    public async Task Update_answers_a_body_without_the_results_id_with_a_server_error()
    {
        var (_, task) = await SeedScenarioTask();
        var result = TestData.Result(task.Id);
        await Seed(result);

        var error = await AssertJsonError(HttpStatusCode.InternalServerError, await RootClient.PutAsJsonAsync($"api/results/{result.Id}", ResultForm(task.Id, "No id"), Ct));

        Assert.StartsWith("The property 'ResultEntity.Id' is part of a key", error.Detail);
    }

    /// <summary>A member of one scenario of a view reads the results of every scenario of the view.</summary>
    [Fact]
    public async Task GetByViewId_returns_the_results_of_every_scenario_of_the_view_to_a_member_of_one()
    {
        var viewId = Guid.NewGuid();
        var (mine, myTask) = await SeedScenarioTask(viewId);
        var (_, theirTask) = await SeedScenarioTask(viewId);
        var theirs = TestData.Result(theirTask.Id);
        await Seed(TestData.Result(myTask.Id), theirs);
        var actor = await Actor().OnScenario(mine.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        var results = await ReadAsync<List<SAVM.Result>>(await Client(actor).GetAsync($"api/views/{viewId}/results", Ct));

        Assert.Contains(theirs.Id, results.Select(x => x.Id));
    }

    /// <summary>Creating a result broadcasts nothing to the result's scenario.</summary>
    [Fact]
    public async Task Create_sends_nothing_to_the_results_scenario()
    {
        var (scenario, task) = await SeedScenarioTask();

        await AssertStatus(HttpStatusCode.Created, await RootClient.PostAsJsonAsync("api/results", ResultForm(task.Id, "Unannounced"), Ct));

        Assert.Empty(Factory.Hub<EngineHub>().ToGroup(scenario.Id));
    }

    private async Task<(Data.Models.ScenarioEntity Scenario, Data.Models.TaskEntity Task)> SeedScenarioTask(Guid? viewId = null)
    {
        var scenario = TestData.Scenario(viewId: viewId);
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);

        return (scenario, task);
    }

    private static object ResultForm(Guid taskId, string actualOutput, Guid? id = null) => new
    {
        id = id ?? Guid.Empty,
        taskId,
        action = "send_email",
        apiUrl = "email",
        actionParameters = new Dictionary<string, string>(),
        status = nameof(TaskStatus.succeeded),
        expectedOutput = "",
        actualOutput
    };
}
