// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using STT = System.Threading.Tasks;

namespace Steamfitter.Api.Infrastructure.Authorization
{
    public class ContentDeveloperRequirement : IAuthorizationRequirement
    {
        public ContentDeveloperRequirement()
        {
        }
    }

    public class ContentDeveloperHandler : AuthorizationHandler<ContentDeveloperRequirement>, IAuthorizationHandler
    {
        protected override STT.Task HandleRequirementAsync(AuthorizationHandlerContext context, ContentDeveloperRequirement requirement)
        {
            if (context.User.HasClaim(c => c.Type == SteamfitterClaimTypes.SystemAdmin.ToString()) ||
                context.User.HasClaim(c => c.Type == SteamfitterClaimTypes.ContentDeveloper.ToString()))
            {
                context.Succeed(requirement);
            }

            return STT.Task.CompletedTask;
        }
    }
}

