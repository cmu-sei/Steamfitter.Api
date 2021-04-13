﻿// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Steamfitter.Api.Data;

namespace Steamfitter.Api.Migrations.PostgreSQL.Migrations
{
    [DbContext(typeof(SteamfitterContext))]
    [Migration("20200403162431_hashSets")]
    partial class hashSets
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:PostgresExtension:uuid-ossp", ",,")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.2.1-servicing-10028")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("Steamfitter.Api.Data.Models.DispatchTaskEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<int>("Action")
                        .HasColumnName("action");

                    b.Property<string>("ApiUrl")
                        .HasColumnName("api_url");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnName("date_modified");

                    b.Property<int>("DelaySeconds")
                        .HasColumnName("delay_seconds");

                    b.Property<string>("Description")
                        .HasColumnName("description");

                    b.Property<string>("ExpectedOutput")
                        .HasColumnName("expected_output");

                    b.Property<int>("ExpirationSeconds")
                        .HasColumnName("expiration_seconds");

                    b.Property<string>("InputString")
                        .HasColumnName("input_string");

                    b.Property<int>("IntervalSeconds")
                        .HasColumnName("interval_seconds");

                    b.Property<int>("Iterations")
                        .HasColumnName("iterations");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnName("modified_by");

                    b.Property<string>("Name")
                        .HasColumnName("name");

                    b.Property<Guid?>("ScenarioId")
                        .HasColumnName("scenario_id");

                    b.Property<Guid?>("SessionId")
                        .HasColumnName("session_id");

                    b.Property<int>("TriggerCondition")
                        .HasColumnName("trigger_condition");

                    b.Property<Guid?>("TriggerTaskId")
                        .HasColumnName("trigger_task_id");

                    b.Property<Guid?>("UserId")
                        .HasColumnName("user_id");

                    b.Property<string>("VmMask")
                        .HasColumnName("vm_mask");

                    b.HasKey("Id");

                    b.HasIndex("ScenarioId");

                    b.HasIndex("SessionId");

                    b.HasIndex("TriggerTaskId");

                    b.HasIndex("UserId");

                    b.ToTable("dispatch_tasks");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.DispatchTaskResultEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<string>("ActualOutput")
                        .HasColumnName("actual_output");

                    b.Property<string>("ApiUrl")
                        .HasColumnName("api_url");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnName("date_modified");

                    b.Property<Guid?>("DispatchTaskId")
                        .IsRequired()
                        .HasColumnName("dispatch_task_id");

                    b.Property<string>("ExpectedOutput")
                        .HasColumnName("expected_output");

                    b.Property<int>("ExpirationSeconds")
                        .HasColumnName("expiration_seconds");

                    b.Property<string>("InputString")
                        .HasColumnName("input_string");

                    b.Property<int>("IntervalSeconds")
                        .HasColumnName("interval_seconds");

                    b.Property<int>("Iterations")
                        .HasColumnName("iterations");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnName("modified_by");

                    b.Property<DateTime>("SentDate")
                        .HasColumnName("sent_date");

                    b.Property<int>("Status")
                        .HasColumnName("status");

                    b.Property<DateTime>("StatusDate")
                        .HasColumnName("status_date");

                    b.Property<Guid?>("VmId")
                        .HasColumnName("vm_id");

                    b.Property<string>("VmName")
                        .HasColumnName("vm_name");

                    b.HasKey("Id");

                    b.HasIndex("DispatchTaskId");

                    b.ToTable("dispatch_task_results");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.ExerciseAgent", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<string>("AgentInstalledPath")
                        .HasColumnName("agent_installed_path");

                    b.Property<string>("AgentName")
                        .HasColumnName("agent_name");

                    b.Property<string>("AgentVersion")
                        .HasColumnName("agent_version");

                    b.Property<DateTime?>("CheckinTime")
                        .HasColumnName("checkin_time");

                    b.Property<string>("FQDN")
                        .HasColumnName("fqdn");

                    b.Property<string>("GuestIp")
                        .HasColumnName("guest_ip");

                    b.Property<string>("MachineName")
                        .HasColumnName("machine_name");

                    b.Property<int?>("OperatingSystemId")
                        .HasColumnName("operating_system_id");

                    b.Property<Guid>("VmWareName")
                        .HasColumnName("vm_ware_name");

                    b.Property<Guid>("VmWareUuid")
                        .HasColumnName("vm_ware_uuid");

                    b.HasKey("Id");

                    b.HasIndex("OperatingSystemId");

                    b.ToTable("exercise_agents");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.FileEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnName("date_modified");

                    b.Property<long>("Length")
                        .HasColumnName("length");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnName("modified_by");

                    b.Property<string>("Name")
                        .HasColumnName("name");

                    b.Property<string>("StoragePath")
                        .HasColumnName("storage_path");

                    b.HasKey("Id");

                    b.ToTable("files");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.LocalUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<string>("Domain")
                        .HasColumnName("domain");

                    b.Property<Guid?>("ExerciseAgentId")
                        .HasColumnName("exercise_agent_id");

                    b.Property<bool>("IsCurrent")
                        .HasColumnName("is_current");

                    b.Property<string>("Username")
                        .HasColumnName("username");

                    b.HasKey("Id");

                    b.HasIndex("ExerciseAgentId");

                    b.ToTable("local_user");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.MonitoredTool", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<Guid?>("ExerciseAgentId")
                        .HasColumnName("exercise_agent_id");

                    b.Property<bool>("IsRunning")
                        .HasColumnName("is_running");

                    b.Property<string>("Location")
                        .HasColumnName("location");

                    b.Property<string>("Name")
                        .HasColumnName("name");

                    b.Property<string>("Version")
                        .HasColumnName("version");

                    b.HasKey("Id");

                    b.HasIndex("ExerciseAgentId");

                    b.ToTable("monitored_tool");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.OS", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<string>("Platform")
                        .HasColumnName("platform");

                    b.Property<string>("ServicePack")
                        .HasColumnName("service_pack");

                    b.Property<string>("Version")
                        .HasColumnName("version");

                    b.Property<string>("VersionString")
                        .HasColumnName("version_string");

                    b.HasKey("Id");

                    b.ToTable("os");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.PermissionEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnName("date_modified");

                    b.Property<string>("Description")
                        .HasColumnName("description");

                    b.Property<string>("Key")
                        .HasColumnName("key");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnName("modified_by");

                    b.Property<bool>("ReadOnly")
                        .HasColumnName("read_only");

                    b.Property<string>("Value")
                        .HasColumnName("value");

                    b.HasKey("Id");

                    b.HasIndex("Key", "Value")
                        .IsUnique();

                    b.ToTable("permissions");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.ScenarioEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnName("date_modified");

                    b.Property<string>("Description")
                        .HasColumnName("description");

                    b.Property<int?>("DurationHours")
                        .HasColumnName("duration_hours");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnName("modified_by");

                    b.Property<string>("Name")
                        .HasColumnName("name");

                    b.HasKey("Id");

                    b.ToTable("scenarios");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.SessionEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnName("date_modified");

                    b.Property<string>("Description")
                        .HasColumnName("description");

                    b.Property<DateTime>("EndDate")
                        .HasColumnName("end_date");

                    b.Property<string>("Exercise")
                        .HasColumnName("exercise");

                    b.Property<Guid?>("ExerciseId")
                        .HasColumnName("exercise_id");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnName("modified_by");

                    b.Property<string>("Name")
                        .HasColumnName("name");

                    b.Property<bool>("OnDemand")
                        .HasColumnName("on_demand");

                    b.Property<Guid?>("ScenarioId")
                        .HasColumnName("scenario_id");

                    b.Property<DateTime>("StartDate")
                        .HasColumnName("start_date");

                    b.Property<int>("Status")
                        .HasColumnName("status");

                    b.HasKey("Id");

                    b.HasIndex("ScenarioId");

                    b.ToTable("sessions");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.SshPort", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<Guid?>("ExerciseAgentId")
                        .HasColumnName("exercise_agent_id");

                    b.Property<string>("Guest")
                        .HasColumnName("guest");

                    b.Property<long>("GuestPort")
                        .HasColumnName("guest_port");

                    b.Property<string>("Server")
                        .HasColumnName("server");

                    b.Property<long>("ServerPort")
                        .HasColumnName("server_port");

                    b.HasKey("Id");

                    b.HasIndex("ExerciseAgentId");

                    b.ToTable("ssh_port");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.UserEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnName("date_modified");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnName("modified_by");

                    b.Property<string>("Name")
                        .HasColumnName("name");

                    b.HasKey("Id");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.ToTable("users");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.UserPermissionEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("PermissionId")
                        .HasColumnName("permission_id");

                    b.Property<Guid>("UserId")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("PermissionId");

                    b.HasIndex("UserId", "PermissionId")
                        .IsUnique();

                    b.ToTable("user_permissions");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.DispatchTaskEntity", b =>
                {
                    b.HasOne("Steamfitter.Api.Data.Models.ScenarioEntity", "Scenario")
                        .WithMany("DispatchTasks")
                        .HasForeignKey("ScenarioId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Steamfitter.Api.Data.Models.SessionEntity", "Session")
                        .WithMany("DispatchTasks")
                        .HasForeignKey("SessionId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Steamfitter.Api.Data.Models.DispatchTaskEntity", "TriggerTask")
                        .WithMany("Children")
                        .HasForeignKey("TriggerTaskId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.DispatchTaskResultEntity", b =>
                {
                    b.HasOne("Steamfitter.Api.Data.Models.DispatchTaskEntity", "DispatchTask")
                        .WithMany("Results")
                        .HasForeignKey("DispatchTaskId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.ExerciseAgent", b =>
                {
                    b.HasOne("Steamfitter.Api.Data.Models.OS", "OperatingSystem")
                        .WithMany()
                        .HasForeignKey("OperatingSystemId");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.LocalUser", b =>
                {
                    b.HasOne("Steamfitter.Api.Data.Models.ExerciseAgent")
                        .WithMany("LocalUsers")
                        .HasForeignKey("ExerciseAgentId");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.MonitoredTool", b =>
                {
                    b.HasOne("Steamfitter.Api.Data.Models.ExerciseAgent")
                        .WithMany("MonitoredTools")
                        .HasForeignKey("ExerciseAgentId");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.SessionEntity", b =>
                {
                    b.HasOne("Steamfitter.Api.Data.Models.ScenarioEntity", "Scenario")
                        .WithMany("Sessions")
                        .HasForeignKey("ScenarioId");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.SshPort", b =>
                {
                    b.HasOne("Steamfitter.Api.Data.Models.ExerciseAgent")
                        .WithMany("SshPorts")
                        .HasForeignKey("ExerciseAgentId");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.UserPermissionEntity", b =>
                {
                    b.HasOne("Steamfitter.Api.Data.Models.PermissionEntity", "Permission")
                        .WithMany("UserPermissions")
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Steamfitter.Api.Data.Models.UserEntity", "User")
                        .WithMany("UserPermissions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
