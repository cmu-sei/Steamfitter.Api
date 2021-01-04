// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Steamfitter.Api.Data.Models
{
    public class TaskEntity : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid? ScenarioTemplateId { get; set; }
        public virtual ScenarioTemplateEntity ScenarioTemplate { get; set; }
        public Guid? ScenarioId { get; set; }
        public virtual ScenarioEntity Scenario { get; set; }
        public Guid? UserId { get; set; }
        public TaskAction Action { get; set; }
        public string VmMask { get; set; }
        public string ApiUrl { get; set; }
        public string InputString { get; set; }
        public string ExpectedOutput { get; set; }
        public int ExpirationSeconds { get; set; }
        public int DelaySeconds { get; set; }
        public int IntervalSeconds { get; set; }
        public int Iterations { get; set; }
        public TaskIterationTermination IterationTermination { get; set; }
        public int CurrentIteration { get; set; }
        public Guid? TriggerTaskId { get; set; }
        public virtual TaskEntity TriggerTask { get; set; }
        public TaskTrigger TriggerCondition { get; set; }
        public virtual ICollection<TaskEntity> Children { get; set; } = new HashSet<TaskEntity>(); // Only immediate children
        public virtual ICollection<ResultEntity> Results { get; set; } = new HashSet<ResultEntity>();
        
    }

    public class TaskConfiguration : IEntityTypeConfiguration<TaskEntity>
    {
        public void Configure(EntityTypeBuilder<TaskEntity> builder)
        {
            builder.HasIndex(d => d.UserId);

            builder
                .HasOne(d => d.TriggerTask)
                .WithMany(d => d.Children)
                .OnDelete(DeleteBehavior.Cascade);
            builder
                .HasOne(d => d.ScenarioTemplate)
                .WithMany(d => d.Tasks)
                .OnDelete(DeleteBehavior.Cascade);
            builder
                .HasOne(d => d.Scenario)
                .WithMany(d => d.Tasks)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

}

