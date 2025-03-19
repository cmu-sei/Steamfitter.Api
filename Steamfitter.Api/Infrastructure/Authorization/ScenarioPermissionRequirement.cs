// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Steamfitter.Api.Data;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Steamfitter.Api.Infrastructure.Authorization
{
    public class ScenarioPermissionRequirement : IAuthorizationRequirement
    {
        public ScenarioPermission[] RequiredPermissions;
        public Guid ScenarioId;

        public ScenarioPermissionRequirement(
            ScenarioPermission[] requiredPermissions,
            Guid projectId)
        {
            RequiredPermissions = requiredPermissions;
            ScenarioId = projectId;
        }
    }

    public class ScenarioPermissionHandler : AuthorizationHandler<ScenarioPermissionRequirement>, IAuthorizationHandler
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScenarioPermissionRequirement requirement)
        {
            if (context.User == null)
            {
                context.Fail();
            }
            else
            {
                ScenarioPermissionClaim scenarioPermissionsClaim = null;

                var claims = context.User.Claims
                    .Where(x => x.Type == AuthorizationConstants.ScenarioPermissionClaimType)
                    .ToList();

                foreach (var claim in claims)
                {
                    var claimValue = ScenarioPermissionClaim.FromString(claim.Value);
                    if (claimValue.ScenarioId == requirement.ScenarioId)
                    {
                        scenarioPermissionsClaim = claimValue;
                        break;
                    }
                }

                if (scenarioPermissionsClaim == null)
                {
                    context.Fail();
                }
                else if (requirement.RequiredPermissions == null || requirement.RequiredPermissions.Length == 0)
                {
                    context.Succeed(requirement);
                }
                else if (requirement.RequiredPermissions.Any(x => scenarioPermissionsClaim.Permissions.Contains(x)))
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}