// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Steamfitter.Api.Data.Models
{
    public class ScenarioTemplateEntity : BaseEntity, IEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public virtual ICollection<TaskEntity> Tasks { get; set; } = new HashSet<TaskEntity>();
        public int? DurationHours { get; set; }
        public virtual ICollection<ScenarioEntity> Scenarios { get; set; } = new HashSet<ScenarioEntity>();
        public Guid? DefaultVmCredentialId { get; set; }
        public virtual ICollection<VmCredentialEntity> VmCredentials { get; set; } = new HashSet<VmCredentialEntity>();
        public virtual ICollection<ScenarioTemplateMembershipEntity> Memberships { get; set; } = new List<ScenarioTemplateMembershipEntity>();
        /// <summary>
        /// Flag that denotes if this ScenarioTemplate's Score needs to be Updated
        /// </summary>
        public bool UpdateScores { get; set; }
        public int Score { get; set; }

        public void CalculateScores()
        {
            if (Tasks == null)
            {
                throw new InvalidOperationException("UpdateScores requires Tasks to be loaded");
            }

            var score = 0;

            // calculate total score for all tasks, starting from the root nodes
            foreach (var task in Tasks.Where(x => !x.TriggerTaskId.HasValue))
            {
                task.CalculateTotalScore();
                task.CalculateTotalStatus();

                score += task.TotalScore;
            }

            Score = score;
            UpdateScores = false;
        }
    }
}
