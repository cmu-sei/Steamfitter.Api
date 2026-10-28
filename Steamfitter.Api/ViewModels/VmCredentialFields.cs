// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Steamfitter.Api.ViewModels
{
    /// <summary>
    /// The fields a VmCredential's create and edit requests share. The owning Scenario or Scenario Template
    /// is not among them: only <see cref="VmCredentialCreate"/> names it, so an edit cannot move a
    /// credential to another Scenario or Scenario Template.
    /// </summary>
    public abstract class VmCredentialFields
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Description { get; set; }
    }

    public class VmCredentialCreate : VmCredentialFields, IValidatableObject
    {
        /// <summary>The Scenario Template the credential belongs to. Exactly one of ScenarioTemplateId and ScenarioId is required.</summary>
        public Guid? ScenarioTemplateId { get; set; }

        /// <summary>The Scenario the credential belongs to. Exactly one of ScenarioTemplateId and ScenarioId is required.</summary>
        public Guid? ScenarioId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if ((ScenarioId == null) == (ScenarioTemplateId == null))
                yield return new ValidationResult("A VmCredential must belong to exactly one Scenario or Scenario Template.", [nameof(ScenarioId), nameof(ScenarioTemplateId)]);
        }
    }

    public class VmCredentialEdit : VmCredentialFields
    {
    }
}
