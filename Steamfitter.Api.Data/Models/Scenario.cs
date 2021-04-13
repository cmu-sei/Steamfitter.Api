// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steamfitter.Api.Data.Models
{
    public class ScenarioEntity : BaseEntity
    {
        public ScenarioEntity()
        {
            this.StartDate = DateTime.UtcNow.AddDays(1);
            this.EndDate = this.StartDate.AddMonths(1);
            this.Status = ScenarioStatus.ready;
        }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ScenarioStatus Status { get; set; }
        public bool OnDemand { get; set; }
        public virtual ICollection<TaskEntity> Tasks { get; set; } = new HashSet<TaskEntity>();
        public Guid? ScenarioTemplateId { get; set; }
        public virtual ScenarioTemplateEntity ScenarioTemplate { get; set; }
        public Guid? ViewId { get; set; }
        public string View { get; set; }
        public Guid? DefaultVmCredentialId { get; set; }
        public virtual ICollection<VmCredentialEntity> VmCredentials { get; set; } = new HashSet<VmCredentialEntity>();
    }

    public class ScenarioEntityConfiguration : IEntityTypeConfiguration<ScenarioEntity>
    {
        public void Configure(EntityTypeBuilder<ScenarioEntity> builder)
        {
            builder
                .HasOne(d => d.ScenarioTemplate)
                .WithMany(d => d.Scenarios)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

}
