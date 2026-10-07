// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data;
using Steamfitter.Api.Tests.Support;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Tests.Controllers;

/// <summary>
/// <c>VmCredentialController</c>: credentials belong to a scenario or a scenario template, and each gate
/// takes the view or edit permission on the one the credential (or the request body) names.
/// </summary>
public class VmCredentialControllerTests(DatabaseFixture fixture, SteamfitterAppFactory factory)
    : ApiTestBase(fixture, factory)
{
    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task An_unauthenticated_request_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync($"api/vmCredentials/{Guid.NewGuid()}", Ct));
    }

    [Fact]
    public async Task GetByScenarioTemplateId_returns_the_templates_credentials_to_a_member_holding_ViewScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        var credential = TestData.VmCredential(scenarioTemplateId: template.Id);
        await Seed(template, credential);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate]).SeedAsync();

        var credentials = await ReadAsync<List<SAVM.VmCredential>>(await Client(actor).GetAsync($"api/scenarioTemplates/{template.Id}/vmCredentials", Ct));

        Assert.Equal([credential.Id], credentials.Select(x => x.Id));
    }

    [Fact]
    public async Task GetByScenarioTemplateId_is_forbidden_for_a_caller_holding_ViewScenarioTemplate_only_on_another_template()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().OnNewScenarioTemplate(ScenarioTemplatePermission.ViewScenarioTemplate).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/scenarioTemplates/{template.Id}/vmCredentials", Ct));
    }

    [Fact]
    public async Task GetByScenarioId_returns_the_scenarios_credentials_to_a_member_holding_ViewScenario()
    {
        var scenario = TestData.Scenario();
        var credential = TestData.VmCredential(scenarioId: scenario.Id);
        await Seed(scenario, credential);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        var credentials = await ReadAsync<List<SAVM.VmCredential>>(await Client(actor).GetAsync($"api/scenarios/{scenario.Id}/vmCredentials", Ct));

        Assert.Equal([credential.Id], credentials.Select(x => x.Id));
    }

    [Fact]
    public async Task GetByScenarioId_is_forbidden_for_a_member_holding_only_ViewTasks()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewTasks]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/scenarios/{scenario.Id}/vmCredentials", Ct));
    }

    [Fact]
    public async Task Get_returns_a_scenario_credential_to_a_member_holding_ViewScenario()
    {
        var scenario = TestData.Scenario();
        var credential = TestData.VmCredential(scenarioId: scenario.Id, username: "readable");
        await Seed(scenario, credential);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        var read = await ReadAsync<SAVM.VmCredential>(await Client(actor).GetAsync($"api/vmCredentials/{credential.Id}", Ct));

        Assert.Equal("readable", read.Username);
    }

    [Fact]
    public async Task Get_returns_a_template_credential_to_a_member_holding_ViewScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        var credential = TestData.VmCredential(scenarioTemplateId: template.Id);
        await Seed(template, credential);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync($"api/vmCredentials/{credential.Id}", Ct));
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_ViewScenario_only_on_another_scenario()
    {
        var scenario = TestData.Scenario();
        var credential = TestData.VmCredential(scenarioId: scenario.Id);
        await Seed(scenario, credential);
        var actor = await Actor().OnNewScenario(ScenarioPermission.ViewScenario).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/vmCredentials/{credential.Id}", Ct));
    }

    [Fact]
    public async Task Get_of_an_unknown_credential_is_not_found()
    {
        await AssertJsonError(HttpStatusCode.NotFound, await RootClient.GetAsync($"api/vmCredentials/{Guid.NewGuid()}", Ct));
    }

    [Fact]
    public async Task Create_persists_a_scenario_credential_for_a_member_holding_EditScenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync("api/vmCredentials", new { scenarioId = scenario.Id, username = "created", password = "pw" }, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        await using var db = NewContext();
        var stored = await db.VmCredentials.SingleAsync(x => x.ScenarioId == scenario.Id, Ct);
        Assert.Equal(("created", actor.Id), (stored.Username, stored.CreatedBy));
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_member_holding_only_ViewScenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/vmCredentials", new { scenarioId = scenario.Id, username = "refused" }, Ct));
        await using var db = NewContext();
        Assert.False(await db.VmCredentials.AnyAsync(x => x.ScenarioId == scenario.Id, Ct));
    }

    [Fact]
    public async Task Create_of_a_template_credential_is_forbidden_for_a_member_holding_only_ViewScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/vmCredentials", new { scenarioTemplateId = template.Id, username = "refused" }, Ct));
    }

    [Fact]
    public async Task Update_saves_a_scenario_credential_for_a_member_holding_EditScenario()
    {
        var scenario = TestData.Scenario();
        var credential = TestData.VmCredential(scenarioId: scenario.Id, username: "before");
        await Seed(scenario, credential);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/vmCredentials/{credential.Id}", new { id = credential.Id, scenarioId = scenario.Id, username = "after" }, Ct));

        await using var db = NewContext();
        Assert.Equal("after", (await db.VmCredentials.SingleAsync(x => x.Id == credential.Id, Ct)).Username);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_member_holding_only_ViewScenario()
    {
        var scenario = TestData.Scenario();
        var credential = TestData.VmCredential(scenarioId: scenario.Id, username: "before");
        await Seed(scenario, credential);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/vmCredentials/{credential.Id}", new { id = credential.Id, scenarioId = scenario.Id, username = "after" }, Ct));
        await using var db = NewContext();
        Assert.Equal("before", (await db.VmCredentials.SingleAsync(x => x.Id == credential.Id, Ct)).Username);
    }

    /// <summary>A credential of another scenario is moved into the scenario the body names.</summary>
    [Fact]
    public async Task Update_moves_a_credential_of_another_scenario_into_the_scenario_the_body_names()
    {
        var mine = TestData.Scenario("Mine");
        var theirs = TestData.Scenario("Theirs");
        var credential = TestData.VmCredential(scenarioId: theirs.Id, username: "theirs");
        await Seed(mine, theirs, credential);
        var actor = await Actor().OnScenario(mine.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        var response = await Client(actor).PutAsJsonAsync($"api/vmCredentials/{credential.Id}", new { id = credential.Id, scenarioId = mine.Id, username = "theirs" }, Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        await using var db = NewContext();
        Assert.Equal(mine.Id, (await db.VmCredentials.SingleAsync(x => x.Id == credential.Id, Ct)).ScenarioId);
    }

    [Fact]
    public async Task Delete_removes_a_scenario_credential_for_a_member_holding_EditScenario()
    {
        var scenario = TestData.Scenario();
        var credential = TestData.VmCredential(scenarioId: scenario.Id);
        await Seed(scenario, credential);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/vmCredentials/{credential.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.VmCredentials.AnyAsync(x => x.Id == credential.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_member_holding_only_ViewScenario()
    {
        var scenario = TestData.Scenario();
        var credential = TestData.VmCredential(scenarioId: scenario.Id);
        await Seed(scenario, credential);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/vmCredentials/{credential.Id}", Ct));
        await using var db = NewContext();
        Assert.True(await db.VmCredentials.AnyAsync(x => x.Id == credential.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_EditScenario_only_on_another_scenario()
    {
        var scenario = TestData.Scenario();
        var credential = TestData.VmCredential(scenarioId: scenario.Id);
        await Seed(scenario, credential);
        var actor = await Actor().OnNewScenario(ScenarioPermission.EditScenario).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/vmCredentials/{credential.Id}", Ct));
    }

    [Fact]
    public async Task GetByScenarioTemplateId_returns_the_templates_credentials_to_a_caller_holding_ViewScenarioTemplates()
    {
        var template = TestData.ScenarioTemplate();
        var credential = TestData.VmCredential(scenarioTemplateId: template.Id);
        await Seed(template, credential);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        var credentials = await ReadAsync<List<SAVM.VmCredential>>(await Client(actor).GetAsync($"api/scenarioTemplates/{template.Id}/vmCredentials", Ct));

        Assert.Equal([credential.Id], credentials.Select(x => x.Id));
    }

    [Fact]
    public async Task GetByScenarioId_returns_the_scenarios_credentials_to_a_caller_holding_ViewScenarios()
    {
        var scenario = TestData.Scenario();
        var credential = TestData.VmCredential(scenarioId: scenario.Id);
        await Seed(scenario, credential);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        var credentials = await ReadAsync<List<SAVM.VmCredential>>(await Client(actor).GetAsync($"api/scenarios/{scenario.Id}/vmCredentials", Ct));

        Assert.Equal([credential.Id], credentials.Select(x => x.Id));
    }

    [Fact]
    public async Task Get_returns_a_scenario_credential_to_a_caller_holding_ViewScenarios()
    {
        var scenario = TestData.Scenario();
        var credential = TestData.VmCredential(scenarioId: scenario.Id);
        await Seed(scenario, credential);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync($"api/vmCredentials/{credential.Id}", Ct));
    }

    [Fact]
    public async Task Get_of_a_template_credential_is_forbidden_for_a_caller_holding_ViewScenarioTemplate_only_on_another_template()
    {
        var template = TestData.ScenarioTemplate();
        var credential = TestData.VmCredential(scenarioTemplateId: template.Id);
        await Seed(template, credential);
        var actor = await Actor().OnNewScenarioTemplate(ScenarioTemplatePermission.ViewScenarioTemplate).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/vmCredentials/{credential.Id}", Ct));
    }

    [Fact]
    public async Task Get_of_a_template_credential_is_forbidden_for_a_caller_holding_only_ViewScenarios()
    {
        var template = TestData.ScenarioTemplate();
        var credential = TestData.VmCredential(scenarioTemplateId: template.Id);
        await Seed(template, credential);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/vmCredentials/{credential.Id}", Ct));
    }

    [Fact]
    public async Task Create_persists_a_template_credential_for_a_member_holding_EditScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate]).SeedAsync();

        await AssertStatus(HttpStatusCode.Created, await Client(actor).PostAsJsonAsync("api/vmCredentials", new { scenarioTemplateId = template.Id, username = "created" }, Ct));

        await using var db = NewContext();
        Assert.Equal("created", (await db.VmCredentials.SingleAsync(x => x.ScenarioTemplateId == template.Id, Ct)).Username);
    }

    [Fact]
    public async Task Create_persists_a_scenario_credential_for_a_caller_holding_EditScenarios()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.Created, await Client(actor).PostAsJsonAsync("api/vmCredentials", new { scenarioId = scenario.Id, username = "created" }, Ct));

        await using var db = NewContext();
        Assert.Equal("created", (await db.VmCredentials.SingleAsync(x => x.ScenarioId == scenario.Id, Ct)).Username);
    }

    [Fact]
    public async Task Create_of_a_scenario_credential_is_forbidden_for_a_caller_holding_only_EditScenarioTemplates()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/vmCredentials", new { scenarioId = scenario.Id, username = "refused" }, Ct));
        await using var db = NewContext();
        Assert.False(await db.VmCredentials.AnyAsync(x => x.ScenarioId == scenario.Id, Ct));
    }

    [Fact]
    public async Task Update_saves_a_template_credential_for_a_member_holding_EditScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        var credential = TestData.VmCredential(scenarioTemplateId: template.Id, username: "before");
        await Seed(template, credential);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/vmCredentials/{credential.Id}", new { id = credential.Id, scenarioTemplateId = template.Id, username = "after" }, Ct));

        await using var db = NewContext();
        Assert.Equal("after", (await db.VmCredentials.SingleAsync(x => x.Id == credential.Id, Ct)).Username);
    }

    [Fact]
    public async Task Update_saves_a_template_credential_for_a_caller_holding_EditScenarioTemplates()
    {
        var template = TestData.ScenarioTemplate();
        var credential = TestData.VmCredential(scenarioTemplateId: template.Id, username: "before");
        await Seed(template, credential);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/vmCredentials/{credential.Id}", new { id = credential.Id, scenarioTemplateId = template.Id, username = "after" }, Ct));

        await using var db = NewContext();
        Assert.Equal("after", (await db.VmCredentials.SingleAsync(x => x.Id == credential.Id, Ct)).Username);
    }

    [Fact]
    public async Task Update_of_a_template_credential_is_forbidden_for_a_member_holding_only_ViewScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        var credential = TestData.VmCredential(scenarioTemplateId: template.Id, username: "before");
        await Seed(template, credential);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/vmCredentials/{credential.Id}", new { id = credential.Id, scenarioTemplateId = template.Id, username = "after" }, Ct));
        await using var db = NewContext();
        Assert.Equal("before", (await db.VmCredentials.SingleAsync(x => x.Id == credential.Id, Ct)).Username);
    }

    [Fact]
    public async Task Update_of_a_template_credential_is_forbidden_for_a_caller_holding_only_EditScenarios()
    {
        var template = TestData.ScenarioTemplate();
        var credential = TestData.VmCredential(scenarioTemplateId: template.Id, username: "before");
        await Seed(template, credential);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/vmCredentials/{credential.Id}", new { id = credential.Id, scenarioTemplateId = template.Id, username = "after" }, Ct));
    }

    [Fact]
    public async Task Delete_removes_a_template_credential_for_a_member_holding_EditScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        var credential = TestData.VmCredential(scenarioTemplateId: template.Id);
        await Seed(template, credential);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.EditScenarioTemplate]).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/vmCredentials/{credential.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.VmCredentials.AnyAsync(x => x.Id == credential.Id, Ct));
    }

    [Fact]
    public async Task Delete_removes_a_scenario_credential_for_a_caller_holding_EditScenarios()
    {
        var scenario = TestData.Scenario();
        var credential = TestData.VmCredential(scenarioId: scenario.Id);
        await Seed(scenario, credential);
        var actor = await Actor().WithSystemPermissions(SystemPermission.EditScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/vmCredentials/{credential.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.VmCredentials.AnyAsync(x => x.Id == credential.Id, Ct));
    }

    [Fact]
    public async Task Delete_of_a_template_credential_is_forbidden_for_a_member_holding_only_ViewScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        var credential = TestData.VmCredential(scenarioTemplateId: template.Id);
        await Seed(template, credential);
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate]).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/vmCredentials/{credential.Id}", Ct));
        await using var db = NewContext();
        Assert.True(await db.VmCredentials.AnyAsync(x => x.Id == credential.Id, Ct));
    }

    [Fact]
    public async Task Delete_of_a_scenario_credential_is_forbidden_for_a_caller_holding_only_ViewScenarios()
    {
        var scenario = TestData.Scenario();
        var credential = TestData.VmCredential(scenarioId: scenario.Id);
        await Seed(scenario, credential);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/vmCredentials/{credential.Id}", Ct));
    }

    /// <summary>An update body without the credential's id is answered with a 500.</summary>
    [Fact]
    public async Task Update_answers_a_body_without_the_credentials_id_with_a_server_error()
    {
        var scenario = TestData.Scenario();
        var credential = TestData.VmCredential(scenarioId: scenario.Id, username: "before");
        await Seed(scenario, credential);

        var response = await RootClient.PutAsJsonAsync($"api/vmCredentials/{credential.Id}", new { scenarioId = scenario.Id, username = "after" }, Ct);

        var error = await AssertJsonError(HttpStatusCode.InternalServerError, response);
        Assert.StartsWith("The property 'VmCredentialEntity.Id' is part of a key", error.Detail);
    }
}
