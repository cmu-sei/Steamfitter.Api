// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Infrastructure.Options;
using Steamfitter.Api.Services;
using Steamfitter.Api.Tests.Support;

namespace Steamfitter.Api.Tests.Services;

/// <summary>
/// <c>XApiService</c>, the statements Steamfitter queues for the LRS. Driven directly over the test's
/// database with a queue that keeps what it was given.
/// </summary>
public class XApiServiceTests(DatabaseFixture fixture) : DatabaseTestBase(fixture)
{
    private const string Extensions = "https://crucible.sei.cmu.edu/xapi/extensions/";

    /// <summary>The executed-task activity carries its task's fields under the hyphenated extension IRIs.</summary>
    [Fact]
    public async Task An_executed_tasks_activity_carries_its_fields_under_the_hyphenated_extension_iris()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        var task = TestData.ScenarioTask(scenario.Id);
        task.Action = TaskAction.guest_process_run;
        task.ApiUrl = "vm";
        task.VmMask = "web";
        task.ExpectedOutput = "ok";
        var user = TestData.User(name: "Executor");
        await Seed(scenario, task, user);
        var queue = new RecordingQueue();

        Assert.True(await Service(queue).TaskExecutedAsync(task.Id, scenario.Id, user.Id, Ct));

        var extensions = ActivityExtensions(Assert.Single(queue.Statements));
        Assert.Equal(
            [
                ($"{Extensions}api-url", "vm"),
                ($"{Extensions}expected-output", "ok"),
                ($"{Extensions}task-action", "guest_process_run"),
                ($"{Extensions}vm-mask", "web"),
            ],
            extensions.Properties().Select(x => (x.Name, x.Value.ToString())).OrderBy(x => x.Name));
    }

    /// <summary>A task with no VM mask, API url or expected output leaves those extensions out.</summary>
    [Fact]
    public async Task An_executed_tasks_empty_fields_are_left_out_of_its_extensions()
    {
        var scenario = TestData.Scenario(status: ScenarioStatus.active);
        var task = TestData.ScenarioTask(scenario.Id);
        task.ApiUrl = "";
        await Seed(scenario, task);
        var queue = new RecordingQueue();

        Assert.True(await Service(queue).TaskExecutedAsync(task.Id, scenario.Id, Guid.NewGuid(), Ct));

        var extensions = ActivityExtensions(Assert.Single(queue.Statements));
        Assert.Equal([$"{Extensions}task-action"], extensions.Properties().Select(x => x.Name));
    }

    private XApiService Service(RecordingQueue queue) =>
        new(
            NewContext(),
            null,
            new XApiOptions { Enabled = true, Username = "lrs", ApiUrl = "http://steamfitter.test/api/", Platform = "Steamfitter" },
            queue,
            NullLogger<XApiService>.Instance);

    private static JObject ActivityExtensions(XApiQueuedStatementEntity statement) =>
        (JObject)JObject.Parse(statement.StatementJson)["object"]["definition"]["extensions"];

    private sealed class RecordingQueue : IXApiQueueService
    {
        private readonly ConcurrentQueue<XApiQueuedStatementEntity> _statements = new();

        public IReadOnlyList<XApiQueuedStatementEntity> Statements => [.. _statements];

        public Task EnqueueAsync(XApiQueuedStatementEntity statement, CancellationToken ct = default)
        {
            _statements.Enqueue(statement);
            return Task.CompletedTask;
        }

        public Task<List<XApiQueuedStatementEntity>> DequeueAsync(int batchSize = 10, CancellationToken ct = default) => throw new NotSupportedException();

        public Task MarkCompletedAsync(Guid statementId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task MarkFailedAsync(Guid statementId, string errorMessage, bool isTransientError, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<int> GetQueueDepthAsync(CancellationToken ct = default) => throw new NotSupportedException();

        public Task<List<XApiQueuedStatementEntity>> GetOldCompletedStatementsAsync(DateTime cutoffDate, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<List<XApiQueuedStatementEntity>> GetOldFailedStatementsAsync(DateTime cutoffDate, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<List<XApiQueuedStatementEntity>> GetStuckProcessingStatementsAsync(DateTime stuckThreshold, CancellationToken ct = default) => throw new NotSupportedException();

        public Task DeleteStatementsAsync(List<Guid> statementIds, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
