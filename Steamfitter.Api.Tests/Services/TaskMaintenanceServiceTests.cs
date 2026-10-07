// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steamfitter.Api.Data;
using Steamfitter.Api.Hubs;
using Steamfitter.Api.Infrastructure.HealthChecks;
using Steamfitter.Api.Infrastructure.Options;
using Steamfitter.Api.Services;
using Steamfitter.Api.Tests.Support;
using SAVM = Steamfitter.Api.ViewModels;
using TaskStatus = Steamfitter.Api.Data.TaskStatus;

namespace Steamfitter.Api.Tests.Services;

/// <summary>
/// <c>TaskMaintenanceService</c>, the hosted loop that expires pending results and ends overdue scenarios.
/// Driven directly over an <see cref="ApiTestHost"/>, whose pass period is one second; its work starts in
/// <c>StartAsync</c> and completes nowhere a test can await, so the tests wait for its effects.
/// </summary>
public class TaskMaintenanceServiceTests(DatabaseFixture fixture) : ServiceTestBase(fixture)
{
    /// <summary>Expired results are announced to the scenario administrators as a single Result, not a list.</summary>
    [Fact]
    public async Task An_expired_result_is_sent_to_the_scenario_administrators_as_a_single_result()
    {
        var resultId = await SeedOverduePendingResult();
        var logger = new RecordingLogger<TaskMaintenanceService>();

        await Start(logger);
        await WaitUntil(() => Task.FromResult(RootHost.Hub.ToGroup(EngineHub.SCENARIO_GROUP).Any(x => x.Method == EngineMethods.ResultsUpdated)), "a ResultsUpdated broadcast");

        var sent = RootHost.Hub.ToGroup(EngineHub.SCENARIO_GROUP).First(x => x.Method == EngineMethods.ResultsUpdated);
        Assert.Equal(resultId, Assert.IsType<SAVM.Result>(sent.Argument).Id);
    }

    [Fact]
    public async Task An_overdue_pending_result_is_stored_as_expired()
    {
        var resultId = await SeedOverduePendingResult();

        await Start(new RecordingLogger<TaskMaintenanceService>());
        await WaitUntil(async () =>
        {
            await using var db = NewContext();
            return (await db.Results.SingleAsync(x => x.Id == resultId, Ct)).Status == TaskStatus.expired;
        }, "the result to expire");
    }

    /// <summary>The scenario's own group is sent nothing: the pass ends in a NullReferenceException before it.</summary>
    [Fact]
    public async Task An_expired_result_is_not_sent_to_its_scenarios_group()
    {
        var scenarioId = Guid.NewGuid();
        await SeedOverduePendingResult(scenarioId);
        var logger = new RecordingLogger<TaskMaintenanceService>();

        await Start(logger);
        await WaitUntil(() => Task.FromResult(logger.At(LogLevel.Error).Any()), "the maintenance pass to log an error");

        Assert.IsType<NullReferenceException>(logger.At(LogLevel.Error)[0].Exception);
        Assert.Empty(RootHost.Hub.ToGroup(scenarioId));
    }

    [Fact]
    public async Task An_active_scenario_past_its_end_date_is_ended()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        scenario.StartDate = DateTime.UtcNow.AddDays(-2);
        scenario.EndDate = DateTime.UtcNow.AddDays(-1);
        await Seed(scenario);

        await Start(new RecordingLogger<TaskMaintenanceService>());
        await WaitUntil(async () =>
        {
            await using var db = NewContext();
            return (await db.Scenarios.SingleAsync(x => x.Id == scenario.Id, Ct)).Status == ScenarioStatus.ended;
        }, "the scenario to end");
    }

    private async Task<Guid> SeedOverduePendingResult(Guid? scenarioId = null)
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        scenario.Id = scenarioId ?? scenario.Id;
        var task = TestData.ScenarioTask(scenario.Id);
        var result = TestData.Result(task.Id, status: TaskStatus.pending);
        result.ExpirationSeconds = 1;
        await Seed(scenario, task, result);

        return result.Id;
    }

    /// <summary>Starts the service with a logger the test reads; its loop outlives the test and fails quietly once the host is gone.</summary>
    private Task Start(ILogger<TaskMaintenanceService> logger)
    {
        var host = RootHost;
        var service = new TaskMaintenanceService(
            logger,
            host.Resolve<IOptionsMonitor<VmTaskProcessingOptions>>(),
            host.Resolve<IServiceScopeFactory>(),
            TestMapper.Mapper,
            host.Resolve<IHubContext<EngineHub>>(),
            host.Resolve<TaskMaintenanceServiceHealthCheck>());

        return service.StartAsync(Ct);
    }
}
