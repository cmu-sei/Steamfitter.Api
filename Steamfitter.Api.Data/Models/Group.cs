// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Steamfitter.Api.Data.Models;

public class GroupEntity : IEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public string Name { get; set; }
    public string Description { get; set; }

    public virtual ICollection<GroupMembershipEntity> Memberships { get; set; } = new List<GroupMembershipEntity>();
    public virtual ICollection<ScenarioTemplateMembershipEntity> ScenarioTemplateMemberships { get; set; } = new List<ScenarioTemplateMembershipEntity>();
    public virtual ICollection<ScenarioMembershipEntity> ScenarioMemberships { get; set; } = new List<ScenarioMembershipEntity>();
}