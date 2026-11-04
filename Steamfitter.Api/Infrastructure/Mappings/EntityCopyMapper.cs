// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Steamfitter.Api.Data.Models;
using Riok.Mapperly.Abstractions;

namespace Steamfitter.Api.Infrastructure.Mappings;

/// <summary>Copies of stored tasks, scenarios, templates and credentials, for the copy requests.</summary>
/// <remarks>
/// A mapping between two values of the same type returns the source itself unless the mapper deep-clones,
/// so the copies have a mapper of their own. Only scalar members are left to copy: ids and navigations are
/// ignored, and the caller places the copy.
/// </remarks>
[Mapper(UseDeepCloning = true)]
public static partial class EntityCopyMapper
{
    /// <summary>A new task with the original's values; the caller places it and sets its owner and status.</summary>
    /// <remarks>Its subtasks and results are not copied here, and neither are navigations.</remarks>
    [MapperIgnoreTarget(nameof(TaskEntity.Id))]
    [MapperIgnoreTarget(nameof(TaskEntity.ScenarioTemplate))]
    [MapperIgnoreTarget(nameof(TaskEntity.Scenario))]
    [MapperIgnoreTarget(nameof(TaskEntity.TriggerTask))]
    [MapperIgnoreTarget(nameof(TaskEntity.Children))]
    [MapperIgnoreTarget(nameof(TaskEntity.Results))]
    public static partial TaskEntity ToCopy(this TaskEntity source);

    /// <summary>A new scenario with the original's values, its template included.</summary>
    /// <remarks>
    /// A copy gets its own tasks, credentials and memberships; mapping the original's collections would
    /// move whichever of their rows the context tracks onto the copy. Navigations are left out too.
    /// </remarks>
    [MapperIgnoreTarget(nameof(ScenarioEntity.Id))]
    [MapperIgnoreTarget(nameof(ScenarioEntity.Tasks))]
    [MapperIgnoreTarget(nameof(ScenarioEntity.VmCredentials))]
    [MapperIgnoreTarget(nameof(ScenarioEntity.Memberships))]
    [MapperIgnoreTarget(nameof(ScenarioEntity.ScenarioTemplate))]
    public static partial ScenarioEntity ToCopy(this ScenarioEntity source);

    /// <summary>A new template with the original's values.</summary>
    /// <remarks>
    /// A copy gets its own tasks, credentials and memberships, and none of the original's scenarios;
    /// mapping the original's collections would move whichever of their rows the context tracks onto the copy.
    /// </remarks>
    [MapperIgnoreTarget(nameof(ScenarioTemplateEntity.Id))]
    [MapperIgnoreTarget(nameof(ScenarioTemplateEntity.Tasks))]
    [MapperIgnoreTarget(nameof(ScenarioTemplateEntity.Scenarios))]
    [MapperIgnoreTarget(nameof(ScenarioTemplateEntity.VmCredentials))]
    [MapperIgnoreTarget(nameof(ScenarioTemplateEntity.Memberships))]
    public static partial ScenarioTemplateEntity ToCopy(this ScenarioTemplateEntity source);

    /// <summary>A new credential with the original's values, for a copied scenario or template; the caller places it.</summary>
    [MapperIgnoreTarget(nameof(VmCredentialEntity.Id))]
    [MapperIgnoreTarget(nameof(VmCredentialEntity.ScenarioTemplate))]
    [MapperIgnoreTarget(nameof(VmCredentialEntity.Scenario))]
    public static partial VmCredentialEntity ToCopy(this VmCredentialEntity source);
}
