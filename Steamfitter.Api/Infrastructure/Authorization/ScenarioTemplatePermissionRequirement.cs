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
    public class ScenarioTemplatePermissionRequirement : IAuthorizationRequirement
    {
        public ScenarioTemplatePermission[] RequiredPermissions;
        public Guid ScenarioId;

        public ScenarioTemplatePermissionRequirement(
            ScenarioTemplatePermission[] requiredPermissions,
            Guid projectId)
        {
            RequiredPermissions = requiredPermissions;
            ScenarioId = projectId;
        }
    }

    public class ScenarioTemplatePermissionHandler : AuthorizationHandler<ScenarioTemplatePermissionRequirement>, IAuthorizationHandler
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScenarioTemplatePermissionRequirement requirement)
        {
            if (context.User == null)
            {
                context.Fail();
            }
            else
            {
                ScenarioTemplatePermissionClaim scenarioTemplatePermissionsClaim = null;

                var claims = context.User.Claims
                    .Where(x => x.Type == AuthorizationConstants.ScenarioTemplatePermissionClaimType)
                    .ToList();

                foreach (var claim in claims)
                {
                    var claimValue = ScenarioTemplatePermissionClaim.FromString(claim.Value);
                    if (claimValue.ScenarioTemplateId == requirement.ScenarioId)
                    {
                        scenarioTemplatePermissionsClaim = claimValue;
                        break;
                    }
                }

                if (scenarioTemplatePermissionsClaim == null)
                {
                    context.Fail();
                }
                else if (requirement.RequiredPermissions == null || requirement.RequiredPermissions.Length == 0)
                {
                    context.Succeed(requirement);
                }
                else if (requirement.RequiredPermissions.Any(x => scenarioTemplatePermissionsClaim.Permissions.Contains(x)))
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}