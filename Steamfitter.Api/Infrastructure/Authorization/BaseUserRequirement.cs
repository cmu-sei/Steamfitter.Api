// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using System;
using System.Security.Claims;
using STT = System.Threading.Tasks;

namespace Steamfitter.Api.Infrastructure.Authorization
{
    public class BaseUserRequirement : IAuthorizationRequirement
    {
    }

    public class BaseUserHandler : AuthorizationHandler<BaseUserRequirement>, IAuthorizationHandler
    {
        protected override STT.Task HandleRequirementAsync(AuthorizationHandlerContext context, BaseUserRequirement requirement)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                context.Succeed(requirement);
            }

            return STT.Task.CompletedTask;
        }
    }
}

