// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Steamfitter.Api.Data.Models;
using Steamfitter.Api.ViewModels;
using Riok.Mapperly.Abstractions;

namespace Steamfitter.Api.Infrastructure.Mappings;

/// <summary>Groups' and group memberships' mappings. The group view model has no description, so the stored one is kept on update.</summary>
[Mapper]
public static partial class GroupMapper
{
    public static partial Group ToGroup(this GroupEntity source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial GroupEntity ToEntity(this Group source);

    /// <remarks>Writes the body's id onto the row as well, so an id other than the route's fails the save.</remarks>
    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial void ApplyTo(this Group source, GroupEntity target);

    public static partial GroupMembership ToGroupMembership(this GroupMembershipEntity source);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public static partial GroupMembershipEntity ToEntity(this GroupMembership source);
}
