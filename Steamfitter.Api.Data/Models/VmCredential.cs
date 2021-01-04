// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Steamfitter.Api.Data.Models
{
    public class VmCredentialEntity : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public Guid? ScenarioTemplateId { get; set; }
        public virtual ScenarioTemplateEntity ScenarioTemplate { get; set; }
        public Guid? ScenarioId { get; set; }
        public virtual ScenarioEntity Scenario { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Description { get; set; }
    }

    public class VmCredentialConfiguration : IEntityTypeConfiguration<VmCredentialEntity>
    {
        public void Configure(EntityTypeBuilder<VmCredentialEntity> builder)
        {
            builder
                .HasOne(d => d.ScenarioTemplate)
                .WithMany(d => d.VmCredentials)
                .OnDelete(DeleteBehavior.Cascade);
            builder
                .HasOne(d => d.Scenario)
                .WithMany(d => d.VmCredentials)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

}

