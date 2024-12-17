// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steamfitter.Api.Data.Models;

public class ScenarioRoleEntity : IEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public bool AllPermissions { get; set; }

    public List<ScenarioPermission> Permissions { get; set; }
}

public enum ScenarioPermission
{
    ViewScenario,
    EditScenario,
    ManageScenario
}

public static class ScenarioRoleDefaults
{
    public static Guid ScenarioCreatorRoleId = new("1a3f26cd-9d99-4b98-b914-12931e786198");
    public static Guid ScenarioReadOnlyRoleId = new("39aa296e-05ba-4fb0-8d74-c92cf3354c6f");
    public static Guid ScenarioMemberRoleId = new("f870d8ee-7332-4f7f-8ee0-63bd07cfd7e4");
}

public class ScenarioRoleConfiguration : IEntityTypeConfiguration<ScenarioRoleEntity>
{
    public void Configure(EntityTypeBuilder<ScenarioRoleEntity> builder)
    {
        builder.HasData(
            new ScenarioRoleEntity
            {
                Id = ScenarioRoleDefaults.ScenarioCreatorRoleId,
                Name = "Manager",
                AllPermissions = true,
                Permissions = [],
                Description = "Can perform all actions on the Scenario"
            },
            new ScenarioRoleEntity
            {
                Id = ScenarioRoleDefaults.ScenarioReadOnlyRoleId,
                Name = "Observer",
                AllPermissions = false,
                Permissions = [ScenarioPermission.ViewScenario],
                Description = "Has read only access to the Scenario"
            },
            new ScenarioRoleEntity
            {
                Id = ScenarioRoleDefaults.ScenarioMemberRoleId,
                Name = "Member",
                AllPermissions = false,
                Permissions = [
                    ScenarioPermission.ViewScenario,
                    ScenarioPermission.EditScenario
                ],
                Description = "Has read only access to the Scenario"
            }
        );
    }
}