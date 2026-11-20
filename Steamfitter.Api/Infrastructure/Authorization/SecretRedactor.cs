// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data;
using Steamfitter.Api.Services;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Infrastructure.Authorization;

/// <summary>
/// Hides the guest credentials in a response from a caller who may view, but not edit, the Scenario or Scenario
/// Template they belong to: a Scenario's or Scenario Template's VM Credentials are left out, and the secrets a Task
/// or Result copied from them are redacted. A caller who may edit gets them as they are, to edit them.
/// </summary>
public interface ISecretRedactor
{
    /// <summary>
    /// The response <paramref name="value"/> with the secrets the caller may not read hidden. A sequence is
    /// returned as a list, so that the records redacted are the ones written.
    /// </summary>
    Task<object> RedactAsync(object value, CancellationToken cancellationToken);
}

public class SecretRedactor(ISteamfitterAuthorizationService authorizationService, SteamfitterContext dbContext) : ISecretRedactor
{
    public async Task<object> RedactAsync(object value, CancellationToken cancellationToken)
    {
        switch (value)
        {
            case SAVM.Task task:
                await RedactTasksAsync([task], cancellationToken);
                return task;
            case IEnumerable<SAVM.Task> tasks:
                var taskList = tasks.ToList();
                await RedactTasksAsync(taskList, cancellationToken);
                return taskList;
            case SAVM.Result result:
                await RedactResultsAsync([result], cancellationToken);
                return result;
            case IEnumerable<SAVM.Result> results:
                var resultList = results.ToList();
                await RedactResultsAsync(resultList, cancellationToken);
                return resultList;
            case SAVM.Scenario scenario:
                await RedactScenariosAsync([scenario], cancellationToken);
                return scenario;
            case IEnumerable<SAVM.Scenario> scenarios:
                var scenarioList = scenarios.ToList();
                await RedactScenariosAsync(scenarioList, cancellationToken);
                return scenarioList;
            case SAVM.ScenarioTemplate scenarioTemplate:
                await RedactScenarioTemplatesAsync([scenarioTemplate], cancellationToken);
                return scenarioTemplate;
            case IEnumerable<SAVM.ScenarioTemplate> scenarioTemplates:
                var scenarioTemplateList = scenarioTemplates.ToList();
                await RedactScenarioTemplatesAsync(scenarioTemplateList, cancellationToken);
                return scenarioTemplateList;
            default:
                return value;
        }
    }

    private async Task RedactTasksAsync(IReadOnlyList<SAVM.Task> tasks, CancellationToken cancellationToken)
    {
        var access = await authorizationService.GetSecretAccessAsync(cancellationToken);
        foreach (var task in tasks.Where(x => x != null && !access.Allows(x.ScenarioId, x.ScenarioTemplateId)))
        {
            TaskSecretParameters.Redact(task.ActionParameters);
        }
    }

    private async Task RedactResultsAsync(IReadOnlyList<SAVM.Result> results, CancellationToken cancellationToken)
    {
        var access = await authorizationService.GetSecretAccessAsync(cancellationToken);
        if (access.AllowsAll || results.Count == 0)
            return;

        // a Result names only its Task, whose Scenario or Scenario Template decides
        var taskIds = results.Where(x => x?.TaskId != null).Select(x => x.TaskId.Value).Distinct().ToList();
        var scopes = await dbContext.Tasks
            .Where(x => taskIds.Contains(x.Id))
            .Select(x => new { x.Id, x.ScenarioId, x.ScenarioTemplateId })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var result in results.Where(x => x != null))
        {
            var allowed = result.TaskId != null && scopes.TryGetValue(result.TaskId.Value, out var scope)
                ? access.Allows(scope.ScenarioId, scope.ScenarioTemplateId)
                : access.Allows(null, null);
            if (!allowed)
                TaskSecretParameters.Redact(result.ActionParameters);
        }
    }

    private async Task RedactScenariosAsync(IReadOnlyList<SAVM.Scenario> scenarios, CancellationToken cancellationToken)
    {
        var access = await authorizationService.GetSecretAccessAsync(cancellationToken);
        foreach (var scenario in scenarios.Where(x => x != null && !access.Allows(x.Id, null)))
        {
            scenario.VmCredentials = [];
        }
    }

    private async Task RedactScenarioTemplatesAsync(IReadOnlyList<SAVM.ScenarioTemplate> scenarioTemplates, CancellationToken cancellationToken)
    {
        var access = await authorizationService.GetSecretAccessAsync(cancellationToken);
        foreach (var scenarioTemplate in scenarioTemplates.Where(x => x != null && !access.Allows(null, x.Id)))
        {
            scenarioTemplate.VmCredentials = [];
        }
    }
}
