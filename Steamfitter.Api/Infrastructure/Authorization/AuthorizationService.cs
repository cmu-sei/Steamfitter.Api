// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Steamfitter.Api.Data;
using Steamfitter.Api.ViewModels;
using Steamfitter.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Infrastructure.Authorization;

public interface ISteamfitterAuthorizationService
{
    Task<bool> AuthorizeAsync(
        SystemPermission[] requiredSystemPermissions,
        CancellationToken cancellationToken);

    Task<bool> AuthorizeAsync<T>(
        Guid? resourceId,
        SystemPermission[] requiredSystemPermissions,
        ScenarioPermission[] requiredScenarioPermissions,
        CancellationToken cancellationToken) where T : IAuthorizationType;

    Task<bool> AuthorizeAsync<T>(
        Guid? resourceId,
        SystemPermission[] requiredSystemPermissions,
        ScenarioTemplatePermission[] requiredScenarioTemplatePermissions,
        CancellationToken cancellationToken) where T : IAuthorizationType;

    /// <summary>The Scenarios on which the caller holds <paramref name="permission"/>; membership alone is not enough.</summary>
    IReadOnlyList<Guid> GetAuthorizedScenarioIds(ScenarioPermission permission);

    /// <summary>The Scenario Templates on which the caller holds <paramref name="permission"/>; membership alone is not enough.</summary>
    IReadOnlyList<Guid> GetAuthorizedScenarioTemplateIds(ScenarioTemplatePermission permission);

    /// <summary>
    /// Whether the caller holds <paramref name="access"/> on the Scenario or the Scenario Template given, exactly
    /// one of which must be. Naming neither or both grants nothing.
    /// </summary>
    Task<bool> AuthorizeScopeAsync(Guid? scenarioId, Guid? scenarioTemplateId, ScopeAccess access, CancellationToken cancellationToken);

    /// <summary>Whether the caller holds <paramref name="access"/> on the Scenario or Scenario Template the Task belongs to.</summary>
    Task<bool> AuthorizeTaskScopeAsync(Guid taskId, ScopeAccess access, CancellationToken cancellationToken);

    /// <summary>Whether the caller holds <paramref name="access"/> on the Scenario or Scenario Template of the Result's Task.</summary>
    Task<bool> AuthorizeResultScopeAsync(Guid resultId, ScopeAccess access, CancellationToken cancellationToken);

    /// <summary>Whether the caller holds <paramref name="access"/> on the Scenario or Scenario Template the VM Credential belongs to.</summary>
    Task<bool> AuthorizeVmCredentialScopeAsync(Guid vmCredentialId, ScopeAccess access, CancellationToken cancellationToken);

    /// <summary>Whether any of the given Scenarios belongs to the Player View.</summary>
    Task<bool> AnyScenarioOfViewAsync(Guid viewId, IReadOnlyCollection<Guid> scenarioIds, CancellationToken cancellationToken);
    IEnumerable<SystemPermission> GetSystemPermissions();
    IEnumerable<ScenarioPermissionClaim> GetScenarioPermissions(Guid? scenarioId = null);
    IEnumerable<ScenarioTemplatePermissionClaim> GetScenarioTemplatePermissions(Guid? scenarioTemplateId = null);
}

/// <summary>The access a request needs on the Scenario or Scenario Template a record belongs to.</summary>
public enum ScopeAccess
{
    View,
    Edit
}

public class AuthorizationService(
    IAuthorizationService authService,
    IIdentityResolver identityResolver,
    SteamfitterContext dbContext) : ISteamfitterAuthorizationService
{
    public async Task<bool> AuthorizeAsync(
        SystemPermission[] requiredSystemPermissions,
        CancellationToken cancellationToken)
    {
        return await HasSystemPermission<IAuthorizationType>(requiredSystemPermissions);
    }

    public async Task<bool> AuthorizeAsync<T>(
        Guid? resourceId,
        SystemPermission[] requiredSystemPermissions,
        ScenarioPermission[] requiredScenarioPermissions,
        CancellationToken cancellationToken) where T : IAuthorizationType
    {
        var claimsPrincipal = identityResolver.GetClaimsPrincipal();
        bool succeeded = await HasSystemPermission<IAuthorizationType>(requiredSystemPermissions);

        if (!succeeded && resourceId.HasValue)
        {
            var scenarioId = await GetScenarioId<T>(resourceId.Value, cancellationToken);

            if (scenarioId != null)
            {
                var scenarioPermissionRequirement = new ScenarioPermissionRequirement(requiredScenarioPermissions, scenarioId.Value);
                var scenarioPermissionResult = await authService.AuthorizeAsync(claimsPrincipal, null, scenarioPermissionRequirement);

                succeeded = scenarioPermissionResult.Succeeded;
            }

        }

        return succeeded;
    }

    public async Task<bool> AuthorizeAsync<T>(
        Guid? resourceId,
        SystemPermission[] requiredSystemPermissions,
        ScenarioTemplatePermission[] requiredScenarioTemplatePermissions,
        CancellationToken cancellationToken) where T : IAuthorizationType
    {
        var claimsPrincipal = identityResolver.GetClaimsPrincipal();
        bool succeeded = await HasSystemPermission<IAuthorizationType>(requiredSystemPermissions);

        if (!succeeded && resourceId.HasValue)
        {
            var scenarioTemplateId = await GetScenarioTemplateId<T>(resourceId.Value, cancellationToken);

            if (scenarioTemplateId != null)
            {
                var scenarioTemplatePermissionRequirement = new ScenarioTemplatePermissionRequirement(requiredScenarioTemplatePermissions, scenarioTemplateId.Value);
                var scenarioTemplatePermissionResult = await authService.AuthorizeAsync(claimsPrincipal, null, scenarioTemplatePermissionRequirement);

                succeeded = scenarioTemplatePermissionResult.Succeeded;
            }

        }

        return succeeded;
    }

    public async Task<bool> AuthorizeScopeAsync(Guid? scenarioId, Guid? scenarioTemplateId, ScopeAccess access, CancellationToken cancellationToken)
    {
        if ((scenarioId == null) == (scenarioTemplateId == null))
            return false;

        return scenarioId != null
            ? await AuthorizeAsync<Scenario>(scenarioId, [ScenarioSystemPermission(access)], [ScenarioPermissionFor(access)], cancellationToken)
            : await AuthorizeAsync<ScenarioTemplate>(scenarioTemplateId, [ScenarioTemplateSystemPermission(access)], [ScenarioTemplatePermissionFor(access)], cancellationToken);
    }

    public Task<bool> AuthorizeTaskScopeAsync(Guid taskId, ScopeAccess access, CancellationToken cancellationToken) =>
        AuthorizeScopeOfAsync(dbContext.Tasks
            .Where(x => x.Id == taskId)
            .Select(x => new ParentScope(x.ScenarioId, x.ScenarioTemplateId)), access, cancellationToken);

    public Task<bool> AuthorizeResultScopeAsync(Guid resultId, ScopeAccess access, CancellationToken cancellationToken) =>
        AuthorizeScopeOfAsync(dbContext.Results
            .Where(x => x.Id == resultId)
            .Select(x => new ParentScope(x.Task.ScenarioId, x.Task.ScenarioTemplateId)), access, cancellationToken);

    public Task<bool> AuthorizeVmCredentialScopeAsync(Guid vmCredentialId, ScopeAccess access, CancellationToken cancellationToken) =>
        AuthorizeScopeOfAsync(dbContext.VmCredentials
            .Where(x => x.Id == vmCredentialId)
            .Select(x => new ParentScope(x.ScenarioId, x.ScenarioTemplateId)), access, cancellationToken);

    // A record that is not there answers as one the caller may not reach, so that a 403 does not tell that
    // it is there. Only a caller whose system permissions reach every record of its kind learns it is not.
    private async Task<bool> AuthorizeScopeOfAsync(IQueryable<ParentScope> lookup, ScopeAccess access, CancellationToken cancellationToken)
    {
        var scope = await lookup.FirstOrDefaultAsync(cancellationToken);
        if (scope == null)
            return await HasSystemPermission<IAuthorizationType>([ScenarioSystemPermission(access)])
                && await HasSystemPermission<IAuthorizationType>([ScenarioTemplateSystemPermission(access)]);

        return await AuthorizeScopeAsync(scope.ScenarioId, scope.ScenarioTemplateId, access, cancellationToken);
    }

    private static SystemPermission ScenarioSystemPermission(ScopeAccess access) =>
        access == ScopeAccess.Edit ? SystemPermission.EditScenarios : SystemPermission.ViewScenarios;

    private static ScenarioPermission ScenarioPermissionFor(ScopeAccess access) =>
        access == ScopeAccess.Edit ? ScenarioPermission.EditScenario : ScenarioPermission.ViewScenario;

    private static SystemPermission ScenarioTemplateSystemPermission(ScopeAccess access) =>
        access == ScopeAccess.Edit ? SystemPermission.EditScenarioTemplates : SystemPermission.ViewScenarioTemplates;

    private static ScenarioTemplatePermission ScenarioTemplatePermissionFor(ScopeAccess access) =>
        access == ScopeAccess.Edit ? ScenarioTemplatePermission.EditScenarioTemplate : ScenarioTemplatePermission.ViewScenarioTemplate;

    public IReadOnlyList<Guid> GetAuthorizedScenarioIds(ScenarioPermission permission)
    {
        return GetScenarioPermissions()
            .Where(x => x.Permissions.Contains(permission))
            .Select(x => x.ScenarioId)
            .ToList();
    }

    public IReadOnlyList<Guid> GetAuthorizedScenarioTemplateIds(ScenarioTemplatePermission permission)
    {
        return GetScenarioTemplatePermissions()
            .Where(x => x.Permissions.Contains(permission))
            .Select(x => x.ScenarioTemplateId)
            .ToList();
    }

    public async Task<bool> AnyScenarioOfViewAsync(Guid viewId, IReadOnlyCollection<Guid> scenarioIds, CancellationToken cancellationToken)
    {
        return await dbContext.Scenarios.AnyAsync(x => x.ViewId == viewId && scenarioIds.Contains(x.Id), cancellationToken);
    }

    public IEnumerable<SystemPermission> GetSystemPermissions()
    {
        var principal = identityResolver.GetClaimsPrincipal();
        var claims = principal.Claims;
        var permissions = claims
           .Where(x => x.Type == AuthorizationConstants.PermissionClaimType)
           .Select(x =>
           {
               if (Enum.TryParse<SystemPermission>(x.Value, out var permission))
                   return permission;

               return (SystemPermission?)null;
           })
           .Where(x => x.HasValue)
           .Select(x => x.Value)
           .ToList();
        return permissions;
    }

    public IEnumerable<ScenarioPermissionClaim> GetScenarioPermissions(Guid? scenarioId = null)
    {
        var permissions = identityResolver.GetClaimsPrincipal().Claims
           .Where(x => x.Type == AuthorizationConstants.ScenarioPermissionClaimType)
           .Select(x => ScenarioPermissionClaim.FromString(x.Value));

        if (scenarioId.HasValue)
        {
            permissions = permissions.Where(x => x.ScenarioId == scenarioId.Value);
        }

        return permissions;
    }

    public IEnumerable<ScenarioTemplatePermissionClaim> GetScenarioTemplatePermissions(Guid? scenarioTemplateId = null)
    {
        var permissions = identityResolver.GetClaimsPrincipal().Claims
           .Where(x => x.Type == AuthorizationConstants.ScenarioTemplatePermissionClaimType)
           .Select(x => ScenarioTemplatePermissionClaim.FromString(x.Value));

        if (scenarioTemplateId.HasValue)
        {
            permissions = permissions.Where(x => x.ScenarioTemplateId == scenarioTemplateId.Value);
        }

        return permissions;
    }

    private async Task<bool> HasSystemPermission<T>(
        SystemPermission[] requiredSystemPermissions) where T : IAuthorizationType
    {
        var claimsPrincipal = identityResolver.GetClaimsPrincipal();
        var permissionRequirement = new SystemPermissionRequirement(requiredSystemPermissions);
        var permissionResult = await authService.AuthorizeAsync(claimsPrincipal, null, permissionRequirement);

        return permissionResult.Succeeded;
    }

    // A record that is not there resolves to null, as one that is there but has no scenario (or no
    // template) does, and either grants nothing: a missing record answers 403, the same as one the
    // caller may not reach, so that the answer does not tell which ids exist.
    private async Task<Guid?> GetScenarioId<T>(Guid resourceId, CancellationToken cancellationToken)
    {
        return typeof(T) switch
        {
            var t when t == typeof(Scenario) => await GetScenarioIdFromScenario(resourceId, cancellationToken),
            var t when t == typeof(SAVM.Task) => await GetScenarioIdFromTask(resourceId, cancellationToken),
            var t when t == typeof(ScenarioMembership) => await GetScenarioIdFromScenarioMembership(resourceId, cancellationToken),
            var t when t == typeof(Result) => await GetScenarioIdFromResult(resourceId, cancellationToken),
            _ => throw new NotImplementedException($"Handler for type {typeof(T).Name} is not implemented.")
        };
    }

    private async Task<Guid?> GetScenarioTemplateId<T>(Guid resourceId, CancellationToken cancellationToken)
    {
        return typeof(T) switch
        {
            var t when t == typeof(ScenarioTemplate) => await GetScenarioTemplateIdFromScenarioTemplate(resourceId, cancellationToken),
            var t when t == typeof(SAVM.Task) => await GetScenarioTemplateIdFromTask(resourceId, cancellationToken),
            var t when t == typeof(ScenarioTemplateMembership) => await GetScenarioTemplateIdFromScenarioTemplateMembership(resourceId, cancellationToken),
            _ => throw new NotImplementedException($"Handler for type {typeof(T).Name} is not implemented.")
        };
    }

    private async Task<Guid?> GetScenarioIdFromScenario(Guid id, CancellationToken cancellationToken)
    {
        return (await dbContext.Scenarios
            .Where(x => x.Id == id)
            .Select(x => new ScopeRow(x.Id))
            .FirstOrDefaultAsync(cancellationToken))?.Id;
    }

    private async Task<Guid?> GetScenarioIdFromTask(Guid id, CancellationToken cancellationToken)
    {
        return (await dbContext.Tasks
            .Where(x => x.Id == id)
            .Select(x => new ScopeRow(x.ScenarioId))
            .FirstOrDefaultAsync(cancellationToken))?.Id;
    }

    private async Task<Guid?> GetScenarioIdFromScenarioMembership(Guid id, CancellationToken cancellationToken)
    {
        return (await dbContext.ScenarioMemberships
            .Where(x => x.Id == id)
            .Select(x => new ScopeRow(x.ScenarioId))
            .FirstOrDefaultAsync(cancellationToken))?.Id;
    }

    private async Task<Guid?> GetScenarioIdFromResult(Guid id, CancellationToken cancellationToken)
    {
        return (await dbContext.Results
            .Where(x => x.Id == id)
            .Select(x => new ScopeRow(x.Task.ScenarioId))
            .FirstOrDefaultAsync(cancellationToken))?.Id;
    }

    private async Task<Guid?> GetScenarioTemplateIdFromScenarioTemplate(Guid id, CancellationToken cancellationToken)
    {
        return (await dbContext.ScenarioTemplates
            .Where(x => x.Id == id)
            .Select(x => new ScopeRow(x.Id))
            .FirstOrDefaultAsync(cancellationToken))?.Id;
    }

    private async Task<Guid?> GetScenarioTemplateIdFromTask(Guid id, CancellationToken cancellationToken)
    {
        return (await dbContext.Tasks
            .Where(x => x.Id == id)
            .Select(x => new ScopeRow(x.ScenarioTemplateId))
            .FirstOrDefaultAsync(cancellationToken))?.Id;
    }

    private async Task<Guid?> GetScenarioTemplateIdFromScenarioTemplateMembership(Guid id, CancellationToken cancellationToken)
    {
        return (await dbContext.ScenarioTemplateMemberships
            .Where(x => x.Id == id)
            .Select(x => new ScopeRow(x.ScenarioTemplateId))
            .FirstOrDefaultAsync(cancellationToken))?.Id;
    }

    /// <summary>The scope a looked-up record belongs to; null when it belongs to none. No row means no record.</summary>
    private sealed record ScopeRow(Guid? Id);

    /// <summary>The Scenario or Scenario Template a looked-up record belongs to.</summary>
    private sealed record ParentScope(Guid? ScenarioId, Guid? ScenarioTemplateId);

}