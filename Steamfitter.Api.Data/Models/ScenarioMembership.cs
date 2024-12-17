// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steamfitter.Api.Data.Models;

public class ScenarioMembershipEntity : IEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid ScenarioId { get; set; }
    public virtual ScenarioEntity Scenario { get; set; }

    public Guid? UserId { get; set; }
    public virtual UserEntity User { get; set; }

    public Guid? GroupId { get; set; }
    public virtual GroupEntity Group { get; set; }

    public Guid RoleId { get; set; } = ScenarioRoleDefaults.ScenarioMemberRoleId;
    public ScenarioRoleEntity Role { get; set; }


    public ScenarioMembershipEntity() { }

    public ScenarioMembershipEntity(Guid scenarioId, Guid? userId, Guid? groupId)
    {
        ScenarioId = scenarioId;
        UserId = userId;
        GroupId = groupId;
    }

    public class ScenarioMembershipEntityConfiguration : IEntityTypeConfiguration<ScenarioMembershipEntity>
    {
        public void Configure(EntityTypeBuilder<ScenarioMembershipEntity> builder)
        {
            builder.HasIndex(e => new { e.ScenarioId, e.UserId, e.GroupId }).IsUnique();

            builder.Property(x => x.RoleId).HasDefaultValue(ScenarioRoleDefaults.ScenarioMemberRoleId);

            builder
                .HasOne(x => x.Scenario)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.ScenarioId);

            builder
                .HasOne(x => x.User)
                .WithMany(x => x.ScenarioMemberships)
                .HasForeignKey(x => x.UserId)
                .HasPrincipalKey(x => x.Id);

            builder
                .HasOne(x => x.Group)
                .WithMany(x => x.ScenarioMemberships)
                .HasForeignKey(x => x.GroupId)
                .HasPrincipalKey(x => x.Id);
        }
    }
}