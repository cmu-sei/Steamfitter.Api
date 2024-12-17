// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Steamfitter.Api.Data.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Steamfitter.Api.Infrastructure.Authorization
{
    public class SystemPermissionRequirement : IAuthorizationRequirement
    {
        public SystemPermission[] RequiredPermissions;

        public SystemPermissionRequirement(SystemPermission[] requiredPermissions)
        {
            RequiredPermissions = requiredPermissions;
        }
    }

    public class SystemPermissionsHandler : AuthorizationHandler<SystemPermissionRequirement>, IAuthorizationHandler
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SystemPermissionRequirement requirement)
        {
            if (context.User == null)
            {
                context.Fail();
            }
            else if (requirement.RequiredPermissions == null || requirement.RequiredPermissions.Length == 0)
            {
                context.Succeed(requirement);
            }
            else if (requirement.RequiredPermissions.Any(p => context.User.HasClaim(AuthorizationConstants.PermissionClaimType, p.ToString())))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}