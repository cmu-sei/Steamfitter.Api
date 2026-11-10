// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Steamfitter.Api.Data.Models;
using Steamfitter.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Steamfitter.Api.Infrastructure.Mappings;

/// <summary>Scenario and scenario template memberships' mappings.</summary>
[Mapper]
public static partial class MembershipMapper
{
    public static partial ScenarioMembership ToScenarioMembership(this ScenarioMembershipEntity source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial ScenarioMembershipEntity ToEntity(this ScenarioMembership source);

    public static partial ScenarioTemplateMembership ToScenarioTemplateMembership(this ScenarioTemplateMembershipEntity source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial ScenarioTemplateMembershipEntity ToEntity(this ScenarioTemplateMembership source);
}
