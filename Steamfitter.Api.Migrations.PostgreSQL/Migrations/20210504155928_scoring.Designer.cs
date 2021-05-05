/*
Copyright 2021 Carnegie Mellon University. All Rights Reserved. 
 Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.
*/

﻿// <auto-generated />
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
    [Migration("20210504155928_scoring")]
    partial class scoring
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:PostgresExtension:uuid-ossp", ",,")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("Steamfitter.Api.Data.Models.BondAgent", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<string>("AgentInstalledPath")
                        .HasColumnName("agent_installed_path")
                        .HasColumnType("text");

                    b.Property<string>("AgentName")
                        .HasColumnName("agent_name")
                        .HasColumnType("text");

                    b.Property<string>("AgentVersion")
                        .HasColumnName("agent_version")
                        .HasColumnType("text");

                    b.Property<DateTime?>("CheckinTime")
                        .HasColumnName("checkin_time")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("FQDN")
                        .HasColumnName("fqdn")
                        .HasColumnType("text");

                    b.Property<string>("GuestIp")
                        .HasColumnName("guest_ip")
                        .HasColumnType("text");

                    b.Property<string>("MachineName")
                        .HasColumnName("machine_name")
                        .HasColumnType("text");

                    b.Property<int?>("OperatingSystemId")
                        .HasColumnName("operating_system_id")
                        .HasColumnType("integer");

                    b.Property<Guid>("VmWareName")
                        .HasColumnName("vm_ware_name")
                        .HasColumnType("uuid");

                    b.Property<Guid>("VmWareUuid")
                        .HasColumnName("vm_ware_uuid")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("OperatingSystemId");

                    b.ToTable("bond_agents");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.FileEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnName("created_by")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnName("date_created")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnName("date_modified")
                        .HasColumnType("timestamp without time zone");

                    b.Property<long>("Length")
                        .HasColumnName("length")
                        .HasColumnType("bigint");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnName("modified_by")
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("text");

                    b.Property<string>("StoragePath")
                        .HasColumnName("storage_path")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("files");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.LocalUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<Guid?>("BondAgentId")
                        .HasColumnName("bond_agent_id")
                        .HasColumnType("uuid");

                    b.Property<string>("Domain")
                        .HasColumnName("domain")
                        .HasColumnType("text");

                    b.Property<bool>("IsCurrent")
                        .HasColumnName("is_current")
                        .HasColumnType("boolean");

                    b.Property<string>("Username")
                        .HasColumnName("username")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("BondAgentId");

                    b.ToTable("local_user");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.MonitoredTool", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<Guid?>("BondAgentId")
                        .HasColumnName("bond_agent_id")
                        .HasColumnType("uuid");

                    b.Property<bool>("IsRunning")
                        .HasColumnName("is_running")
                        .HasColumnType("boolean");

                    b.Property<string>("Location")
                        .HasColumnName("location")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("text");

                    b.Property<string>("Version")
                        .HasColumnName("version")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("BondAgentId");

                    b.ToTable("monitored_tool");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.OS", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Platform")
                        .HasColumnName("platform")
                        .HasColumnType("text");

                    b.Property<string>("ServicePack")
                        .HasColumnName("service_pack")
                        .HasColumnType("text");

                    b.Property<string>("Version")
                        .HasColumnName("version")
                        .HasColumnType("text");

                    b.Property<string>("VersionString")
                        .HasColumnName("version_string")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("os");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.PermissionEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnName("created_by")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnName("date_created")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnName("date_modified")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Description")
                        .HasColumnName("description")
                        .HasColumnType("text");

                    b.Property<string>("Key")
                        .HasColumnName("key")
                        .HasColumnType("text");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnName("modified_by")
                        .HasColumnType("uuid");

                    b.Property<bool>("ReadOnly")
                        .HasColumnName("read_only")
                        .HasColumnType("boolean");

                    b.Property<string>("Value")
                        .HasColumnName("value")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Key", "Value")
                        .IsUnique();

                    b.ToTable("permissions");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.ResultEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<int>("Action")
                        .HasColumnName("action")
                        .HasColumnType("integer");

                    b.Property<string>("ActualOutput")
                        .HasColumnName("actual_output")
                        .HasColumnType("text");

                    b.Property<string>("ApiUrl")
                        .HasColumnName("api_url")
                        .HasColumnType("text");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnName("created_by")
                        .HasColumnType("uuid");

                    b.Property<int>("CurrentIteration")
                        .HasColumnName("current_iteration")
                        .HasColumnType("integer");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnName("date_created")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnName("date_modified")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("ExpectedOutput")
                        .HasColumnName("expected_output")
                        .HasColumnType("text");

                    b.Property<int>("ExpirationSeconds")
                        .HasColumnName("expiration_seconds")
                        .HasColumnType("integer");

                    b.Property<string>("InputString")
                        .HasColumnName("input_string")
                        .HasColumnType("text");

                    b.Property<int>("IntervalSeconds")
                        .HasColumnName("interval_seconds")
                        .HasColumnType("integer");

                    b.Property<int>("Iterations")
                        .HasColumnName("iterations")
                        .HasColumnType("integer");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnName("modified_by")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("SentDate")
                        .HasColumnName("sent_date")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int>("Status")
                        .HasColumnName("status")
                        .HasColumnType("integer");

                    b.Property<DateTime>("StatusDate")
                        .HasColumnName("status_date")
                        .HasColumnType("timestamp without time zone");

                    b.Property<Guid?>("TaskId")
                        .HasColumnName("task_id")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("VmId")
                        .HasColumnName("vm_id")
                        .HasColumnType("uuid");

                    b.Property<string>("VmName")
                        .HasColumnName("vm_name")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("TaskId");

                    b.ToTable("results");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.ScenarioEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnName("created_by")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnName("date_created")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnName("date_modified")
                        .HasColumnType("timestamp without time zone");

                    b.Property<Guid?>("DefaultVmCredentialId")
                        .HasColumnName("default_vm_credential_id")
                        .HasColumnType("uuid");

                    b.Property<string>("Description")
                        .HasColumnName("description")
                        .HasColumnType("text");

                    b.Property<DateTime>("EndDate")
                        .HasColumnName("end_date")
                        .HasColumnType("timestamp without time zone");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnName("modified_by")
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("text");

                    b.Property<bool>("OnDemand")
                        .HasColumnName("on_demand")
                        .HasColumnType("boolean");

                    b.Property<Guid?>("ScenarioTemplateId")
                        .HasColumnName("scenario_template_id")
                        .HasColumnType("uuid");

                    b.Property<int>("Score")
                        .HasColumnName("score")
                        .HasColumnType("integer");

                    b.Property<int>("ScoreEarned")
                        .HasColumnName("score_earned")
                        .HasColumnType("integer");

                    b.Property<DateTime>("StartDate")
                        .HasColumnName("start_date")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int>("Status")
                        .HasColumnName("status")
                        .HasColumnType("integer");

                    b.Property<bool>("UpdateScores")
                        .HasColumnName("update_scores")
                        .HasColumnType("boolean");

                    b.Property<string>("View")
                        .HasColumnName("view")
                        .HasColumnType("text");

                    b.Property<Guid?>("ViewId")
                        .HasColumnName("view_id")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("ScenarioTemplateId");

                    b.ToTable("scenarios");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.ScenarioTemplateEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnName("created_by")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnName("date_created")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnName("date_modified")
                        .HasColumnType("timestamp without time zone");

                    b.Property<Guid?>("DefaultVmCredentialId")
                        .HasColumnName("default_vm_credential_id")
                        .HasColumnType("uuid");

                    b.Property<string>("Description")
                        .HasColumnName("description")
                        .HasColumnType("text");

                    b.Property<int?>("DurationHours")
                        .HasColumnName("duration_hours")
                        .HasColumnType("integer");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnName("modified_by")
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("text");

                    b.Property<int>("Score")
                        .HasColumnName("score")
                        .HasColumnType("integer");

                    b.Property<bool>("UpdateScores")
                        .HasColumnName("update_scores")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.ToTable("scenario_templates");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.SshPort", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<Guid?>("BondAgentId")
                        .HasColumnName("bond_agent_id")
                        .HasColumnType("uuid");

                    b.Property<string>("Guest")
                        .HasColumnName("guest")
                        .HasColumnType("text");

                    b.Property<long>("GuestPort")
                        .HasColumnName("guest_port")
                        .HasColumnType("bigint");

                    b.Property<string>("Server")
                        .HasColumnName("server")
                        .HasColumnType("text");

                    b.Property<long>("ServerPort")
                        .HasColumnName("server_port")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("BondAgentId");

                    b.ToTable("ssh_port");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.TaskEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<int>("Action")
                        .HasColumnName("action")
                        .HasColumnType("integer");

                    b.Property<string>("ApiUrl")
                        .HasColumnName("api_url")
                        .HasColumnType("text");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnName("created_by")
                        .HasColumnType("uuid");

                    b.Property<int>("CurrentIteration")
                        .HasColumnName("current_iteration")
                        .HasColumnType("integer");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnName("date_created")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnName("date_modified")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int>("DelaySeconds")
                        .HasColumnName("delay_seconds")
                        .HasColumnType("integer");

                    b.Property<string>("Description")
                        .HasColumnName("description")
                        .HasColumnType("text");

                    b.Property<string>("ExpectedOutput")
                        .HasColumnName("expected_output")
                        .HasColumnType("text");

                    b.Property<int>("ExpirationSeconds")
                        .HasColumnName("expiration_seconds")
                        .HasColumnType("integer");

                    b.Property<string>("InputString")
                        .HasColumnName("input_string")
                        .HasColumnType("text");

                    b.Property<int>("IntervalSeconds")
                        .HasColumnName("interval_seconds")
                        .HasColumnType("integer");

                    b.Property<int>("IterationTermination")
                        .HasColumnName("iteration_termination")
                        .HasColumnType("integer");

                    b.Property<int>("Iterations")
                        .HasColumnName("iterations")
                        .HasColumnType("integer");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnName("modified_by")
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("text");

                    b.Property<bool>("Repeatable")
                        .HasColumnName("repeatable")
                        .HasColumnType("boolean");

                    b.Property<Guid?>("ScenarioId")
                        .HasColumnName("scenario_id")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("ScenarioTemplateId")
                        .HasColumnName("scenario_template_id")
                        .HasColumnType("uuid");

                    b.Property<int>("Score")
                        .HasColumnName("score")
                        .HasColumnType("integer");

                    b.Property<int>("Status")
                        .HasColumnName("status")
                        .HasColumnType("integer");

                    b.Property<int>("TotalScore")
                        .HasColumnName("total_score")
                        .HasColumnType("integer");

                    b.Property<int>("TotalScoreEarned")
                        .HasColumnName("total_score_earned")
                        .HasColumnType("integer");

                    b.Property<int>("TotalStatus")
                        .HasColumnName("total_status")
                        .HasColumnType("integer");

                    b.Property<int>("TriggerCondition")
                        .HasColumnName("trigger_condition")
                        .HasColumnType("integer");

                    b.Property<Guid?>("TriggerTaskId")
                        .HasColumnName("trigger_task_id")
                        .HasColumnType("uuid");

                    b.Property<bool>("UserExecutable")
                        .HasColumnName("user_executable")
                        .HasColumnType("boolean");

                    b.Property<Guid?>("UserId")
                        .HasColumnName("user_id")
                        .HasColumnType("uuid");

                    b.Property<string>("VmMask")
                        .HasColumnName("vm_mask")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ScenarioId");

                    b.HasIndex("ScenarioTemplateId");

                    b.HasIndex("TriggerTaskId");

                    b.HasIndex("UserId");

                    b.ToTable("tasks");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.UserEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnName("created_by")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnName("date_created")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnName("date_modified")
                        .HasColumnType("timestamp without time zone");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnName("modified_by")
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("text");

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
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("PermissionId")
                        .HasColumnName("permission_id")
                        .HasColumnType("uuid");

                    b.Property<Guid>("UserId")
                        .HasColumnName("user_id")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("PermissionId");

                    b.HasIndex("UserId", "PermissionId")
                        .IsUnique();

                    b.ToTable("user_permissions");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.UserScenarioEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("ScenarioId")
                        .HasColumnName("scenario_id")
                        .HasColumnType("uuid");

                    b.Property<Guid>("UserId")
                        .HasColumnName("user_id")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("ScenarioId");

                    b.HasIndex("UserId", "ScenarioId")
                        .IsUnique();

                    b.ToTable("user_scenario_entity");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.VmCredentialEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("uuid")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnName("created_by")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnName("date_created")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnName("date_modified")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Description")
                        .HasColumnName("description")
                        .HasColumnType("text");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnName("modified_by")
                        .HasColumnType("uuid");

                    b.Property<string>("Password")
                        .HasColumnName("password")
                        .HasColumnType("text");

                    b.Property<Guid?>("ScenarioId")
                        .HasColumnName("scenario_id")
                        .HasColumnType("uuid");

                    b.Property<Guid?>("ScenarioTemplateId")
                        .HasColumnName("scenario_template_id")
                        .HasColumnType("uuid");

                    b.Property<string>("Username")
                        .HasColumnName("username")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ScenarioId");

                    b.HasIndex("ScenarioTemplateId");

                    b.ToTable("vm_credentials");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.BondAgent", b =>
                {
                    b.HasOne("Steamfitter.Api.Data.Models.OS", "OperatingSystem")
                        .WithMany()
                        .HasForeignKey("OperatingSystemId");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.LocalUser", b =>
                {
                    b.HasOne("Steamfitter.Api.Data.Models.BondAgent", null)
                        .WithMany("LocalUsers")
                        .HasForeignKey("BondAgentId");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.MonitoredTool", b =>
                {
                    b.HasOne("Steamfitter.Api.Data.Models.BondAgent", null)
                        .WithMany("MonitoredTools")
                        .HasForeignKey("BondAgentId");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.ResultEntity", b =>
                {
                    b.HasOne("Steamfitter.Api.Data.Models.TaskEntity", "Task")
                        .WithMany("Results")
                        .HasForeignKey("TaskId")
                        .OnDelete(DeleteBehavior.SetNull);
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.ScenarioEntity", b =>
                {
                    b.HasOne("Steamfitter.Api.Data.Models.ScenarioTemplateEntity", "ScenarioTemplate")
                        .WithMany("Scenarios")
                        .HasForeignKey("ScenarioTemplateId")
                        .OnDelete(DeleteBehavior.SetNull);
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.SshPort", b =>
                {
                    b.HasOne("Steamfitter.Api.Data.Models.BondAgent", null)
                        .WithMany("SshPorts")
                        .HasForeignKey("BondAgentId");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.TaskEntity", b =>
                {
                    b.HasOne("Steamfitter.Api.Data.Models.ScenarioEntity", "Scenario")
                        .WithMany("Tasks")
                        .HasForeignKey("ScenarioId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Steamfitter.Api.Data.Models.ScenarioTemplateEntity", "ScenarioTemplate")
                        .WithMany("Tasks")
                        .HasForeignKey("ScenarioTemplateId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Steamfitter.Api.Data.Models.TaskEntity", "TriggerTask")
                        .WithMany("Children")
                        .HasForeignKey("TriggerTaskId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.UserPermissionEntity", b =>
                {
                    b.HasOne("Steamfitter.Api.Data.Models.PermissionEntity", "Permission")
                        .WithMany("UserPermissions")
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Steamfitter.Api.Data.Models.UserEntity", "User")
                        .WithMany("UserPermissions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.UserScenarioEntity", b =>
                {
                    b.HasOne("Steamfitter.Api.Data.Models.ScenarioEntity", "Scenario")
                        .WithMany("Users")
                        .HasForeignKey("ScenarioId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Steamfitter.Api.Data.Models.UserEntity", "User")
                        .WithMany("UserScenarios")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.VmCredentialEntity", b =>
                {
                    b.HasOne("Steamfitter.Api.Data.Models.ScenarioEntity", "Scenario")
                        .WithMany("VmCredentials")
                        .HasForeignKey("ScenarioId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Steamfitter.Api.Data.Models.ScenarioTemplateEntity", "ScenarioTemplate")
                        .WithMany("VmCredentials")
                        .HasForeignKey("ScenarioTemplateId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
