// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Steamfitter.Api.Tests.Support;

namespace Steamfitter.Api.Tests.Controllers;

/// <summary>
/// <c>api/health/live</c> and <c>api/health/ready</c>, which need no identity. The startup check turns
/// healthy only once the hosted <c>TaskExecutionService</c> has bootstrapped, and the harness removes it.
/// </summary>
public class HealthControllerTests(DatabaseFixture fixture, SteamfitterAppFactory factory)
    : ApiTestBase(fixture, factory)
{
    [Fact]
    public async Task Liveness_is_healthy_without_an_identity()
    {
        var response = await Client().GetAsync("api/health/live", Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        Assert.Equal("Healthy", await response.Content.ReadFromJsonAsync<string>(Ct));
    }

    [Fact]
    public async Task Readiness_is_unavailable_until_the_task_runner_has_bootstrapped()
    {
        var response = await Client().GetAsync("api/health/ready", Ct);

        await AssertStatus(HttpStatusCode.ServiceUnavailable, response);
        Assert.Equal("Unhealthy", await response.Content.ReadFromJsonAsync<string>(Ct));
    }
}
