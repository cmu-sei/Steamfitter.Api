// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steamfitter.Api.Data.Models;

public class SystemRoleEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool AllPermissions { get; set; }
    public bool Immutable { get; set; }

    public List<SystemPermission> Permissions { get; set; }
}

public static class SystemRoleEntityDefaults
{
    public static Guid AdministratorRoleId = new("f35e8fff-f996-4cba-b303-3ba515ad8d2f");
}

public class SystemRoleEntityConfiguration : IEntityTypeConfiguration<SystemRoleEntity>
{
    public void Configure(EntityTypeBuilder<SystemRoleEntity> builder)
    {
        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new SystemRoleEntity
            {
                Id = SystemRoleEntityDefaults.AdministratorRoleId,
                Name = "Administrator",
                AllPermissions = true,
                Immutable = true,
                Permissions = [],
                Description = "Can perform all actions"
            }
        );
    }
}