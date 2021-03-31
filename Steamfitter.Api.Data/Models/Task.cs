// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

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
        public int Score { get; set; }
        public int TotalScore { get; set; }
        public int TotalScoreEarned { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.pending;
        public TaskStatus TotalStatus { get; set; } = TaskStatus.pending;
        public bool UserExecutable { get; set; }
        public bool Repeatable { get; set; }
        public virtual ICollection<TaskEntity> Children { get; set; } = new HashSet<TaskEntity>(); // Only immediate children
        public virtual ICollection<ResultEntity> Results { get; set; } = new HashSet<ResultEntity>();
        public int ScoreEarned
        {
            get
            {
                if (Status == TaskStatus.succeeded)
                {
                    return Score;
                }
                else
                {
                    return 0;
                }
            }
        }

        public bool Executable
        {
            get
            {
                if (!Repeatable && TotalStatus == TaskStatus.succeeded)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Recursively calculate and set the Total Score of a Task by rolling up it's children's Scores.
        /// Should only be used in a Serializable Transaction and with a fully loaded Task tree.
        /// </summary>
        /// <returns>The calculated Total Score of the task</returns>
        public int CalculateTotalScore()
        {
            if (Children == null)
                throw new ArgumentException("Task Total Score can only be calculated with all Children loaded.");

            if (!Children.Any())
            {
                TotalScore = Score;
            }
            else
            {
                var successTasks = Children.Where(x => x.TriggerCondition == TaskTrigger.Success);
                var failureTasks = Children.Where(x => x.TriggerCondition == TaskTrigger.Failure);
                var expirationTasks = Children.Where(x => x.TriggerCondition == TaskTrigger.Expiration);
                var completionTasks = Children.Where(x => x.TriggerCondition == TaskTrigger.Completion);

                var completionScore = completionTasks.Select(x => x.CalculateTotalScore()).Sum() +
                    Math.Max(
                        successTasks.Select(x => x.CalculateTotalScore()).Sum(),
                        failureTasks.Select(x => x.CalculateTotalScore()).Sum());

                var expirationScore = expirationTasks.Select(x => x.CalculateTotalScore()).Sum();
                TotalScore = Score + Math.Max(completionScore, expirationScore);
            }

            return TotalScore;
        }

        /// <summary>
        /// Recursively calculate and set the Total Score Earned of a Task by rolling up it's children's Scores Earned.
        /// Should only be used with a fully loaded Task tree.
        /// </summary>
        /// <returns>The calculated Total Score Earned of the task</returns>
        public int CalculateTotalScoreEarned()
        {
            if (Children == null)
                throw new ArgumentException("Task Total Score Earned can only be calculated with all Children loaded.");

            if (!Children.Any())
            {
                TotalScoreEarned = ScoreEarned;
            }
            else
            {
                TotalScoreEarned = ScoreEarned + Children.Select(x => x.CalculateTotalScoreEarned()).Sum();
            }

            return TotalScoreEarned;
        }

        public TaskStatus CalculateTotalStatus()
        {
            if (Children == null)
                throw new ArgumentException("Task Total Status can only be calculated with all Children loaded.");

            if (!Children.Any())
            {
                TotalStatus = Status;
            }
            else
            {
                var allStatuses = new List<TaskStatus>() { Status };
                allStatuses.AddRange(Children.Select(x => x.CalculateTotalStatus()));

                if (allStatuses.Contains(TaskStatus.pending))
                {
                    TotalStatus = TaskStatus.pending;
                }
                else if (allStatuses.Contains(TaskStatus.queued))
                {
                    TotalStatus = TaskStatus.queued;
                }
                else if (allStatuses.Contains(TaskStatus.sent))
                {
                    TotalStatus = TaskStatus.sent;
                }
                else if (allStatuses.Contains(TaskStatus.cancelled))
                {
                    TotalStatus = TaskStatus.cancelled;
                }
                else if (allStatuses.Contains(TaskStatus.error))
                {
                    TotalStatus = TaskStatus.error;
                }
                else if (allStatuses.Contains(TaskStatus.expired))
                {
                    TotalStatus = TaskStatus.expired;
                }
                else if (allStatuses.Contains(TaskStatus.failed))
                {
                    TotalStatus = TaskStatus.failed;
                }
                else if (allStatuses.Contains(TaskStatus.none))
                {
                    TotalStatus = TaskStatus.none;
                }
                else
                {
                    TotalStatus = TaskStatus.succeeded;
                }
            }

            return TotalStatus;
        }

        public bool IsTreeComplete()
        {
            if (Children == null)
                throw new ArgumentException("IsTreeComplete can only be calculated with all Children loaded.");

            if (!Children.Any())
            {
                return IsComplete;
            }
            else
            {
                return Children.Select(x => !x.IsTreeComplete()).Any();
            }
        }

        public bool IsComplete
        {
            get
            {
                return
                    Status != TaskStatus.pending &&
                    Status != TaskStatus.queued &&
                    Status != TaskStatus.sent;
            }
        }

        public void ResetTree(Guid userId)
        {
            if (Children == null)
                throw new ArgumentException("ResetTree can only be calculated with all Children loaded.");

            CurrentIteration = 0;
            Status = TaskStatus.none;
            UserId = userId;

            foreach (var child in Children)
            {
                child.ResetTree(userId);
            }
        }
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
