// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data;
using Steamfitter.Api.Tests.Support;

namespace Steamfitter.Api.Tests.Infrastructure.Filters;

/// <summary>Malformed bodies, route values and enum values at the edge of the MVC pipeline.</summary>
public class RequestValidationTests(DatabaseFixture fixture, SteamfitterAppFactory factory) : ApiTestBase(fixture, factory)
{
    [Fact]
    public async Task A_malformed_json_body_is_a_400_validation_problem()
    {
        var response = await RootClient.PostAsync("api/groups", new StringContent("{ not json", Encoding.UTF8, "application/json"), Ct);

        var problem = await AssertProblem(HttpStatusCode.BadRequest, response);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task A_route_id_that_is_not_a_guid_is_a_400_validation_problem()
    {
        await AssertProblem(HttpStatusCode.BadRequest, await RootClient.GetAsync("api/groups/not-a-guid", Ct));
    }

    /// <summary>An action number outside TaskAction is accepted and stored.</summary>
    [Fact]
    public async Task An_out_of_range_task_action_is_stored()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);

        var response = await RootClient.PostAsJsonAsync(
            "api/tasks",
            new { name = "Out of range", scenarioId = scenario.Id, action = 9999, apiUrl = "email", vmMask = "", actionParameters = new { } },
            Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        await using var db = NewContext();
        Assert.Equal(9999, (int)(await db.Tasks.SingleAsync(x => x.Name == "Out of range", Ct)).Action);
    }
}
