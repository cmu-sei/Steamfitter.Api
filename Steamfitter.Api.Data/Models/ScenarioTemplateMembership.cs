// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steamfitter.Api.Data.Models;

public class ScenarioTemplateMembershipEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid ScenarioTemplateId { get; set; }
    public virtual ScenarioTemplateEntity ScenarioTemplate { get; set; }

    public Guid? UserId { get; set; }
    public virtual UserEntity User { get; set; }

    public Guid? GroupId { get; set; }
    public virtual GroupEntity Group { get; set; }

    public Guid RoleId { get; set; } = ScenarioTemplateRoleEntityDefaults.ScenarioTemplateMemberRoleId;
    public ScenarioTemplateRoleEntity Role { get; set; }


    public ScenarioTemplateMembershipEntity() { }

    public ScenarioTemplateMembershipEntity(Guid scenarioTemplateId, Guid? userId, Guid? groupId)
    {
        ScenarioTemplateId = scenarioTemplateId;
        UserId = userId;
        GroupId = groupId;
    }

    public class ScenarioTemplateMembershipConfiguration : IEntityTypeConfiguration<ScenarioTemplateMembershipEntity>
    {
        public void Configure(EntityTypeBuilder<ScenarioTemplateMembershipEntity> builder)
        {
            builder.HasIndex(e => new { e.ScenarioTemplateId, e.UserId, e.GroupId }).IsUnique();

            builder.Property(x => x.RoleId).HasDefaultValue(ScenarioTemplateRoleEntityDefaults.ScenarioTemplateMemberRoleId);

            builder
                .HasOne(x => x.ScenarioTemplate)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.ScenarioTemplateId);

            builder
                .HasOne(x => x.User)
                .WithMany(x => x.ScenarioTemplateMemberships)
                .HasForeignKey(x => x.UserId)
                .HasPrincipalKey(x => x.Id);

            builder
                .HasOne(x => x.Group)
                .WithMany(x => x.ScenarioTemplateMemberships)
                .HasForeignKey(x => x.GroupId)
                .HasPrincipalKey(x => x.Id);
        }
    }
}