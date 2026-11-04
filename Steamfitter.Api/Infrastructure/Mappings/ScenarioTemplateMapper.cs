// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Steamfitter.Api.Infrastructure.Mappings;

/// <summary>Scenario templates' mappings. Missing credentials are an empty list, never null.</summary>
[Mapper]
public static partial class ScenarioTemplateMapper
{
    /// <remarks>
    /// <see cref="ScenarioTemplate.ScenarioTemplatePermissions"/> describes the caller; the controller
    /// fills it. A template earns no score, so <see cref="ScenarioTemplate.ScoreEarned"/> stays 0.
    /// </remarks>
    [MapperIgnoreTarget(nameof(ScenarioTemplate.ScenarioTemplatePermissions))]
    [MapperIgnoreTarget(nameof(ScenarioTemplate.ScoreEarned))]
    public static partial ScenarioTemplate ToScenarioTemplate(this ScenarioTemplateEntity source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial ScenarioTemplateEntity ToEntity(this ScenarioTemplateForm source);

    /// <remarks>A form without a duration clears the stored one.</remarks>
    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial void ApplyTo(this ScenarioTemplateForm source, ScenarioTemplateEntity target);

    // VmCredentialMapper owns the list.
    private static List<VmCredential> MapVmCredentials(ICollection<VmCredentialEntity> source) => source.ToVmCredentials();
}
