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

    [Fact]
    public async Task A_subtask_in_another_scenario_is_not_run_when_its_trigger_finishes()
    {
        var parent = await SeedActiveTask(TaskAction.send_email, "email", """{"Subject":"marker-parent"}""");
        var sibling = TestData.ScenarioTask(parent.ScenarioId.Value, "Same scenario child", triggerTaskId: parent.Id);
        sibling.InputString = """{"Subject":"marker-same-scenario"}""";
        sibling.UserId = parent.UserId;
        var other = TestData.Scenario("Other", status: ScenarioStatus.active);
        var foreign = TestData.ScenarioTask(other.Id, "Other scenario child", triggerTaskId: parent.Id);
        foreign.InputString = """{"Subject":"marker-other-scenario"}""";
        foreign.UserId = parent.UserId;
        await Seed(sibling, other, foreign);

        await Run(RootHost, parent);
        await WaitForFinishedResult(sibling.Id);

        await using var db = NewContext();
        Assert.False(await db.Results.AnyAsync(x => x.TaskId == foreign.Id, Ct));
        Assert.Empty(RootHost.Executors.CallsContaining("marker-other-scenario"));
    }

    /// <summary>
    /// A row left on the removed StackStorm API (the az_* actions, which TaskAction no longer names) fails
    /// with a message saying so, rather than leaving its result pending.
    /// </summary>
    [Fact]
    public async Task A_task_left_on_the_removed_stackstorm_api_fails_saying_the_action_was_removed()
    {
        var task = await SeedActiveTask((TaskAction)105, "stackstorm", "{}");

        await Run(RootHost, task);
        var result = await WaitForFinishedResult(task.Id);

        Assert.Equal(
            (TaskStatus.failed, "StackStorm Action 105 was removed in Steamfitter 3.10 and can no longer be run."),
            (result.Status, result.ActualOutput));
        Assert.Empty(RootHost.Executors.Calls);
    }

    [Fact]
    public async Task A_task_whose_api_has_no_handler_fails_and_its_task_reaches_a_terminal_status()
    {
        var task = await SeedActiveTask(TaskAction.send_email, "nonesuch", "{}");

        await Run(RootHost, task);
        var result = await WaitForFinishedResult(task.Id);

        Assert.Equal((TaskStatus.failed, "API (nonesuch) is not currently implemented"), (result.Status, result.ActualOutput));
        await WaitForTaskStatus(task.Id, TaskStatus.failed);
    }

    [Theory]
    [InlineData("vm", TaskAction.send_email, "Player VM API Action send_email has not been implemented.")]
    [InlineData("ssh", TaskAction.send_email, "SSH Action send_email has not been implemented.")]
    [InlineData("email", TaskAction.linux_rm, "Email Action linux_rm has not been implemented.")]
    public async Task An_action_its_api_has_no_handler_for_fails_with_the_message_as_its_output(string apiUrl, TaskAction action, string message)
    {
        var task = await SeedActiveTask(action, apiUrl, "{}");

        await Run(RootHost, task);
        var result = await WaitForFinishedResult(task.Id);

        Assert.Equal((TaskStatus.failed, message), (result.Status, result.ActualOutput));
    }

    /// <summary>A task that fails for want of a handler still runs its Failure subtask.</summary>
    [Fact]
    public async Task A_task_whose_api_has_no_handler_runs_its_failure_subtask()
    {
        var task = await SeedActiveTask((TaskAction)105, "stackstorm", "{}");
        var onFailure = TestData.ScenarioTask(task.ScenarioId.Value, "On Failure", triggerTaskId: task.Id);
        onFailure.TriggerCondition = TaskTrigger.Failure;
        onFailure.InputString = """{"Subject":"marker-on-failure"}""";
        var onSuccess = TestData.ScenarioTask(task.ScenarioId.Value, "On Success", triggerTaskId: task.Id);
        onSuccess.InputString = """{"Subject":"marker-on-success"}""";
        await Seed(onFailure, onSuccess);

        await Run(RootHost, task);
        var result = await WaitForFinishedResult(onFailure.Id);

        Assert.Equal(TaskStatus.succeeded, result.Status);
        Assert.Single(RootHost.Executors.CallsContaining("marker-on-failure"));
        Assert.Empty(RootHost.Executors.CallsContaining("marker-on-success"));
    }

    /// <summary>
    /// A VM task lists the View's Vms through the Player VM API's <c>views/{viewId}/vms/all</c>, which
    /// returns every Vm in the View to Steamfitter's system-level account, and creates a result for each
    /// Vm its mask matches.
    /// </summary>
    [Fact]
    public async Task A_vm_task_runs_against_the_vms_of_the_view_that_the_vm_api_lists_for_all_vms()
    {
        var user = new ClaimsPrincipalBuilder().WithSystemPermissions(Enum.GetValues<SystemPermission>()).Build();
        // IdentityModel refuses a discovery document over http except on loopback, one without a key set,
        // and a password grant without a user name.
        var host = HostFor(user, options =>
        {
            options.ResourceOwnerAuthorization.Authority = "http://localhost:5000";
            options.ResourceOwnerAuthorization.UserName = "steamfitter";
            options.ResourceOwnerAuthorization.Password = "secret";
        });
        var viewId = Guid.NewGuid();
        var target = Guid.NewGuid();
        var other = Guid.NewGuid();
        host.OutboundHttp
            .AnswersJson("GET .well-known/openid-configuration", """{"issuer":"http://localhost:5000","token_endpoint":"http://localhost:5000/connect/token","jwks_uri":"http://localhost:5000/jwks"}""")
            .AnswersJson("GET jwks", """{"keys":[]}""")
            .AnswersJson("POST connect/token", """{"access_token":"vm-api-token","token_type":"Bearer","expires_in":3600}""")
            .AnswersJson($"GET api/views/{viewId}/vms/all", $$"""[{"id":"{{target}}","name":"target-vm"},{"id":"{{other}}","name":"other-vm"}]""")
            .RefusesUnmatched();
        var task = await SeedActiveTask(TaskAction.guest_process_run, "vm", """{"Moid":"{moid}","marker":"marker-all-vms"}""", viewId: viewId, vmMask: "target");

        await Run(host, task);
        var result = await WaitForFinishedResult(task.Id);

        Assert.Equal((TaskStatus.succeeded, target, "target-vm"), (result.Status, result.VmId, result.VmName));
        var call = Assert.Single(host.Executors.CallsContaining("marker-all-vms"));
        Assert.Equal(nameof(IVmOperationsService.GuestCommand), call.Operation);
        Assert.Contains(target.ToString(), call.Parameters);
        Assert.Equal("Bearer vm-api-token", Assert.Single(host.OutboundHttp.Sent, x => x.Path == $"api/views/{viewId}/vms/all").Authorization);
    }

    private async Task<TaskEntity> SeedActiveTask(TaskAction action, string apiUrl, string inputString, ScenarioStatus status = ScenarioStatus.active, string expectedOutput = "", Guid? viewId = null, string vmMask = "")
    {
        var scenario = TestData.Scenario(status: status, viewId: viewId);
        var task = TestData.ScenarioTask(scenario.Id);
        task.Action = action;
        task.ApiUrl = apiUrl;
        task.InputString = inputString;
        task.ExpectedOutput = expectedOutput;
        task.VmMask = vmMask;
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

    private async Task WaitForTaskStatus(Guid taskId, TaskStatus status)
    {
        await WaitUntil(async () =>
        {
            await using var db = NewContext();
            return await db.Tasks.AnyAsync(x => x.Id == taskId && x.Status == status, Ct);
        }, $"task {taskId} to reach {status}");
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
