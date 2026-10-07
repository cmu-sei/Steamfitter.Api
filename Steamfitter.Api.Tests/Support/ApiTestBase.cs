// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Steamfitter.Api.Data;

namespace Steamfitter.Api.Tests.Support;

/// <summary>
/// Base class for tests that drive the application over HTTP: the real routes, the real filters, the
/// real claims transformer, the real authorization handlers, over a database no other test can see.
/// </summary>
/// <remarks>
/// Derived classes forward both fixtures:
/// <c>MyTests(DatabaseFixture fixture, SteamfitterAppFactory factory) : ApiTestBase(fixture, factory)</c>.
/// </remarks>
public abstract class ApiTestBase(DatabaseFixture fixture, SteamfitterAppFactory factory)
    : ApiTestBase<SteamfitterContext>(fixture, factory)
{
    protected DatabaseFixture Fixture { get; } = fixture;

    protected SteamfitterAppFactory Factory { get; } = factory;

    /// <summary>
    /// An actor holding every system permission, for the tests that are about what an endpoint does
    /// rather than who may call it. Seeded before each test.
    /// </summary>
    protected TestActor Root { get; private set; } = null!;

    /// <summary>A client that acts as <see cref="Root"/>.</summary>
    protected HttpClient RootClient => Client(Root);

    /// <summary>Starts describing an actor to seed: <c>await Actor().WithSystemPermissions(...).SeedAsync()</c>.</summary>
    protected TestActorBuilder Actor() => new(Db, Ct);

    /// <summary>A client that acts as <paramref name="actor"/>, cached per actor.</summary>
    protected HttpClient Client(TestActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);

        return ClientFor(actor.Id, actor.Name);
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        Root = await Actor().WithName("Root").WithAllSystemPermissions().SeedAsync();
    }
}
