// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Data.Extensions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;

namespace Steamfitter.Api.Data
{
    public class SteamfitterContext : DbContext
    {
        private DbContextOptions<SteamfitterContext> _options;

        public SteamfitterContext(DbContextOptions<SteamfitterContext> options) : base(options) {
            _options = options;
        }
        
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
    }
}

