// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Steamfitter.Api.Infrastructure.Mappings;

/// <summary>Scenarios' mappings. Missing credentials are an empty list, never null.</summary>
[Mapper]
public static partial class ScenarioMapper
{
    /// <remarks>
    /// <see cref="Scenario.Users"/> is filled only by the requests that take it, and
    /// <see cref="Scenario.ScenarioPermissions"/> describes the caller; the controller fills it.
    /// </remarks>
    [MapperIgnoreTarget(nameof(Scenario.Users))]
    [MapperIgnoreTarget(nameof(Scenario.ScenarioPermissions))]
    public static partial Scenario ToScenario(this ScenarioEntity source);

    public static partial ScenarioSummary ToScenarioSummary(this ScenarioEntity source);

    /// <summary>A scenario carrying only its summary's members, for a caller who may not see the rest.</summary>
    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial Scenario ToScenario(this ScenarioSummary source);

    /// <remarks>The entity's constructor sets the status, which the request does not carry.</remarks>
    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial ScenarioEntity ToEntity(this ScenarioCreate source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial void ApplyTo(this ScenarioEdit source, ScenarioEntity target);

    private static List<VmCredential> MapVmCredentials(ICollection<VmCredentialEntity> source) =>
        source == null ? [] : source.Select(x => x.ToVmCredential()).ToList();
}
