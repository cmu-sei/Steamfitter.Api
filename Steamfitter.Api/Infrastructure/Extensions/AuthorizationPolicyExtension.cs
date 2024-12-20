// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Steamfitter.Api.Infrastructure.Authorization;

namespace Steamfitter.Api.Infrastructure.Extensions
{
    public static class AuthorizationPolicyExtensions
    {
        public static void AddAuthorizationPolicy(this IServiceCollection services, Options.AuthorizationOptions authOptions)
        {
            services.AddAuthorization(options =>
            {
                // Require all scopes in authOptions
                var policyBuilder = new AuthorizationPolicyBuilder().RequireAuthenticatedUser();
                Array.ForEach(authOptions.AuthorizationScope.Split(' '), x => policyBuilder.RequireClaim("scope", x));

                options.DefaultPolicy = policyBuilder.Build();
                options.AddPolicy(nameof(SteamfitterClaimTypes.SystemAdmin), policy => policy.Requirements.Add(new FullRightsRequirement()));
                options.AddPolicy(nameof(SteamfitterClaimTypes.ContentDeveloper), policy => policy.Requirements.Add(new ContentDeveloperRequirement()));
                options.AddPolicy(nameof(SteamfitterClaimTypes.BaseUser), policy => policy.Requirements.Add(new BaseUserRequirement()));
                options.AddPolicy(nameof(SteamfitterClaimTypes.Operator), policy => policy.Requirements.Add(new OperatorRequirement()));
            });

            services.AddSingleton<IAuthorizationHandler, FullRightsHandler>();
            services.AddSingleton<IAuthorizationHandler, ContentDeveloperHandler>();
            services.AddSingleton<IAuthorizationHandler, OperatorHandler>();
            services.AddSingleton<IAuthorizationHandler, BaseUserHandler>();
            services.AddSingleton<IAuthorizationHandler, SystemPermissionHandler>();
            services.AddSingleton<IAuthorizationHandler, ScenarioPermissionHandler>();
            services.AddSingleton<IAuthorizationHandler, ScenarioTemplatePermissionHandler>();
        }


    }
}
