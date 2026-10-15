// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Hubs;
using Steamfitter.Api.Services;
using Steamfitter.Api.Tests.Support;
using SAVM = Steamfitter.Api.ViewModels;
using TaskStatus = Steamfitter.Api.Data.TaskStatus;

namespace Steamfitter.Api.Tests.Services;

/// <summary>
/// <c>TaskExecutionService</c>, the hosted runner that takes tasks off <c>TaskExecutionQueue</c>, creates
/// their results and dispatches them to the executors. Each test owns a host, so its queue, hub recorder,
/// executor recorder and outbound HTTP stub are its own; the runner's loop blocks on that queue after the
/// test.
/// </summary>
public class TaskExecutionServiceTests(DatabaseFixture fixture) : ServiceTestBase(fixture)
{
    [Fact]
    public async Task An_email_task_is_sent_through_the_email_executor_and_its_result_succeeds()
    {
        var task = await SeedActiveTask(TaskAction.send_email, "email", """{"Subject":"marker-email"}""");

        await Run(RootHost, task);
        var result = await WaitForFinishedResult(task.Id);

        Assert.Equal((TaskStatus.succeeded, TaskActionRecorder.Output), (result.Status, result.ActualOutput));
        Assert.Equal(nameof(IEmailService.SendEmail), Assert.Single(RootHost.Executors.CallsContaining("marker-email")).Operation);
    }

    /// <summary>The runner announces results to the scenario administrators and the scenario as a list.</summary>
    [Fact]
    public async Task Results_are_sent_to_the_scenario_administrators_and_the_scenario_as_a_list()
    {
        var task = await SeedActiveTask(TaskAction.send_email, "email", "{}");

        await Run(RootHost, task);
        await WaitForFinishedResult(task.Id);

        var toAdministrators = RootHost.Hub.ToGroup(EngineHub.SCENARIO_GROUP).First(x => x.Method == EngineMethods.ResultsUpdated);
        var toScenario = RootHost.Hub.ToGroup(task.ScenarioId.Value).First(x => x.Method == EngineMethods.ResultsUpdated);
        Assert.Equal(task.Id, Assert.Single(Assert.IsAssignableFrom<IEnumerable<SAVM.Result>>(toAdministrators.Argument)).TaskId);
        Assert.Equal(task.Id, Assert.Single(Assert.IsAssignableFrom<IEnumerable<SAVM.Result>>(toScenario.Argument)).TaskId);
    }

    [Fact]
    public async Task An_http_task_to_a_host_outside_the_allow_list_fails_without_a_request()
    {
        var user = new ClaimsPrincipalBuilder().WithSystemPermissions(Enum.GetValues<SystemPermission>()).Build();
        var host = HostFor(user, options => options.HttpTask.AllowedHosts = ["allowed.test"]);
        var task = await SeedActiveTask(TaskAction.http_get, "http", """{"Url":"http://blocked.test/thing","Headers":"{\"X-Test\":\"1\"}"}""");

        await Run(host, task);
        var result = await WaitForFinishedResult(task.Id);

        Assert.Equal((TaskStatus.failed, "HTTP task URL host 'blocked.test' is not in the allowed hosts list."), (result.Status, result.ActualOutput));
        Assert.Empty(host.OutboundHttp.Requests);
    }

    [Fact]
    public async Task An_http_task_succeeds_when_the_response_contains_the_expected_output()
    {
        var user = new ClaimsPrincipalBuilder().WithSystemPermissions(Enum.GetValues<SystemPermission>()).Build();
        var host = HostFor(user, options => options.HttpTask.AllowedHosts = ["allowed.test"]);
        host.OutboundHttp.Respond("http://allowed.test/thing", Encoding.UTF8.GetBytes("the body"), "text/plain");
        var task = await SeedActiveTask(TaskAction.http_get, "http", """{"Url":"http://allowed.test/thing","Headers":"{\"X-Test\":\"1\"}"}""", expectedOutput: "body");

        await Run(host, task);
        var result = await WaitForFinishedResult(task.Id);

        Assert.Equal((TaskStatus.succeeded, "the body"), (result.Status, result.ActualOutput));
        Assert.Equal("1", Assert.Single(host.OutboundHttp.Sent).Headers["X-Test"]);
    }

    /// <summary>An http task's own headers are not sent when no header replacements are configured.</summary>
    [Fact]
    public async Task An_http_tasks_headers_are_not_sent_when_no_header_replacements_are_configured()
    {
        var user = new ClaimsPrincipalBuilder().WithSystemPermissions(Enum.GetValues<SystemPermission>()).Build();
        var host = HostFor(user, options => options.VmTaskProcessing.HttpHeaderReplacements = null);
        host.OutboundHttp.Respond("http://allowed.test/headers", Encoding.UTF8.GetBytes("the body"), "text/plain");
        var task = await SeedActiveTask(TaskAction.http_get, "http", """{"Url":"http://allowed.test/headers","Headers":"{\"X-Test\":\"1\"}"}""");

        await Run(host, task);
        await WaitForFinishedResult(task.Id);

        Assert.DoesNotContain("X-Test", Assert.Single(host.OutboundHttp.Sent).Headers.Keys);
    }

    [Fact]
    public async Task A_task_of_a_scenario_that_is_not_active_is_not_executed()
    {
        var task = await SeedActiveTask(TaskAction.send_email, "email", """{"Subject":"marker-inactive"}""", ScenarioStatus.ready);
        var barrier = await SeedActiveTask(TaskAction.send_email, "email", """{"Subject":"marker-barrier"}""");

        await Run(RootHost, task, barrier);
        await WaitForFinishedResult(barrier.Id);

        await using var db = NewContext();
        Assert.False(await db.Results.AnyAsync(x => x.TaskId == task.Id, Ct));
        Assert.Empty(RootHost.Executors.CallsContaining("marker-inactive"));
    }

    private async Task<TaskEntity> SeedActiveTask(TaskAction action, string apiUrl, string inputString, ScenarioStatus status = ScenarioStatus.active, string expectedOutput = "")
    {
        var scenario = TestData.Scenario(status: status);
        var task = TestData.ScenarioTask(scenario.Id);
        task.Action = action;
        task.ApiUrl = apiUrl;
        task.InputString = inputString;
        task.ExpectedOutput = expectedOutput;
        task.UserId = Guid.NewGuid();
        await Seed(scenario, task);

        return task;
    }

    /// <summary>Starts the host's runner and queues the tasks, as the execute endpoints do.</summary>
    private static async Task Run(ApiTestHost host, params TaskEntity[] tasks)
    {
        await host.Resolve<TaskExecutionService>().StartAsync(TestContext.Current.CancellationToken);

        foreach (var task in tasks)
        {
            host.Resolve<ITaskExecutionQueue>().Add(task);
        }
    }

    private async Task<ResultEntity> WaitForFinishedResult(Guid taskId)
    {
        ResultEntity result = null;

        await WaitUntil(async () =>
        {
            await using var db = NewContext();
            result = await db.Results.SingleOrDefaultAsync(x => x.TaskId == taskId && x.Status != TaskStatus.queued && x.Status != TaskStatus.pending, Ct);
            return result != null;
        }, $"a finished result of task {taskId}");

        return result;
    }
}
