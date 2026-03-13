// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using Shouldly;
using Steamfitter.Api.Tests.Integration.Fixtures;
using Xunit;

namespace Steamfitter.Api.Tests.Integration.Tests.Controllers;

[Trait("Category", "Integration")]
public class HealthCheckTests : IClassFixture<SteamfitterTestContext>
{
    private readonly HttpClient _client;

    public HealthCheckTests(SteamfitterTestContext context)
    {
        _client = context.CreateClient();
    }

    [Fact]
    public async Task GetReadiness_WhenHealthy_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/health/ready");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLiveliness_WhenHealthy_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/health/live");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
