// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Steamfitter.Api.Infrastructure.Authorization;

namespace Steamfitter.Api.Infrastructure.Extensions
{
    public static class AuthorizationPolicyExtensions
    {
        public static void AddAuthorizationPolicy(this IServiceCollection services)
        {
            services.AddAuthorization();
            services.AddSingleton<IAuthorizationHandler, FullRightsHandler>();
            services.AddSingleton<IAuthorizationHandler, ContentDeveloperHandler>();
            services.AddSingleton<IAuthorizationHandler, OperatorHandler>();
            services.AddSingleton<IAuthorizationHandler, BaseUserHandler>();
        }


    }
}
