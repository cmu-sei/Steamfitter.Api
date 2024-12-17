// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
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
        public virtual ICollection<UserScenarioEntity> Users { get; set; } = new HashSet<UserScenarioEntity>();
        public virtual ICollection<ScenarioMembershipEntity> Memberships { get; set; } = new List<ScenarioMembershipEntity>();
        /// <summary>
        /// Flag that denotes if this Scenario's Scores need to be Updated
        /// </summary>
        public bool UpdateScores { get; set; } = true;
        public int Score { get; set; }
        public int ScoreEarned { get; set; }

        public void CalculateScores()
        {
            if (Tasks == null)
            {
                throw new InvalidOperationException("UpdateScores requires Tasks to be loaded");
            }

            var score = 0;
            var scoreEarned = 0;

            // calculate total score for all tasks, starting from the root nodes
            foreach (var task in Tasks.Where(x => !x.TriggerTaskId.HasValue))
            {
                task.CalculateTotalScore();
                task.CalculateTotalScoreEarned();
                task.CalculateTotalStatus();

                score += task.TotalScore;
                scoreEarned += task.TotalScoreEarned;
            }

            Score = score;
            ScoreEarned = scoreEarned;
            UpdateScores = false;
        }

        public void AddUsers(IEnumerable<Guid> userIds)
        {
            if (Users == null)
                throw new InvalidOperationException("Users must be populated before being modified.");

            foreach (var userId in userIds)
            {
                if (!Users.Any(x => x.UserId == userId))
                {
                    Users.Add(new UserScenarioEntity(userId, Id));
                }
            }
        }

        public void RemoveUsers(IEnumerable<Guid> userIds)
        {
            if (Users == null)
                throw new InvalidOperationException("Users must be populated before being modified.");

            foreach (var userId in userIds)
            {
                var user = Users.FirstOrDefault(x => x.UserId == userId);

                if (user != null)
                {
                    Users.Remove(user);
                }
            }
        }
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
