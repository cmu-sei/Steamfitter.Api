// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data;
using Steamfitter.Api.Tests.Support;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Tests.Infrastructure.Filters;

/// <summary>Malformed bodies, route values and enum values at the edge of the MVC pipeline.</summary>
public class RequestValidationTests(DatabaseFixture fixture, SteamfitterAppFactory factory) : ApiTestBase(fixture, factory)
{
    [Fact]
    public async Task A_malformed_json_body_is_a_400_validation_problem()
    {
        var response = await RootClient.PostAsync("api/groups", new StringContent("{ not json", Encoding.UTF8, "application/json"), Ct);

        var problem = await AssertProblem(HttpStatusCode.BadRequest, response);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task A_route_id_that_is_not_a_guid_is_a_400_validation_problem()
    {
        await AssertProblem(HttpStatusCode.BadRequest, await RootClient.GetAsync("api/groups/not-a-guid", Ct));
    }

    /// <summary>An action number outside TaskAction is accepted and stored.</summary>
    [Fact]
    public async Task An_out_of_range_task_action_is_stored()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);

        var response = await RootClient.PostAsJsonAsync(
            "api/tasks",
            new { name = "Out of range", scenarioId = scenario.Id, action = 9999, apiUrl = "email", vmMask = "", actionParameters = new { } },
            Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        await using var db = NewContext();
        Assert.Equal(9999, (int)(await db.Tasks.SingleAsync(x => x.Name == "Out of range", Ct)).Action);
    }

    [Fact]
    public async Task An_out_of_range_task_iteration_termination_is_stored()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);

        var response = await RootClient.PostAsJsonAsync("api/tasks",
            new { name = "Out of range", scenarioId = scenario.Id, action = "send_email", apiUrl = "email",
                vmMask = "", actionParameters = new { }, triggerCondition = "Manual", iterationTermination = 9999 }, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        await using var db = NewContext();
        Assert.Equal(9999, (int)(await db.Tasks.SingleAsync(Ct)).IterationTermination);
    }

    [Fact]
    public async Task An_out_of_range_task_trigger_is_stored()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);

        var response = await RootClient.PostAsJsonAsync("api/tasks",
            new { name = "Out of range", scenarioId = scenario.Id, action = "send_email", apiUrl = "email",
                vmMask = "", actionParameters = new { }, triggerCondition = 9999 }, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        await using var db = NewContext();
        Assert.Equal(9999, (int)(await db.Tasks.SingleAsync(Ct)).TriggerCondition);
    }

    /// <summary>A result's action and status belong to the task runner, so a create body cannot set them, in range or not.</summary>
    [Fact]
    public async Task Create_of_a_result_ignores_an_out_of_range_action_and_status()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);

        var response = await RootClient.PostAsJsonAsync("api/results",
            new { taskId = task.Id, action = 9999, status = 9999, actionParameters = new { }, apiUrl = "email" }, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        await using var db = NewContext();
        var stored = await db.Results.SingleAsync(Ct);
        Assert.Equal(Steamfitter.Api.Data.TaskStatus.none, stored.Status);
        Assert.NotEqual(9999, (int)stored.Action);
    }

    /// <summary>A scenario's status changes only through start and end, so an edit body cannot set it, in range or not.</summary>
    [Fact]
    public async Task Update_ignores_an_out_of_range_scenario_status()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);

        var response = await RootClient.PutAsJsonAsync($"api/scenarios/{scenario.Id}",
            new { id = scenario.Id, name = scenario.Name, status = 9999 }, Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        await using var db = NewContext();
        Assert.Equal(scenario.Status, (await db.Scenarios.SingleAsync(Ct)).Status);
    }

    [Fact]
    public async Task Create_stores_and_returns_an_out_of_range_system_permission()
    {
        var response = await RootClient.PostAsJsonAsync("api/system-roles",
            new { name = "Out of range", permissions = new[] { 9999 } }, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        var created = await ReadAsync<SAVM.SystemRole>(response);
        Assert.Equal((SystemPermission)9999, Assert.Single(created.Permissions));
        await using var db = NewContext();
        Assert.Equal((SystemPermission)9999, Assert.Single((await db.SystemRoles.SingleAsync(x => x.Id == created.Id, Ct)).Permissions));
    }
}
