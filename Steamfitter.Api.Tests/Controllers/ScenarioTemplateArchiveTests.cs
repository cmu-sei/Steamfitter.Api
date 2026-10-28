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
    public async Task Export_of_one_template_returns_it_as_a_json_object_with_its_tasks_and_credentials()
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
        var export = await ReadAsync<SAVM.ScenarioTemplateExport>(response, ExportJson);
        Assert.Equal(template.Id, export.Id);
        Assert.Equal(["Parent", "Child"], export.Tasks.Select(x => x.Name));
        Assert.Equal(parent.Id, export.Tasks[1].TriggerTaskId);
        Assert.Equal("exported", Assert.Single(export.VmCredentials).Username);
    }

    /// <summary>
    /// A Task whose trigger is in another template, as data from before triggers were kept in one scope can be,
    /// is written as the manual root it acts as, so the export imports.
    /// </summary>
    [Fact]
    public async Task Export_writes_a_task_triggered_from_another_template_as_a_manual_root_that_imports()
    {
        var other = TestData.ScenarioTemplate("Other");
        var foreignTrigger = TestData.TemplateTask(other.Id, "Foreign trigger");
        var template = TestData.ScenarioTemplate("Linked");
        var linked = TestData.TemplateTask(template.Id, "Linked", triggerTaskId: foreignTrigger.Id);
        linked.TriggerCondition = TaskTrigger.Success;
        await Seed(other, foreignTrigger, template, linked);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates, SystemPermission.CreateScenarioTemplates).SeedAsync();

        var response = await Client(actor).GetAsync($"api/scenarioTemplates/actions/export?ids={template.Id}", Ct);
        var file = await response.Content.ReadAsByteArrayAsync(Ct);
        var exported = Assert.Single(JsonSerializer.Deserialize<SAVM.ScenarioTemplateExport>(file, ExportJson).Tasks);
        var result = await ReadAsync<SAVM.ImportScenarioTemplatesResult>(await Import(actor, file, "Linked.json"), ExportJson);

        Assert.Equal((null, TaskTrigger.Manual), (exported.TriggerTaskId, exported.TriggerCondition));
        Assert.Empty(result.Failures);
        Assert.Single(result.ScenarioTemplates);
    }

    [Fact]
    public async Task Export_of_several_templates_returns_a_list_in_scenario_templates_json()
    {
        var first = TestData.ScenarioTemplate("First");
        var second = TestData.ScenarioTemplate("Second");
        await Seed(first, second);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        var response = await Client(actor).GetAsync($"api/scenarioTemplates/actions/export?ids={first.Id}&ids={second.Id}", Ct);

        Assert.Equal("scenario-templates.json", response.Content.Headers.ContentDisposition?.FileName);
        var export = await ReadAsync<List<SAVM.ScenarioTemplateExport>>(response, ExportJson);
        Assert.Equal(["First", "Second"], export.Select(x => x.Name));
    }

    /// <summary>A name is made a valid file name on Windows too, where the download may be saved.</summary>
    [Fact]
    public async Task Export_names_the_file_without_the_characters_Windows_refuses()
    {
        var template = TestData.ScenarioTemplate("Day 1: Recon? <A|B>");
        await Seed(template);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        var response = await Client(actor).GetAsync($"api/scenarioTemplates/actions/export?ids={template.Id}", Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        Assert.Equal("Day-1--Recon---A-B.json", response.Content.Headers.ContentDisposition?.FileName);
    }

    [Fact]
    public async Task Export_with_no_ids_exports_every_template_to_a_caller_holding_ViewScenarioTemplates()
    {
        await Seed(TestData.ScenarioTemplate("Everything A"), TestData.ScenarioTemplate("Everything B"));
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        var response = await Client(actor).GetAsync("api/scenarioTemplates/actions/export", Ct);

        Assert.Equal("scenario-templates.json", response.Content.Headers.ContentDisposition?.FileName);
        var export = await ReadAsync<List<SAVM.ScenarioTemplateExport>>(response, ExportJson);
        Assert.Contains("Everything A", export.Select(x => x.Name));
        Assert.Contains("Everything B", export.Select(x => x.Name));
    }

    [Fact]
    public async Task Export_with_no_ids_is_forbidden_without_ViewScenarioTemplates()
    {
        var actor = await Actor()
            .WithSystemPermissions(SystemPermission.CreateScenarioTemplates)
            .OnNewScenarioTemplate(ScenarioTemplatePermission.ViewScenarioTemplate)
            .SeedAsync();

        await AssertJsonError(HttpStatusCode.Forbidden,
            await Client(actor).GetAsync("api/scenarioTemplates/actions/export", Ct));
    }

    [Fact]
    public async Task Export_of_an_unknown_template_is_not_found()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        await AssertJsonError(HttpStatusCode.NotFound,
            await Client(actor).GetAsync($"api/scenarioTemplates/actions/export?ids={Guid.NewGuid()}", Ct));
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

    /// <summary>A file names no VM for a Task to run on, as a create request cannot: the markers are stored instead.</summary>
    [Fact]
    public async Task Import_stores_the_vm_markers_in_place_of_a_vm_the_file_names()
    {
        var export = Export("Pinned");
        export.Tasks[0].Action = TaskAction.guest_process_run;
        export.Tasks[0].ApiUrl = "vm";
        export.Tasks[0].ActionParameters = new() { ["Moid"] = Guid.NewGuid().ToString(), ["VmName"] = "elsewhere", ["CommandText"] = "whoami" };
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateScenarioTemplates).SeedAsync();

        var result = await ReadAsync<SAVM.ImportScenarioTemplatesResult>(await Import(actor, Json([export]), "Pinned.json"), ExportJson);

        var created = Assert.Single(result.ScenarioTemplates);
        await using var db = NewContext();
        var stored = await db.Tasks.SingleAsync(x => x.ScenarioTemplateId == created.Id && x.Name == "Parent", Ct);
        Assert.Equal(
            new Dictionary<string, string> { ["Moid"] = "{moid}", ["VmName"] = "{VmName}", ["CommandText"] = "whoami" },
            JsonSerializer.Deserialize<Dictionary<string, string>>(stored.InputString));
    }

    /// <summary>A child task counts once, inside its parent's total: root 10 with child 5 scores 15, not 20.</summary>
    [Fact]
    public async Task Import_scores_a_template_with_child_tasks_once_per_task()
    {
        var export = Export("Scored");
        export.Tasks[0].Score = 10;
        export.Tasks[1].Score = 5;
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateScenarioTemplates).SeedAsync();

        var result = await ReadAsync<SAVM.ImportScenarioTemplatesResult>(await Import(actor, Json([export]), "Scored.json"), ExportJson);

        var created = Assert.Single(result.ScenarioTemplates);
        await using var db = NewContext();
        var stored = await db.ScenarioTemplates.Include(x => x.Tasks).SingleAsync(x => x.Id == created.Id, Ct);
        Assert.Equal(15, stored.Score);
        Assert.False(stored.UpdateScores);
        Assert.Equal(15, Assert.Single(stored.Tasks, x => x.Name == "Parent").TotalScore);
        Assert.Equal(5, Assert.Single(stored.Tasks, x => x.Name == "Child").TotalScore);
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
    public async Task Import_reads_a_file_that_starts_with_a_byte_order_mark()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateScenarioTemplates).SeedAsync();

        var result = await ReadAsync<SAVM.ImportScenarioTemplatesResult>(
            await Import(actor, [0xEF, 0xBB, 0xBF, .. Json([Export("Bom")])], "Bom.json"), ExportJson);

        Assert.Empty(result.Failures);
        Assert.Equal("Bom", Assert.Single(result.ScenarioTemplates).Name);
    }

    /// <summary>A null or non-object item fails on its own; the templates around it are still imported.</summary>
    [Fact]
    public async Task Import_reports_an_item_that_is_not_a_template_and_creates_the_others()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateScenarioTemplates).SeedAsync();
        var valid = JsonSerializer.Serialize(Export("Valid"), ExportJson);

        var result = await ReadAsync<SAVM.ImportScenarioTemplatesResult>(
            await Import(actor, System.Text.Encoding.UTF8.GetBytes($"[null, 42, {valid}]"), "Mixed.json"), ExportJson);

        Assert.Equal("Valid", Assert.Single(result.ScenarioTemplates).Name);
        Assert.Equal(["Item 1", "Item 2"], result.Failures.Select(x => x.Name));
    }

    /// <summary>The limit counts what is extracted, so a small zip that inflates past it is refused.</summary>
    [Fact]
    public async Task Import_refuses_a_zip_whose_json_inflates_past_the_limit()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.CreateScenarioTemplates).SeedAsync();
        using var zip = new MemoryStream();
        using (var archive = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true))
        {
            await using var entry = archive.CreateEntry(SAVM.ScenarioTemplateExportConstants.ExportFileName, CompressionLevel.SmallestSize).Open();
            await entry.WriteAsync(new byte[11 * 1024 * 1024], Ct);
        }

        var result = await ReadAsync<SAVM.ImportScenarioTemplatesResult>(await Import(actor, zip.ToArray(), "Bomb.zip"), ExportJson);

        Assert.Empty(result.ScenarioTemplates);
        Assert.Contains("larger than 10 MB", Assert.Single(result.Failures).Reason);
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
