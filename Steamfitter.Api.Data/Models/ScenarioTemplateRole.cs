// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steamfitter.Api.Data.Models;

public class ScenarioTemplateRoleEntity : IEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public bool AllPermissions { get; set; }

    public List<ScenarioTemplatePermission> Permissions { get; set; }
}

public enum ScenarioTemplatePermission
{
    ViewScenarioTemplate,
    EditScenarioTemplate,
    ManageScenarioTemplate,
    ImportScenarioTemplate
}

public static class ScenarioTemplateRoleEntityDefaults
{
    public static Guid ScenarioTemplateCreatorRoleId = new("1a3f26cd-9d99-4b98-b914-12931e786198");
    public static Guid ScenarioTemplateReadOnlyRoleId = new("39aa296e-05ba-4fb0-8d74-c92cf3354c6f");
    public static Guid ScenarioTemplateMemberRoleId = new("f870d8ee-7332-4f7f-8ee0-63bd07cfd7e4");
}

public class ScenarioTemplateRoleEntityConfiguration : IEntityTypeConfiguration<ScenarioTemplateRoleEntity>
{
    public void Configure(EntityTypeBuilder<ScenarioTemplateRoleEntity> builder)
    {
        builder.HasData(
            new ScenarioTemplateRoleEntity
            {
                Id = ScenarioTemplateRoleEntityDefaults.ScenarioTemplateCreatorRoleId,
                Name = "Manager",
                AllPermissions = true,
                Permissions = [],
                Description = "Can perform all actions on the ScenarioTemplate"
            },
            new ScenarioTemplateRoleEntity
            {
                Id = ScenarioTemplateRoleEntityDefaults.ScenarioTemplateReadOnlyRoleId,
                Name = "Observer",
                AllPermissions = false,
                Permissions = [ScenarioTemplatePermission.ViewScenarioTemplate],
                Description = "Has read only access to the ScenarioTemplate"
            },
            new ScenarioTemplateRoleEntity
            {
                Id = ScenarioTemplateRoleEntityDefaults.ScenarioTemplateMemberRoleId,
                Name = "Member",
                AllPermissions = false,
                Permissions = [
                    ScenarioTemplatePermission.ViewScenarioTemplate,
                    ScenarioTemplatePermission.EditScenarioTemplate,
                    ScenarioTemplatePermission.ImportScenarioTemplate
                ],
                Description = "Has read only access to the ScenarioTemplate"
            }
        );
    }
}