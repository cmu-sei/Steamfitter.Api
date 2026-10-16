// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data;
using Steamfitter.Api.Infrastructure.Exceptions;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Infrastructure.Authorization;

/// <summary>
/// Checks that a record a request refers to belongs to the Scenario or Scenario Template the request was
/// authorized for. A reference into another scope is answered the same as a reference to nothing: 404.
/// </summary>
public static class ScopeValidation
{
    /// <summary>Throws a 404 unless the task belongs to the Scenario or Scenario Template given. A null task id passes.</summary>
    public static async Task EnsureTaskIsInParentAsync(Guid? taskId, Guid? scenarioId, Guid? scenarioTemplateId, SteamfitterContext context, CancellationToken ct)
    {
        if (taskId == null)
            return;

        if (!await context.Tasks.AnyAsync(t => t.Id == taskId && t.ScenarioId == scenarioId && t.ScenarioTemplateId == scenarioTemplateId, ct))
            throw new EntityNotFoundException<SAVM.Task>($"Task {taskId} was not found in {Describe(scenarioId, scenarioTemplateId)}.");
    }

    /// <summary>Throws a 404 unless the credential belongs to the Scenario or Scenario Template given. A null credential id passes.</summary>
    public static async Task EnsureVmCredentialIsInParentAsync(Guid? vmCredentialId, Guid? scenarioId, Guid? scenarioTemplateId, SteamfitterContext context, CancellationToken ct)
    {
        if (vmCredentialId == null)
            return;

        if (!await context.VmCredentials.AnyAsync(c => c.Id == vmCredentialId && c.ScenarioId == scenarioId && c.ScenarioTemplateId == scenarioTemplateId, ct))
            throw new EntityNotFoundException<SAVM.VmCredential>($"VmCredential {vmCredentialId} was not found in {Describe(scenarioId, scenarioTemplateId)}.");
    }

    private static string Describe(Guid? scenarioId, Guid? scenarioTemplateId) =>
        scenarioId != null ? $"Scenario {scenarioId}" : $"Scenario Template {scenarioTemplateId}";
}
