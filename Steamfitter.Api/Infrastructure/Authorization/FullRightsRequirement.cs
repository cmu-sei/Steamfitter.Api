// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using STT = System.Threading.Tasks;

namespace Steamfitter.Api.Infrastructure.Authorization
{
    public class FullRightsRequirement : IAuthorizationRequirement
    {
    }

    public class FullRightsHandler : AuthorizationHandler<FullRightsRequirement>, IAuthorizationHandler
    {
        protected override STT.Task HandleRequirementAsync(AuthorizationHandlerContext context, FullRightsRequirement requirement)
        {
            if(context.User.HasClaim(c => c.Type == SteamfitterClaimTypes.SystemAdmin.ToString()))
            {
                context.Succeed(requirement);
            }

            return STT.Task.CompletedTask;
        }
    }
}

