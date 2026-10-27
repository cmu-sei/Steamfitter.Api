// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data;
using Steamfitter.Api.Tests.Support;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Tests.Controllers;

/// <summary>
/// <c>ScenarioTemplateController</c>: <c>api/scenarioTemplates/actions/export</c> and <c>actions/import</c>.
/// The export is a json file; an import makes new copies with new ids, from that json or a zip holding it.
/// </summary>
public class ScenarioTemplateArchiveTests(DatabaseFixture fixture, SteamfitterAppFactory factory)
    : ApiTestBase(fixture, factory)
{
    private static readonly JsonSerializerOptions ExportJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Export_returns_a_json_file_with_the_template_its_tasks_and_credentials()
    {
        var template = TestData.ScenarioTemplate("Exported");
        var parent = TestData.TemplateTask(template.Id, "Parent");
        var child = TestData.TemplateTask(template.Id, "Child", triggerTaskId: parent.Id);
        await Seed(template, parent, child, TestData.VmCredential(scenarioTemplateId: template.Id, username: "exported"));
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        var response = await Client(actor).GetAsync($"api/scenarioTemplates/actions/export?ids={template.Id}", Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Exported.json", response.Content.Headers.ContentDisposition?.FileName);
        var export = Assert.Single(await ReadAsync<List<SAVM.ScenarioTemplateExport>>(response, ExportJson));
        Assert.Equal(template.Id, export.Id);
        Assert.Equal(["Parent", "Child"], export.Tasks.Select(x => x.Name));
        Assert.Equal(parent.Id, export.Tasks[1].TriggerTaskId);
        Assert.Equal("exported", Assert.Single(export.VmCredentials).Username);
    }

    [Fact]
    public async Task Export_is_forbidden_for_a_caller_holding_ViewScenarioTemplate_only_on_another_template()
    {
        var template = TestData.ScenarioTemplate();
        await Seed(template);
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.CreateScenarioTemplates)
            .OnNewScenarioTemplate(ScenarioTemplatePermission.ViewScenarioTemplate)
            .SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden,
            await Client(actor).GetAsync($"api/scenarioTemplates/actions/export?ids={template.Id}", Ct));
    }

    [Fact]
    public async Task Import_creates_a_copy_with_new_ids_and_rebuilds_the_task_tree_and_default_credential()
    {
        var export = Export("Imported");
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateScenarioTemplates).SeedAsync();

        var result = await ReadAsync<SAVM.ImportScenarioTemplatesResult>(await Import(actor, Json([export]), "Imported.json"), ExportJson);

        Assert.Empty(result.Failures);
        var created = Assert.Single(result.ScenarioTemplates);
        Assert.NotEqual(export.Id, created.Id);
        await using var db = NewContext();
        var stored = await db.ScenarioTemplates.Include(x => x.Tasks).Include(x => x.VmCredentials).Include(x => x.Memberships)
            .SingleAsync(x => x.Id == created.Id, Ct);
        Assert.Equal("Imported", stored.Name);
        var parent = Assert.Single(stored.Tasks, x => x.Name == "Parent");
        var child = Assert.Single(stored.Tasks, x => x.Name == "Child");
        Assert.DoesNotContain(stored.Tasks.Select(x => x.Id), export.Tasks.Select(x => x.Id).Contains);
        Assert.Equal(parent.Id, child.TriggerTaskId);
        var credential = Assert.Single(stored.VmCredentials);
        Assert.NotEqual(export.VmCredentials[0].Id, credential.Id);
        Assert.Equal(credential.Id, stored.DefaultVmCredentialId);
        Assert.Equal(TestData.ScenarioTemplateRoles.Manager, Assert.Single(stored.Memberships, x => x.UserId == actor.Id).RoleId);
    }

    /// <summary>Importing the same file twice makes two copies, the second renamed as a copy is.</summary>
    [Fact]
    public async Task Import_renames_a_template_whose_name_is_in_use_and_notes_it()
    {
        await Seed(TestData.ScenarioTemplate("Taken"));
        var actor = await Actor().WithName("Importer").WithSystemPermissions(SystemPermission.CreateScenarioTemplates).SeedAsync();

        var result = await ReadAsync<SAVM.ImportScenarioTemplatesResult>(await Import(actor, Json([Export("Taken")]), "Taken.json"), ExportJson);

        Assert.Equal("Taken - Importer", Assert.Single(result.ScenarioTemplates).Name);
        Assert.Contains(result.Warnings, x => x.Contains("imported as Taken - Importer"));
    }

    [Fact]
    public async Task Import_accepts_a_zip_holding_the_export()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateScenarioTemplates).SeedAsync();
        using var zip = new MemoryStream();
        using (var archive = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true))
        {
            await using var entry = archive.CreateEntry(SAVM.ScenarioTemplateExportConstants.ExportFileName).Open();
            await entry.WriteAsync(Json([Export("Zipped")]), Ct);
        }

        var result = await ReadAsync<SAVM.ImportScenarioTemplatesResult>(await Import(actor, zip.ToArray(), "Zipped.zip"), ExportJson);

        Assert.Equal("Zipped", Assert.Single(result.ScenarioTemplates).Name);
    }

    [Fact]
    public async Task Import_accepts_a_single_template_rather_than_a_list()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateScenarioTemplates).SeedAsync();

        var result = await ReadAsync<SAVM.ImportScenarioTemplatesResult>(
            await Import(actor, JsonSerializer.SerializeToUtf8Bytes(Export("Single"), ExportJson), "Single.json"), ExportJson);

        Assert.Equal("Single", Assert.Single(result.ScenarioTemplates).Name);
    }

    /// <summary>A template that fails validation is reported and not created; the rest of the file still is.</summary>
    [Fact]
    public async Task Import_reports_a_template_whose_trigger_tasks_form_a_cycle_and_creates_the_others()
    {
        var cyclic = Export("Cyclic");
        cyclic.Tasks[0].TriggerTaskId = cyclic.Tasks[1].Id;
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateScenarioTemplates).SeedAsync();

        var result = await ReadAsync<SAVM.ImportScenarioTemplatesResult>(
            await Import(actor, Json([cyclic, Export("Valid")]), "Mixed.json"), ExportJson);

        Assert.Equal("Valid", Assert.Single(result.ScenarioTemplates).Name);
        Assert.All(result.Failures, x => Assert.Equal("Cyclic", x.Name));
        Assert.Contains(result.Failures, x => x.Reason.Contains("cycle"));
        await using var db = NewContext();
        Assert.False(await db.ScenarioTemplates.AnyAsync(x => x.Name == "Cyclic", Ct));
    }

    [Fact]
    public async Task Import_of_a_file_that_is_not_an_export_reports_it_and_creates_nothing()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateScenarioTemplates).SeedAsync();

        var result = await ReadAsync<SAVM.ImportScenarioTemplatesResult>(await Import(actor, "\"text\""u8.ToArray(), "Text.json"), ExportJson);

        Assert.Empty(result.ScenarioTemplates);
        Assert.Equal("Text.json", Assert.Single(result.Failures).Name);
    }

    [Fact]
    public async Task Import_is_forbidden_for_a_caller_holding_only_ViewScenarioTemplates()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Import(actor, Json([Export("Refused")]), "Refused.json"));
        await using var db = NewContext();
        Assert.False(await db.ScenarioTemplates.AnyAsync(x => x.Name == "Refused", Ct));
    }

    /// <summary>A template with a parent and a child task, and a credential that is its default.</summary>
    private static SAVM.ScenarioTemplateExport Export(string name)
    {
        var parentId = Guid.NewGuid();
        var credentialId = Guid.NewGuid();
        return new SAVM.ScenarioTemplateExport
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "An exported template",
            DefaultVmCredentialId = credentialId,
            VmCredentials = [new SAVM.VmCredentialExport { Id = credentialId, Username = "user", Password = "secret" }],
            Tasks =
            [
                Task(parentId, "Parent", null),
                Task(Guid.NewGuid(), "Child", parentId)
            ]
        };
    }

    private static SAVM.TaskExport Task(Guid id, string name, Guid? triggerTaskId) => new()
    {
        Id = id,
        Name = name,
        Action = TaskAction.send_email,
        ApiUrl = "email",
        Iterations = 1,
        TriggerTaskId = triggerTaskId,
        TriggerCondition = triggerTaskId is null ? TaskTrigger.Manual : TaskTrigger.Success
    };

    private static byte[] Json(List<SAVM.ScenarioTemplateExport> templates) =>
        JsonSerializer.SerializeToUtf8Bytes(templates, ExportJson);

    private Task<HttpResponseMessage> Import(TestActor actor, byte[] file, string fileName)
    {
        var content = new MultipartFormDataContent();
        var part = new ByteArrayContent(file);
        part.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(part, "File", fileName);
        return Client(actor).PostAsync("api/scenarioTemplates/actions/import", content, Ct);
    }
}
