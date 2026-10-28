// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Steamfitter.Api.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Steamfitter.Api.ViewModels
{
    /// <summary>
    /// The fields a Task's create and edit requests share. The owning Scenario or Scenario Template and the
    /// trigger Task are not among them: only <see cref="TaskCreate"/> names them, so an edit cannot move a
    /// Task to another Scenario, Scenario Template or trigger. Moving goes through PUT tasks/{id}/move.
    /// The User and the current iteration are set by the server.
    /// </summary>
    public abstract class TaskFields
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public TaskAction Action { get; set; }
        public string VmMask { get; set; }
        public List<Guid> VmList { get; set; }
        public string ApiUrl { get; set; }
        public Dictionary<string, string> ActionParameters { get; set; }
        public string ExpectedOutput { get; set; }
        public int ExpirationSeconds { get; set; }
        public int DelaySeconds { get; set; }
        public int IntervalSeconds { get; set; }
        public int Iterations { get; set; }
        public TaskIterationTermination IterationTermination { get; set; }
        public TaskTrigger TriggerCondition { get; set; }
        public int Score { get; set; }
        public bool UserExecutable { get; set; }
        public bool Repeatable { get; set; }
        public bool Executable { get; set; }
    }

    public class TaskCreate : TaskFields, IValidatableObject
    {
        /// <summary>The Scenario the Task belongs to. Exactly one of ScenarioId and ScenarioTemplateId is required.</summary>
        public Guid? ScenarioId { get; set; }

        /// <summary>The Scenario Template the Task belongs to. Exactly one of ScenarioId and ScenarioTemplateId is required.</summary>
        public Guid? ScenarioTemplateId { get; set; }

        /// <summary>A Task of the same Scenario or Scenario Template whose completion triggers this one.</summary>
        public Guid? TriggerTaskId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if ((ScenarioId == null) == (ScenarioTemplateId == null))
                yield return new ValidationResult("A Task must belong to exactly one Scenario or Scenario Template.", [nameof(ScenarioId), nameof(ScenarioTemplateId)]);
        }
    }

    public class TaskEdit : TaskFields
    {
    }
}
