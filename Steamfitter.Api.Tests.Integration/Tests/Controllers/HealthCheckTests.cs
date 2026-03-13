// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using Steamfitter.Api.Tests.Integration.Fixtures;
using TUnit.Core;

namespace Steamfitter.Api.Tests.Integration.Tests.Controllers;

[Category("Integration")]
[ClassDataSource<SteamfitterTestContext>(Shared = SharedType.PerTestSession)]
public class HealthCheckTests(SteamfitterTestContext context)
{
    private readonly HttpClient _client = context.CreateClient();

    [Test]
    public async System.Threading.Tasks.Task GetReadiness_WhenHealthy_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/health/ready");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async System.Threading.Tasks.Task GetLiveliness_WhenHealthy_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/health/live");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
