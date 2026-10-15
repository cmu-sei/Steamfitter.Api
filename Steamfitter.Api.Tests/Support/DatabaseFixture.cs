// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// Steamfitter's migrations live in Steamfitter.Api.Migrations.PostgreSQL. Program.Main has no switch that
// skips InitializeDatabase, so the run-wide factory takes step 1B: the database is static and the host
// gets one throwaway clone of the template for the run (HostDatabase).

using System;
using System.Threading.Tasks;
using Steamfitter.Api.Data;

namespace Steamfitter.Api.Tests.Support;

/// <summary>
/// Owns the PostgreSQL database for the whole test run: starts it on first use and hands out an isolated
/// session per test.
/// </summary>
/// <remarks>
/// PostgreSQL exercises production's actual database, including the <c>if (Database.IsNpgsql())</c>
/// branch of <c>SteamfitterContext.OnModelCreating</c> and the real migration history. A usable Docker
/// daemon is therefore required by every test that takes a database.
/// </remarks>
public sealed class DatabaseFixture : IAsyncLifetime, ITestDatabaseSessionSource<SteamfitterContext>
{
    private static readonly PostgresTestDatabase<SteamfitterContext> _database = new(new()
    {
        Name = "steamfitter",
        TestAssembly = "Steamfitter.Api.Tests",
        // Production computes this as {AssemblyName}.Migrations.{provider} in
        // DatabaseExtensions.UseConfiguredDatabase. Without it EF looks in the context's own assembly and
        // finds none.
        MigrationsAssembly = "Steamfitter.Api.Migrations.PostgreSQL",
        CreateContext = SteamfitterContextFactory.CreateContext,
        CreateServices = SteamfitterContextFactory.CreateServices
    });

    /// <summary>Nothing to do here: the container starts on the first request for a session.</summary>
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public Task<ITestDatabaseSession<SteamfitterContext>> BeginSessionAsync() => _database.BeginSessionAsync();

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    // Step 1B: the host's own database for the run, for Program.Main's InitializeDatabase. Lazy, and
    // blocking only inside ConfigureWebHost, which runs when the first test uses the host, so tests that
    // need no database still run without Docker. Never dropped: the container goes at the end.
    private static readonly Lazy<Task<ITestDatabaseSession<SteamfitterContext>>> _host =
        new(() => _database.BeginSessionAsync());

    public static ITestDatabaseSession<SteamfitterContext> HostDatabase() => _host.Value.GetAwaiter().GetResult();
}
