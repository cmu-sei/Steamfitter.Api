// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// SteamfitterContext takes only DbContextOptions<SteamfitterContext>, and production attaches no
// interceptor besides the one AddEventPublishingDbContextFactory adds.

using System;
using Crucible.Common.EntityEvents.Interceptors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Steamfitter.Api.Data;

namespace Steamfitter.Api.Tests.Support;

/// <summary>Builds <see cref="SteamfitterContext"/> instances wired the way production wires them.</summary>
/// <remarks>
/// <see cref="SteamfitterContext"/> extends <c>EventPublishingDbContext</c>, and its
/// <c>PublishEventsAsync</c> resolves <see cref="IMediator"/> and a logger off the settable
/// <c>ServiceProvider</c> property with <c>GetRequiredService</c>. Both must be registered or the first
/// event-publishing save throws.
/// </remarks>
internal static class SteamfitterContextFactory
{
    /// <summary>
    /// The provider a session shares across its contexts, and the substituted mediator tests assert on.
    /// A substitute is right here: each session gets its own, and only its own test reads it.
    /// </summary>
    public static (IServiceProvider Services, IMediator Mediator) CreateServices()
    {
        var mediator = Substitute.For<IMediator>();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(mediator);

        return (services.BuildServiceProvider(), mediator);
    }

    /// <summary>
    /// A context over the given provider configuration, with the entity event interceptor attached so
    /// SaveChanges publishes events exactly as it does in production.
    /// </summary>
    public static SteamfitterContext CreateContext(
        Action<DbContextOptionsBuilder<SteamfitterContext>> configureProvider,
        IServiceProvider services)
    {
        var builder = new DbContextOptionsBuilder<SteamfitterContext>();
        configureProvider(builder);
        builder.AddInterceptors(new EntityEventInterceptor(NullLogger<EntityEventInterceptor>.Instance));

        return new SteamfitterContext(builder.Options) { ServiceProvider = services };
    }
}
