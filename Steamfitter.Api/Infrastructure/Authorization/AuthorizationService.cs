// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

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
        CancellationToken cancellationToken) where T : IEntity;

    Task<bool> AuthorizeAsync<T>(
        Guid? resourceId,
        SystemPermission[] requiredSystemPermissions,
        ScenarioTemplatePermission[] requiredScenarioTemplatePermissions,
        CancellationToken cancellationToken) where T : IEntity;

    IEnumerable<Guid> GetAuthorizedScenarioIds();
    IEnumerable<SystemPermission> GetSystemPermissions();
    IEnumerable<ScenarioPermissionClaim> GetScenarioPermissions(Guid? scenarioId = null);
    IEnumerable<ScenarioTemplatePermissionClaim> GetScenarioTemplatePermissions(Guid? scenarioTemplateId = null);
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
        return await HasSystemPermission<IEntity>(requiredSystemPermissions);
    }

    public async Task<bool> AuthorizeAsync<T>(
        Guid? resourceId,
        SystemPermission[] requiredSystemPermissions,
        ScenarioPermission[] requiredScenarioPermissions,
        CancellationToken cancellationToken) where T : IEntity
    {
        var claimsPrincipal = identityResolver.GetClaimsPrincipal();
        bool succeeded = await HasSystemPermission<IEntity>(requiredSystemPermissions);

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
        CancellationToken cancellationToken) where T : IEntity
    {
        var claimsPrincipal = identityResolver.GetClaimsPrincipal();
        bool succeeded = await HasSystemPermission<IEntity>(requiredSystemPermissions);

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

    public IEnumerable<Guid> GetAuthorizedScenarioIds()
    {
        return identityResolver.GetClaimsPrincipal().Claims
            .Where(x => x.Type == AuthorizationConstants.ScenarioPermissionClaimType)
            .Select(x => ScenarioPermissionClaim.FromString(x.Value).ScenarioId)
            .ToList();
    }

    public IEnumerable<SystemPermission> GetSystemPermissions()
    {
        return identityResolver.GetClaimsPrincipal().Claims
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
        SystemPermission[] requiredSystemPermissions) where T : IEntity
    {
        var claimsPrincipal = identityResolver.GetClaimsPrincipal();
        var permissionRequirement = new SystemPermissionRequirement(requiredSystemPermissions);
        var permissionResult = await authService.AuthorizeAsync(claimsPrincipal, null, permissionRequirement);

        return permissionResult.Succeeded;
    }

    private async Task<Guid?> GetScenarioId<T>(Guid resourceId, CancellationToken cancellationToken)
    {
        return typeof(T) switch
        {
            var t when t == typeof(ScenarioEntity) => resourceId,
            var t when t == typeof(TaskEntity) => await GetScenarioIdFromTask(resourceId, cancellationToken),
            var t when t == typeof(ScenarioMembershipEntity) => await GetScenarioIdFromScenarioMembership(resourceId, cancellationToken),
            _ => throw new NotImplementedException($"Handler for type {typeof(T).Name} is not implemented.")
        };
    }

    private async Task<Guid?> GetScenarioTemplateId<T>(Guid resourceId, CancellationToken cancellationToken)
    {
        return typeof(T) switch
        {
            var t when t == typeof(ScenarioTemplateEntity) => resourceId,
            var t when t == typeof(ScenarioEntity) => await GetScenarioTemplateIdFromScenario(resourceId, cancellationToken),
            var t when t == typeof(TaskEntity) => await GetScenarioIdFromTaskTemplate(resourceId, cancellationToken),
            var t when t == typeof(ScenarioMembershipEntity) => await GetScenarioTemplateIdFromScenarioTemplateMembership(resourceId, cancellationToken),
            _ => throw new NotImplementedException($"Handler for type {typeof(T).Name} is not implemented.")
        };
    }

    private async Task<Guid> GetScenarioTemplateIdFromScenario(Guid id, CancellationToken cancellationToken)
    {
        return (Guid)await dbContext.Tasks
            .Where(x => x.Id == id)
            .Select(x => x.ScenarioId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Guid> GetScenarioIdFromTask(Guid id, CancellationToken cancellationToken)
    {
        return (Guid)await dbContext.Tasks
            .Where(x => x.Id == id)
            .Select(x => x.ScenarioId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Guid> GetScenarioIdFromTaskTemplate(Guid id, CancellationToken cancellationToken)
    {
        return (Guid)await dbContext.Tasks
            .Where(x => x.Id == id)
            .Select(x => x.ScenarioTemplateId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Guid> GetScenarioIdFromScenarioMembership(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.ScenarioMemberships
            .Where(x => x.Id == id)
            .Select(x => x.ScenarioId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Guid> GetScenarioTemplateIdFromScenarioTemplateMembership(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.ScenarioTemplateMemberships
            .Where(x => x.Id == id)
            .Select(x => x.ScenarioTemplateId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}