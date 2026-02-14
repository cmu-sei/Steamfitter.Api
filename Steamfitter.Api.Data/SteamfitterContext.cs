// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Data.Extensions;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Data;
using System.Collections.Generic;
using MediatR;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

namespace Steamfitter.Api.Data
{
    public class SteamfitterContext : DbContext
    {
        // Needed for EventInterceptor
        public IServiceProvider ServiceProvider;

        // Entity Events collected by EventTransactionInterceptor and published in SaveChanges
        public List<INotification> Events { get; } = [];

        public SteamfitterContext(DbContextOptions<SteamfitterContext> options) : base(options) { }

        public DbSet<TaskEntity> Tasks { get; set; }
        public DbSet<ResultEntity> Results { get; set; }
        public DbSet<ScenarioTemplateEntity> ScenarioTemplates { get; set; }
        public DbSet<ScenarioEntity> Scenarios { get; set; }
        public DbSet<PermissionEntity> Permissions { get; set; }
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<UserPermissionEntity> UserPermissions { get; set; }
        public DbSet<FileEntity> Files { get; set; }
        public DbSet<BondAgent> BondAgents { get; set; }
        public DbSet<VmCredentialEntity> VmCredentials { get; set; }
        public DbSet<SystemRoleEntity> SystemRoles { get; set; }
        public DbSet<ScenarioRoleEntity> ScenarioRoles { get; set; }
        public DbSet<ScenarioMembershipEntity> ScenarioMemberships { get; set; }
        public DbSet<ScenarioTemplateRoleEntity> ScenarioTemplateRoles { get; set; }
        public DbSet<ScenarioTemplateMembershipEntity> ScenarioTemplateMemberships { get; set; }
        public DbSet<GroupEntity> Groups { get; set; }
        public DbSet<GroupMembershipEntity> GroupMemberships { get; set; }
        public DbSet<XApiQueuedStatementEntity> XApiQueuedStatements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurations();

            // Apply PostgreSQL specific options
            if (Database.IsNpgsql())
            {
                modelBuilder.AddPostgresUUIDGeneration();
                modelBuilder.UsePostgresCasing();
            }

        }

        public override int SaveChanges()
        {
            CheckForScoreUpdates(default).Wait();
            var result = base.SaveChanges();
            PublishEvents().Wait();
            return result;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            await CheckForScoreUpdates(ct);
            var result = await base.SaveChangesAsync(ct);
            await PublishEvents(ct);
            return result;
        }

        private async Task PublishEvents(CancellationToken cancellationToken = default)
        {
            // Publish deferred events after transaction is committed and cleared
            if (Events.Count > 0 && ServiceProvider is not null)
            {
                var mediator = ServiceProvider.GetRequiredService<IMediator>();
                var eventsToPublish = Events.ToArray();
                Events.Clear();

                foreach (var evt in eventsToPublish)
                {
                    await mediator.Publish(evt, cancellationToken);
                }
            }
        }

        private async Task CheckForScoreUpdates(CancellationToken ct)
        {
            var addedEntries = ChangeTracker.Entries().Where(x => x.State == EntityState.Added).ToList();
            var modifiedEntries = ChangeTracker.Entries().Where(x => x.State == EntityState.Modified).ToList();
            var deletedEntries = ChangeTracker.Entries().Where(x => x.State == EntityState.Deleted).ToList();

            // new tasks with score > 0
            var addedTasks = addedEntries.Where(x => x.Entity is TaskEntity entity && entity.Score > 0).Select(x => x.Entity as TaskEntity).ToList();

            // modified tasks where score affecting properties changed
            var propertiesToFind = new List<string>
            {
                nameof(TaskEntity.Score),
                nameof(TaskEntity.ScenarioId),
                nameof(TaskEntity.ScenarioTemplateId),
                nameof(TaskEntity.TriggerCondition)
            };

            var modifiedTasks = modifiedEntries
                .Where(x => x.Entity is TaskEntity &&
                            x.Properties
                                .Any(y => y.IsModified && propertiesToFind.Contains(y.Metadata.Name)))
                .ToList();

            // deleted tasks with score > 0
            var deletedTasks = deletedEntries.Where(x => x.Entity is TaskEntity entity && entity.Score > 0).Select(x => x.Entity as TaskEntity).ToList();
            foreach (var deletedTask in deletedTasks)
            {
                deletedTask.Score = 0;
            }

            // get scenarioId and scenarioTemplateId from all tasks
            var scenarioIds = new List<Guid?>();
            var scenarioTemplateIds = new List<Guid?>();

            foreach (var task in addedTasks)
            {
                scenarioIds.Add(task.ScenarioId);
                scenarioTemplateIds.Add(task.ScenarioTemplateId);
            }

            foreach (var task in modifiedTasks)
            {
                scenarioIds.AddRange(task.Properties.Where(x => x.Metadata.Name == nameof(TaskEntity.ScenarioId)).Select(y => y.CurrentValue).Cast<Guid?>());
                scenarioIds.AddRange(task.Properties.Where(x => x.Metadata.Name == nameof(TaskEntity.ScenarioId)).Select(y => y.OriginalValue).Cast<Guid?>());

                scenarioTemplateIds.AddRange(task.Properties.Where(x => x.Metadata.Name == nameof(TaskEntity.ScenarioTemplateId)).Select(y => y.CurrentValue).Cast<Guid?>());
                scenarioTemplateIds.AddRange(task.Properties.Where(x => x.Metadata.Name == nameof(TaskEntity.ScenarioTemplateId)).Select(y => y.OriginalValue).Cast<Guid?>());
            }

            foreach (var task in deletedTasks)
            {
                scenarioIds.Add(task.ScenarioId);
                scenarioTemplateIds.Add(task.ScenarioTemplateId);
            }

            scenarioIds = scenarioIds.Where(x => x.HasValue).Distinct().ToList();
            scenarioTemplateIds = scenarioTemplateIds.Where(x => x.HasValue).Distinct().ToList();

            // set update flag on scenarios and scenario templates
            if (scenarioIds.Any() || scenarioTemplateIds.Any())
            {
                var scenarios = await Scenarios
                    .Where(x => scenarioIds.Contains(x.Id))
                    .ToListAsync(ct);

                foreach (var scenario in scenarios)
                {
                    scenario.UpdateScores = true;
                    Entry(scenario).Properties
                        .Where(x => x.Metadata.Name == nameof(ScenarioEntity.UpdateScores))
                        .FirstOrDefault()
                    .IsModified = true;
                }
            }

            if (scenarioTemplateIds.Any())
            {
                var scenarioTemplates = await ScenarioTemplates
                    .Where(x => scenarioTemplateIds.Contains(x.Id))
                    .ToListAsync(ct);

                foreach (var scenarioTemplate in scenarioTemplates)
                {
                    scenarioTemplate.UpdateScores = true;
                    Entry(scenarioTemplate).Properties
                        .Where(x => x.Metadata.Name == nameof(ScenarioTemplateEntity.UpdateScores))
                        .FirstOrDefault()
                    .IsModified = true;
                }
            }
        }
    }
}
