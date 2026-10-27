// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Steamfitter.Api.Data;

namespace Steamfitter.Api.ViewModels
{
    public static class ScenarioTemplateExportConstants
    {
        /// <summary>
        /// The name of the json entry looked for inside an uploaded zip
        /// </summary>
        public const string ExportFileName = "scenario-templates.json";

        /// <summary>
        /// The export file name, without extension, when several Scenario Templates are exported
        /// </summary>
        public const string DefaultFileName = "scenario-templates";
    }

    /// <summary>
    /// A Scenario Template and its Task tree, as written to an export file.
    /// Tasks are a flat list; the tree is rebuilt from TriggerTaskId.
    /// </summary>
    public class ScenarioTemplateExport
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? DurationHours { get; set; }
        public Guid? DefaultVmCredentialId { get; set; }
        public List<VmCredentialExport> VmCredentials { get; set; } = new();
        public List<TaskExport> Tasks { get; set; } = new();
    }

    public class VmCredentialExport
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Description { get; set; }
    }

    public class TaskExport
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public TaskAction Action { get; set; }
        public string VmMask { get; set; }
        public string ApiUrl { get; set; }
        public Dictionary<string, string> ActionParameters { get; set; }
        /// <summary>
        /// Only set when the stored action parameters are not a valid json object,
        /// so that they survive the round trip unchanged
        /// </summary>
        public string InputString { get; set; }
        public string ExpectedOutput { get; set; }
        public int ExpirationSeconds { get; set; }
        public int DelaySeconds { get; set; }
        public int IntervalSeconds { get; set; }
        public int Iterations { get; set; }
        public TaskIterationTermination IterationTermination { get; set; }
        /// <summary>
        /// The parent Task within the same Scenario Template. Null for a root Task.
        /// </summary>
        public Guid? TriggerTaskId { get; set; }
        public TaskTrigger TriggerCondition { get; set; }
        public int Score { get; set; }
        public bool UserExecutable { get; set; }
        public bool Repeatable { get; set; }
    }

    public class ScenarioTemplateImportForm
    {
        /// <summary>
        /// A json file produced by the Scenario Template export, or a zip containing one
        /// </summary>
        [Required]
        public IFormFile File { get; set; }
    }

    public class ImportScenarioTemplateFailure
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public string Reason { get; set; }
    }

    public class ImportScenarioTemplatesResult
    {
        /// <summary>
        /// The Scenario Templates that were created
        /// </summary>
        public List<ScenarioTemplate> ScenarioTemplates { get; set; } = new();

        /// <summary>
        /// Scenario Templates in the file that were not imported, and why
        /// </summary>
        public List<ImportScenarioTemplateFailure> Failures { get; set; } = new();

        /// <summary>
        /// Notes on imported Scenario Templates: a template renamed because its name was in use,
        /// or references to VMs that are specific to the environment the file was exported from
        /// </summary>
        public List<string> Warnings { get; set; } = new();
    }
}
