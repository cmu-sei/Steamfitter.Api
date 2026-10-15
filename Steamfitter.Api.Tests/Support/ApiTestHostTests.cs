// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// ApiTestHost re-declares registrations Startup makes; each service a test resolves from it is
// constructed here, so a dependency nothing registers fails one test by name.

using Steamfitter.Api.Hubs;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Services;

namespace Steamfitter.Api.Tests.Support;

/// <summary>Tests for <see cref="ApiTestHost"/>: every service the service tests resolve can be built.</summary>
public class ApiTestHostTests(DatabaseFixture fixture) : ServiceTestBase(fixture)
{
    [Fact]
    public void The_task_runner_can_be_built() => Assert.NotNull(RootHost.Resolve<TaskExecutionService>());

    [Fact]
    public void The_maintenance_service_can_be_built() => Assert.NotNull(RootHost.Resolve<TaskMaintenanceService>());

    [Fact]
    public void The_scoring_service_can_be_built() => Assert.NotNull(RootHost.Resolve<IScoringService>());

    [Fact]
    public void The_authorization_service_can_be_built() => Assert.NotNull(RootHost.Resolve<ISteamfitterAuthorizationService>());

    [Fact]
    public void The_hub_can_be_built() => Assert.NotNull(RootHost.Resolve<EngineHub>());
}
