// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Steamfitter.Api.Data.Models
{
    public class UserEntity : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        public string Name { get; set; }

        public Guid? RoleId { get; set; }
        public virtual SystemRoleEntity Role { get; set; }

        public ICollection<ScenarioMembershipEntity> ScenarioMemberships { get; set; } = new List<ScenarioMembershipEntity>();
        public ICollection<ScenarioTemplateMembershipEntity> ScenarioTemplateMemberships { get; set; } = new List<ScenarioTemplateMembershipEntity>();
        public ICollection<GroupMembershipEntity> GroupMemberships { get; set; } = new List<GroupMembershipEntity>();
    }

    public class UserConfiguration : IEntityTypeConfiguration<UserEntity>
    {
        public void Configure(EntityTypeBuilder<UserEntity> builder)
        {
            builder.HasIndex(e => e.Id).IsUnique();
        }
    }
}
