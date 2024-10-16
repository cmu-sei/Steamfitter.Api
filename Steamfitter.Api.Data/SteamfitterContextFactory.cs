// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.
using System;
using Microsoft.EntityFrameworkCore;
namespace Steamfitter.Api.Data;
public class SteamfitterContextFactory : IDbContextFactory<SteamfitterContext>
{
    private readonly IDbContextFactory<SteamfitterContext> _pooledFactory;
    private readonly IServiceProvider _serviceProvider;
    public SteamfitterContextFactory(
        IDbContextFactory<SteamfitterContext> pooledFactory,
        IServiceProvider serviceProvider)
    {
        _pooledFactory = pooledFactory;
        _serviceProvider = serviceProvider;
    }
    public SteamfitterContext CreateDbContext()
    {
        var context = _pooledFactory.CreateDbContext();
        // Inject the current scope's ServiceProvider
        context.ServiceProvider = _serviceProvider;
        return context;
    }
}