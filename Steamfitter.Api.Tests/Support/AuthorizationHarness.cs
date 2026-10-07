// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// Wired as AuthorizationPolicyExtensions.AddAuthorizationPolicy registers it: the System, Scenario and
// ScenarioTemplate permission handlers. UserAccessHandler exists but production registers none.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Steamfitter.Api.Data;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Infrastructure.Identity;

namespace Steamfitter.Api.Tests.Support;

/// <summary>The authorization stack wired as production wires it, for testing handlers directly.</summary>
public static class AuthorizationHarness
{
    /// <summary>The framework authorization service with the app's handlers registered.</summary>
    public static IAuthorizationService CreateFrameworkAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationHandler, SystemPermissionHandler>();
        services.AddSingleton<IAuthorizationHandler, ScenarioPermissionHandler>();
        services.AddSingleton<IAuthorizationHandler, ScenarioTemplatePermissionHandler>();

        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    /// <summary>
    /// Steamfitter's own <see cref="ISteamfitterAuthorizationService"/>, reading <paramref name="user"/>
    /// through the real <see cref="IdentityResolver"/> and resolving resource ids through <paramref name="db"/>.
    /// </summary>
    public static ISteamfitterAuthorizationService CreateSteamfitterAuthorizationService(ClaimsPrincipal user, SteamfitterContext db)
    {
        var framework = CreateFrameworkAuthorizationService();
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = user } };

        return new AuthorizationService(framework, new IdentityResolver(accessor, framework), db);
    }

    /// <summary>
    /// Runs a requirement through a handler directly and returns the resulting context.
    /// </summary>
    public static async Task<AuthorizationHandlerContext> HandleAsync<TRequirement>(
        IAuthorizationHandler handler,
        TRequirement requirement,
        ClaimsPrincipal user,
        object resource = null)
        where TRequirement : IAuthorizationRequirement
    {
        var context = new AuthorizationHandlerContext([requirement], user, resource);
        await handler.HandleAsync(context);

        return context;
    }
}
