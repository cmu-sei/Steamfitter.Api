// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steamfitter.Api.Data.Models
{
    public class UserScenarioEntity
    {
        public UserScenarioEntity() { }

        public UserScenarioEntity(Guid userId, Guid scenarioId)
        {
            UserId = userId;
            ScenarioId = scenarioId;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public UserEntity User { get; set; }

        public Guid ScenarioId { get; set; }
        public ScenarioEntity Scenario { get; set; }
    }

    public class ScenarioUserConfiguration : IEntityTypeConfiguration<UserScenarioEntity>
    {
        public void Configure(EntityTypeBuilder<UserScenarioEntity> builder)
        {
            builder.HasIndex(x => new { x.UserId, x.ScenarioId }).IsUnique();

            builder
                .HasOne(u => u.User)
                .WithMany(p => p.UserScenarios)
                .HasForeignKey(x => x.UserId);
            builder
                .HasOne(u => u.Scenario)
                .WithMany(p => p.Users)
                .HasForeignKey(x => x.ScenarioId);
        }
    }
}
