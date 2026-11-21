// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Hubs;
using Steamfitter.Api.Services;
using Steamfitter.Api.Tests.Support;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Tests.Controllers;

/// <summary>
/// Guest credentials are read only by a caller who may edit the Scenario or Scenario Template they belong to.
/// A caller who may only view it gets a Scenario or Scenario Template without its VM
/// Credentials, and Tasks and Results with their passwords and private keys redacted; SignalR, whose groups take
/// viewers, sends them to no one. A Task edited with the placeholder keeps the secret it has.
/// </summary>
public class SecretRedactionTests(DatabaseFixture fixture, SteamfitterAppFactory factory)
    : ApiTestBase(fixture, factory)
{
    private const string Secret = "guest-secret";
    private const string Key = "-----BEGIN OPENSSH PRIVATE KEY-----";

    private static readonly JsonSerializerOptions ExportJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static string SecretInput(string password = Secret) => JsonSerializer.Serialize(new Dictionary<string, string>
    {
        ["Username"] = "guest",
        ["Password"] = password,
        ["PrivateKey"] = Key,
        ["Command"] = "whoami"
    });

    private static TaskEntity WithSecrets(TaskEntity task, string password = Secret)
    {
        task.InputString = SecretInput(password);
        return task;
    }

    private static VmCredentialEntity Credential(Guid? scenarioId = null, Guid? scenarioTemplateId = null)
    {
        var credential = TestData.VmCredential(scenarioId: scenarioId, scenarioTemplateId: scenarioTemplateId, username: "guest");
        credential.Password = Secret;
        return credential;
    }

    private static void AssertRedacted(Dictionary<string, string> actionParameters)
    {
        Assert.Equal((TaskSecretParameters.Redacted, TaskSecretParameters.Redacted), (actionParameters["Password"], actionParameters["PrivateKey"]));
        Assert.Equal(("guest", "whoami"), (actionParameters["Username"], actionParameters["Command"]));
    }

    private static void AssertNotRedacted(Dictionary<string, string> actionParameters) =>
        Assert.Equal((Secret, Key), (actionParameters["Password"], actionParameters["PrivateKey"]));

    // ---- Scenarios and Scenario Templates ----

    [Fact]
    public async Task A_scenario_is_read_without_its_credentials_by_a_member_given_the_Observer_role()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario, Credential(scenarioId: scenario.Id));
        var actor = await Actor().OnScenario(scenario.Id, roleId: TestData.ScenarioRoles.Observer).SeedAsync();

        var response = await Client(actor).GetAsync($"api/scenarios/{scenario.Id}", Ct);

        var read = await ReadAsync<SAVM.Scenario>(response);
        Assert.Empty(read.VmCredentials);
        Assert.DoesNotContain(Secret, await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task A_scenario_is_read_with_its_credentials_by_a_member_holding_EditScenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario, Credential(scenarioId: scenario.Id));
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario, ScenarioPermission.EditScenario]).SeedAsync();

        var read = await ReadAsync<SAVM.Scenario>(await Client(actor).GetAsync($"api/scenarios/{scenario.Id}", Ct));

        Assert.Equal(Secret, Assert.Single(read.VmCredentials).Password);
    }

    /// <summary>The system Observer role holds every View permission and no Edit one.</summary>
    [Fact]
    public async Task The_scenario_list_leaves_out_the_credentials_for_a_caller_holding_only_ViewScenarios()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario, Credential(scenarioId: scenario.Id));
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        var list = await ReadAsync<List<SAVM.Scenario>>(await Client(actor).GetAsync("api/scenarios", Ct));

        Assert.Empty(list.Single(x => x.Id == scenario.Id).VmCredentials);
    }

    [Fact]
    public async Task The_scenario_list_keeps_the_credentials_for_a_caller_holding_EditScenarios()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario, Credential(scenarioId: scenario.Id));
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios, SystemPermission.EditScenarios).SeedAsync();

        var list = await ReadAsync<List<SAVM.Scenario>>(await Client(actor).GetAsync("api/scenarios", Ct));

        Assert.Equal(Secret, Assert.Single(list.Single(x => x.Id == scenario.Id).VmCredentials).Password);
    }

    [Fact]
    public async Task A_template_is_read_without_its_credentials_by_a_member_holding_only_ViewScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template, Credential(scenarioTemplateId: template.Id));
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate]).SeedAsync();

        var read = await ReadAsync<SAVM.ScenarioTemplate>(await Client(actor).GetAsync($"api/scenarioTemplates/{template.Id}", Ct));

        Assert.Empty(read.VmCredentials);
    }

    [Fact]
    public async Task A_template_is_read_with_its_credentials_by_a_member_holding_EditScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template, Credential(scenarioTemplateId: template.Id));
        var actor = await Actor().OnScenarioTemplate(template.Id,
            permissions: [ScenarioTemplatePermission.ViewScenarioTemplate, ScenarioTemplatePermission.EditScenarioTemplate]).SeedAsync();

        var read = await ReadAsync<SAVM.ScenarioTemplate>(await Client(actor).GetAsync($"api/scenarioTemplates/{template.Id}", Ct));

        Assert.Equal(Secret, Assert.Single(read.VmCredentials).Password);
    }

    // ---- Tasks ----

    [Fact]
    public async Task A_scenarios_tasks_are_read_with_their_secrets_redacted_by_a_member_given_the_Observer_role()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario, WithSecrets(TestData.ScenarioTask(scenario.Id)));
        var actor = await Actor().OnScenario(scenario.Id, roleId: TestData.ScenarioRoles.Observer).SeedAsync();

        var response = await Client(actor).GetAsync($"api/scenarios/{scenario.Id}/tasks", Ct);

        AssertRedacted(Assert.Single(await ReadAsync<List<SAVM.Task>>(response)).ActionParameters);
        Assert.DoesNotContain(Secret, await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task A_scenarios_tasks_are_read_with_their_secrets_by_a_member_holding_EditScenario()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario, WithSecrets(TestData.ScenarioTask(scenario.Id)));
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario, ScenarioPermission.EditScenario]).SeedAsync();

        var tasks = await ReadAsync<List<SAVM.Task>>(await Client(actor).GetAsync($"api/scenarios/{scenario.Id}/tasks", Ct));

        AssertNotRedacted(Assert.Single(tasks).ActionParameters);
    }

    [Fact]
    public async Task A_task_is_read_with_its_secrets_redacted_by_a_caller_holding_only_ViewScenarios()
    {
        var scenario = TestData.Scenario();
        var task = WithSecrets(TestData.ScenarioTask(scenario.Id));
        await Seed(scenario, task);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        var read = await ReadAsync<SAVM.Task>(await Client(actor).GetAsync($"api/tasks/{task.Id}", Ct));

        AssertRedacted(read.ActionParameters);
    }

    [Fact]
    public async Task A_templates_tasks_are_read_with_their_secrets_redacted_by_a_member_holding_only_ViewScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template, WithSecrets(TestData.TemplateTask(template.Id)));
        var actor = await Actor().OnScenarioTemplate(template.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate]).SeedAsync();

        var tasks = await ReadAsync<List<SAVM.Task>>(await Client(actor).GetAsync($"api/scenarioTemplates/{template.Id}/tasks", Ct));

        AssertRedacted(Assert.Single(tasks).ActionParameters);
    }

    /// <summary>An empty secret is not hidden, as there is nothing to hide.</summary>
    [Fact]
    public async Task An_empty_password_is_read_as_empty_by_a_viewer()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario, WithSecrets(TestData.ScenarioTask(scenario.Id), password: ""));
        var actor = await Actor().OnScenario(scenario.Id, roleId: TestData.ScenarioRoles.Observer).SeedAsync();

        var tasks = await ReadAsync<List<SAVM.Task>>(await Client(actor).GetAsync($"api/scenarios/{scenario.Id}/tasks", Ct));

        Assert.Equal("", Assert.Single(tasks).ActionParameters["Password"]);
    }

    /// <summary>An input string the mapper cannot read is returned whole, so it is hidden as a secret is.</summary>
    [Fact]
    public async Task An_unreadable_input_string_is_redacted_for_a_viewer()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id);
        task.InputString = $$"""{"Password":"{{Secret}}","Port":22}""";
        await Seed(scenario, task);
        var actor = await Actor().OnScenario(scenario.Id, roleId: TestData.ScenarioRoles.Observer).SeedAsync();

        var response = await Client(actor).GetAsync($"api/tasks/{task.Id}", Ct);

        Assert.Equal(TaskSecretParameters.Redacted, (await ReadAsync<SAVM.Task>(response)).ActionParameters[TaskSecretParameters.BadInputString]);
        Assert.DoesNotContain(Secret, await response.Content.ReadAsStringAsync(Ct));
    }

    // ---- Results ----

    [Fact]
    public async Task A_scenarios_results_are_read_with_their_secrets_redacted_by_a_member_given_the_Observer_role()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id);
        var result = TestData.Result(task.Id);
        result.InputString = SecretInput();
        await Seed(scenario, task, result);
        var actor = await Actor().OnScenario(scenario.Id, roleId: TestData.ScenarioRoles.Observer).SeedAsync();

        var response = await Client(actor).GetAsync($"api/scenarios/{scenario.Id}/results", Ct);

        AssertRedacted(Assert.Single(await ReadAsync<List<SAVM.Result>>(response)).ActionParameters);
        Assert.DoesNotContain(Secret, await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task A_result_is_read_with_its_secrets_by_a_member_holding_EditScenario()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id);
        var result = TestData.Result(task.Id);
        result.InputString = SecretInput();
        await Seed(scenario, task, result);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario, ScenarioPermission.EditScenario]).SeedAsync();

        var read = await ReadAsync<SAVM.Result>(await Client(actor).GetAsync($"api/results/{result.Id}", Ct));

        AssertNotRedacted(read.ActionParameters);
    }

    /// <summary>A Result whose Task is gone belongs to no Scenario, so only a caller who may edit them all reads its secrets.</summary>
    [Fact]
    public async Task A_result_without_a_task_is_redacted_for_a_caller_holding_only_ViewScenarios_and_EditScenarios()
    {
        var user = await Actor().SeedAsync();
        var result = TestData.Result(null, createdBy: user.Id);
        result.InputString = SecretInput();
        await Seed(result);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios, SystemPermission.EditScenarios).SeedAsync();

        var results = await ReadAsync<List<SAVM.Result>>(await Client(actor).GetAsync($"api/users/{user.Id}/results", Ct));

        AssertRedacted(Assert.Single(results, x => x.Id == result.Id).ActionParameters);
    }

    // ---- Writing back what was read ----

    [Fact]
    public async Task An_edit_sending_the_placeholder_keeps_the_stored_secrets()
    {
        var scenario = TestData.Scenario();
        var task = WithSecrets(TestData.ScenarioTask(scenario.Id));
        await Seed(scenario, task);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario, ScenarioPermission.EditScenario]).SeedAsync();

        var response = await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}", TaskForm("Renamed", new()
        {
            ["Username"] = "guest",
            ["Password"] = TaskSecretParameters.Redacted,
            ["PrivateKey"] = TaskSecretParameters.Redacted,
            ["Command"] = "hostname"
        }), Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        await using var db = NewContext();
        var stored = JsonSerializer.Deserialize<Dictionary<string, string>>((await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct)).InputString);
        Assert.Equal((Secret, Key, "hostname"), (stored["Password"], stored["PrivateKey"], stored["Command"]));
    }

    [Fact]
    public async Task An_edit_sending_a_new_password_saves_it()
    {
        var scenario = TestData.Scenario();
        var task = WithSecrets(TestData.ScenarioTask(scenario.Id));
        await Seed(scenario, task);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario, ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}",
            TaskForm("Changed", new() { ["Username"] = "guest", ["Password"] = "changed" }), Ct));

        await using var db = NewContext();
        var stored = JsonSerializer.Deserialize<Dictionary<string, string>>((await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct)).InputString);
        Assert.Equal("changed", stored["Password"]);
    }

    /// <summary>A placeholder the stored Task has no secret for is not saved as one.</summary>
    [Fact]
    public async Task An_edit_sending_the_placeholder_for_a_secret_the_task_lacks_saves_it_empty()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id);
        await Seed(scenario, task);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario, ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}",
            TaskForm("Guessed", new() { ["Password"] = TaskSecretParameters.Redacted }), Ct));

        await using var db = NewContext();
        var stored = JsonSerializer.Deserialize<Dictionary<string, string>>((await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct)).InputString);
        Assert.Equal("", stored["Password"]);
    }

    /// <summary>
    /// An unread input string, once a save has wrapped it under BadInputString, is redacted under that key too; sent
    /// back as the placeholder, it keeps the wrapped value.
    /// </summary>
    [Fact]
    public async Task An_edit_sending_the_placeholder_for_a_wrapped_unread_input_keeps_it()
    {
        var scenario = TestData.Scenario();
        var task = TestData.ScenarioTask(scenario.Id);
        var unread = $$"""{"Password":"{{Secret}}","Port":22}""";
        task.InputString = JsonSerializer.Serialize(new Dictionary<string, string> { [TaskSecretParameters.BadInputString] = unread });
        await Seed(scenario, task);
        var actor = await Actor().OnScenario(scenario.Id, permissions: [ScenarioPermission.ViewScenario, ScenarioPermission.EditScenario]).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/tasks/{task.Id}",
            TaskForm("Renamed", new() { [TaskSecretParameters.BadInputString] = TaskSecretParameters.Redacted }), Ct));

        await using var db = NewContext();
        var stored = JsonSerializer.Deserialize<Dictionary<string, string>>((await db.Tasks.SingleAsync(x => x.Id == task.Id, Ct)).InputString);
        Assert.Equal(unread, stored[TaskSecretParameters.BadInputString]);
    }

    [Fact]
    public async Task A_create_sending_the_placeholder_is_a_bad_request()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario);

        var form = TaskForm("Copied", new() { ["Password"] = TaskSecretParameters.Redacted });
        form["scenarioId"] = scenario.Id;
        var response = await RootClient.PostAsJsonAsync("api/tasks", form, Ct);

        await AssertStatus(HttpStatusCode.BadRequest, response);
        await using var db = NewContext();
        Assert.False(await db.Tasks.AnyAsync(x => x.ScenarioId == scenario.Id, Ct));
    }

    // ---- SignalR ----

    /// <summary>The groups take viewers, so a broadcast Task carries no secret, though the caller who made it reads them.</summary>
    [Fact]
    public async Task A_task_is_broadcast_with_its_secrets_redacted()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var form = TaskForm("Broadcast", new() { ["Username"] = "guest", ["Password"] = Secret, ["PrivateKey"] = Key, ["Command"] = "whoami" });
        form["scenarioTemplateId"] = template.Id;

        var created = await ReadAsync<SAVM.Task>(await RootClient.PostAsJsonAsync("api/tasks", form, Ct));

        AssertNotRedacted(created.ActionParameters);
        var hub = Factory.Hub<EngineHub>();
        foreach (var group in new[] { template.Id.ToString(), EngineHub.SCENARIO_TEMPLATE_GROUP })
        {
            var sent = hub.ToGroup(group).Where(x => x.Method == EngineMethods.TaskCreated).Select(x => (SAVM.Task)x.Argument).Single(x => x.Id == created.Id);
            AssertRedacted(sent.ActionParameters);
        }
    }

    [Fact]
    public async Task A_scenario_is_broadcast_without_its_credentials()
    {
        var scenario = TestData.Scenario();
        await Seed(scenario, Credential(scenarioId: scenario.Id));

        await AssertStatus(HttpStatusCode.OK, await RootClient.PutAsJsonAsync($"api/scenarios/{scenario.Id}",
            new { name = "Renamed", startDate = scenario.StartDate, endDate = scenario.EndDate }, Ct));

        var sent = Factory.Hub<EngineHub>().ToGroup(EngineHub.SCENARIO_GROUP)
            .Where(x => x.Method == EngineMethods.ScenarioUpdated)
            .Select(x => (SAVM.Scenario)x.Argument)
            .Where(x => x.Id == scenario.Id)
            .ToList();
        Assert.NotEmpty(sent);
        Assert.All(sent, x => Assert.Null(x.VmCredentials));
    }

    [Fact]
    public async Task A_template_is_broadcast_without_its_credentials()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template, Credential(scenarioTemplateId: template.Id));

        await AssertStatus(HttpStatusCode.OK, await RootClient.PutAsJsonAsync($"api/scenarioTemplates/{template.Id}", new { name = "Renamed" }, Ct));

        var sent = Factory.Hub<EngineHub>().ToGroup(template.Id)
            .Where(x => x.Method == EngineMethods.ScenarioTemplateUpdated)
            .Select(x => (SAVM.ScenarioTemplate)x.Argument)
            .ToList();
        Assert.NotEmpty(sent);
        Assert.All(sent, x => Assert.Null(x.VmCredentials));
    }

    // ---- Export ----

    [Fact]
    public async Task Export_leaves_out_the_secrets_of_a_template_the_caller_may_only_view()
    {
        var template = TestData.ScenarioTemplate("Viewed");
        var credential = Credential(scenarioTemplateId: template.Id);
        template.DefaultVmCredentialId = credential.Id;
        await Seed(template, credential, WithSecrets(TestData.TemplateTask(template.Id)));
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        var response = await Client(actor).GetAsync($"api/scenarioTemplates/actions/export?ids={template.Id}", Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        var file = await response.Content.ReadAsStringAsync(Ct);
        Assert.DoesNotContain(Secret, file);
        Assert.DoesNotContain(Key, file);
        var export = JsonSerializer.Deserialize<SAVM.ScenarioTemplateExport>(file, ExportJson);
        Assert.Empty(export.VmCredentials);
        Assert.Null(export.DefaultVmCredentialId);
        var parameters = Assert.Single(export.Tasks).ActionParameters;
        Assert.Equal(("", "", "whoami"), (parameters["Password"], parameters["PrivateKey"], parameters["Command"]));
    }

    [Fact]
    public async Task Export_keeps_the_secrets_only_of_the_templates_the_caller_may_edit()
    {
        var edited = TestData.ScenarioTemplate("Edited");
        var viewed = TestData.ScenarioTemplate("Viewed");
        await Seed(edited, viewed, Credential(scenarioTemplateId: edited.Id), Credential(scenarioTemplateId: viewed.Id),
            WithSecrets(TestData.TemplateTask(edited.Id)), WithSecrets(TestData.TemplateTask(viewed.Id)));
        var actor = await Actor()
            .OnScenarioTemplate(edited.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate, ScenarioTemplatePermission.EditScenarioTemplate])
            .OnScenarioTemplate(viewed.Id, permissions: [ScenarioTemplatePermission.ViewScenarioTemplate])
            .SeedAsync();

        var export = await ReadAsync<List<SAVM.ScenarioTemplateExport>>(
            await Client(actor).GetAsync($"api/scenarioTemplates/actions/export?ids={edited.Id}&ids={viewed.Id}", Ct), ExportJson);

        var mine = export.Single(x => x.Id == edited.Id);
        var theirs = export.Single(x => x.Id == viewed.Id);
        Assert.Equal(Secret, Assert.Single(mine.VmCredentials).Password);
        Assert.Equal(Secret, Assert.Single(mine.Tasks).ActionParameters["Password"]);
        Assert.Empty(theirs.VmCredentials);
        Assert.Equal("", Assert.Single(theirs.Tasks).ActionParameters["Password"]);
    }

    /// <summary>
    /// A Task whose input string could not be read is shown, and saved back, with the whole string under
    /// BadInputString, so a viewer's export empties that too.
    /// </summary>
    [Fact]
    public async Task Export_leaves_out_an_unread_input_string_saved_back_by_an_edit()
    {
        var template = TestData.ScenarioTemplate("Saved back");
        var task = TestData.TemplateTask(template.Id);
        task.InputString = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            [TaskSecretParameters.BadInputString] = $$"""{"Password":"{{Secret}}","Port":22}"""
        });
        await Seed(template, task);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        var response = await Client(actor).GetAsync($"api/scenarioTemplates/actions/export?ids={template.Id}", Ct);

        var file = await response.Content.ReadAsStringAsync(Ct);
        Assert.DoesNotContain(Secret, file);
        var export = JsonSerializer.Deserialize<SAVM.ScenarioTemplateExport>(file, ExportJson);
        Assert.Equal("", Assert.Single(export.Tasks).ActionParameters[TaskSecretParameters.BadInputString]);
    }

    private static Dictionary<string, object> TaskForm(string name, Dictionary<string, string> actionParameters) => new()
    {
        ["name"] = name,
        ["action"] = "guest_process_run",
        ["apiUrl"] = "stackstorm",
        ["vmMask"] = "",
        ["actionParameters"] = actionParameters,
        ["triggerCondition"] = "Manual",
        ["expectedOutput"] = ""
    };
}
