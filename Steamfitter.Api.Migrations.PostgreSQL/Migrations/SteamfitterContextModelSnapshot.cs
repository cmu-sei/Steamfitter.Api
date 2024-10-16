/*
Copyright 2021 Carnegie Mellon University. All Rights Reserved.
 Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.
*/

// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Steamfitter.Api.Data;

#nullable disable

namespace Steamfitter.Api.Migrations.PostgreSQL.Migrations
{
    [DbContext(typeof(SteamfitterContext))]
    partial class SteamfitterContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "uuid-ossp");
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Steamfitter.Api.Data.Models.BondAgent", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<string>("AgentInstalledPath")
                        .HasColumnType("text")
                        .HasColumnName("agent_installed_path");

                    b.Property<string>("AgentName")
                        .HasColumnType("text")
                        .HasColumnName("agent_name");

                    b.Property<string>("AgentVersion")
                        .HasColumnType("text")
                        .HasColumnName("agent_version");

                    b.Property<DateTime?>("CheckinTime")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("checkin_time");

                    b.Property<string>("FQDN")
                        .HasColumnType("text")
                        .HasColumnName("fqdn");

                    b.Property<string>("GuestIp")
                        .HasColumnType("text")
                        .HasColumnName("guest_ip");

                    b.Property<string>("MachineName")
                        .HasColumnType("text")
                        .HasColumnName("machine_name");

                    b.Property<int?>("OperatingSystemId")
                        .HasColumnType("integer")
                        .HasColumnName("operating_system_id");

                    b.Property<Guid>("VmWareName")
                        .HasColumnType("uuid")
                        .HasColumnName("vm_ware_name");

                    b.Property<Guid>("VmWareUuid")
                        .HasColumnType("uuid")
                        .HasColumnName("vm_ware_uuid");

                    b.HasKey("Id");

                    b.HasIndex("OperatingSystemId");

                    b.ToTable("bond_agents");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.FileEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_modified");

                    b.Property<long>("Length")
                        .HasColumnType("bigint")
                        .HasColumnName("length");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("modified_by");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<string>("StoragePath")
                        .HasColumnType("text")
                        .HasColumnName("storage_path");

                    b.HasKey("Id");

                    b.ToTable("files");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.LocalUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<Guid?>("BondAgentId")
                        .HasColumnType("uuid")
                        .HasColumnName("bond_agent_id");

                    b.Property<string>("Domain")
                        .HasColumnType("text")
                        .HasColumnName("domain");

                    b.Property<bool>("IsCurrent")
                        .HasColumnType("boolean")
                        .HasColumnName("is_current");

                    b.Property<string>("Username")
                        .HasColumnType("text")
                        .HasColumnName("username");

                    b.HasKey("Id");

                    b.HasIndex("BondAgentId");

                    b.ToTable("local_user");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.MonitoredTool", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<Guid?>("BondAgentId")
                        .HasColumnType("uuid")
                        .HasColumnName("bond_agent_id");

                    b.Property<bool>("IsRunning")
                        .HasColumnType("boolean")
                        .HasColumnName("is_running");

                    b.Property<string>("Location")
                        .HasColumnType("text")
                        .HasColumnName("location");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<string>("Version")
                        .HasColumnType("text")
                        .HasColumnName("version");

                    b.HasKey("Id");

                    b.HasIndex("BondAgentId");

                    b.ToTable("monitored_tool");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.OS", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Platform")
                        .HasColumnType("text")
                        .HasColumnName("platform");

                    b.Property<string>("ServicePack")
                        .HasColumnType("text")
                        .HasColumnName("service_pack");

                    b.Property<string>("Version")
                        .HasColumnType("text")
                        .HasColumnName("version");

                    b.Property<string>("VersionString")
                        .HasColumnType("text")
                        .HasColumnName("version_string");

                    b.HasKey("Id");

                    b.ToTable("os");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.PermissionEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_modified");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<string>("Key")
                        .HasColumnType("text")
                        .HasColumnName("key");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("modified_by");

                    b.Property<bool>("ReadOnly")
                        .HasColumnType("boolean")
                        .HasColumnName("read_only");

                    b.Property<string>("Value")
                        .HasColumnType("text")
                        .HasColumnName("value");

                    b.HasKey("Id");

                    b.HasIndex("Key", "Value")
                        .IsUnique();

                    b.ToTable("permissions");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.ResultEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<int>("Action")
                        .HasColumnType("integer")
                        .HasColumnName("action");

                    b.Property<string>("ActualOutput")
                        .HasColumnType("text")
                        .HasColumnName("actual_output");

                    b.Property<string>("ApiUrl")
                        .HasColumnType("text")
                        .HasColumnName("api_url");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<int>("CurrentIteration")
                        .HasColumnType("integer")
                        .HasColumnName("current_iteration");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_modified");

                    b.Property<string>("ExpectedOutput")
                        .HasColumnType("text")
                        .HasColumnName("expected_output");

                    b.Property<int>("ExpirationSeconds")
                        .HasColumnType("integer")
                        .HasColumnName("expiration_seconds");

                    b.Property<string>("InputString")
                        .HasColumnType("text")
                        .HasColumnName("input_string");

                    b.Property<int>("IntervalSeconds")
                        .HasColumnType("integer")
                        .HasColumnName("interval_seconds");

                    b.Property<int>("Iterations")
                        .HasColumnType("integer")
                        .HasColumnName("iterations");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("modified_by");

                    b.Property<DateTime>("SentDate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("sent_date");

                    b.Property<int>("Status")
                        .HasColumnType("integer")
                        .HasColumnName("status");

                    b.Property<DateTime>("StatusDate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("status_date");

                    b.Property<Guid?>("TaskId")
                        .HasColumnType("uuid")
                        .HasColumnName("task_id");

                    b.Property<Guid?>("VmId")
                        .HasColumnType("uuid")
                        .HasColumnName("vm_id");

                    b.Property<string>("VmName")
                        .HasColumnType("text")
                        .HasColumnName("vm_name");

                    b.HasKey("Id");

                    b.HasIndex("TaskId");

                    b.ToTable("results");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.ScenarioEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_modified");

                    b.Property<Guid?>("DefaultVmCredentialId")
                        .HasColumnType("uuid")
                        .HasColumnName("default_vm_credential_id");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<DateTime>("EndDate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("end_date");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("modified_by");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<bool>("OnDemand")
                        .HasColumnType("boolean")
                        .HasColumnName("on_demand");

                    b.Property<Guid?>("ScenarioTemplateId")
                        .HasColumnType("uuid")
                        .HasColumnName("scenario_template_id");

                    b.Property<int>("Score")
                        .HasColumnType("integer")
                        .HasColumnName("score");

                    b.Property<int>("ScoreEarned")
                        .HasColumnType("integer")
                        .HasColumnName("score_earned");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("start_date");

                    b.Property<int>("Status")
                        .HasColumnType("integer")
                        .HasColumnName("status");

                    b.Property<bool>("UpdateScores")
                        .HasColumnType("boolean")
                        .HasColumnName("update_scores");

                    b.Property<string>("View")
                        .HasColumnType("text")
                        .HasColumnName("view");

                    b.Property<Guid?>("ViewId")
                        .HasColumnType("uuid")
                        .HasColumnName("view_id");

                    b.HasKey("Id");

                    b.HasIndex("ScenarioTemplateId");

                    b.ToTable("scenarios");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.ScenarioTemplateEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_modified");

                    b.Property<Guid?>("DefaultVmCredentialId")
                        .HasColumnType("uuid")
                        .HasColumnName("default_vm_credential_id");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<int?>("DurationHours")
                        .HasColumnType("integer")
                        .HasColumnName("duration_hours");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("modified_by");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<int>("Score")
                        .HasColumnType("integer")
                        .HasColumnName("score");

                    b.Property<bool>("UpdateScores")
                        .HasColumnType("boolean")
                        .HasColumnName("update_scores");

                    b.HasKey("Id");

                    b.ToTable("scenario_templates");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.SshPort", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<Guid?>("BondAgentId")
                        .HasColumnType("uuid")
                        .HasColumnName("bond_agent_id");

                    b.Property<string>("Guest")
                        .HasColumnType("text")
                        .HasColumnName("guest");

                    b.Property<long>("GuestPort")
                        .HasColumnType("bigint")
                        .HasColumnName("guest_port");

                    b.Property<string>("Server")
                        .HasColumnType("text")
                        .HasColumnName("server");

                    b.Property<long>("ServerPort")
                        .HasColumnType("bigint")
                        .HasColumnName("server_port");

                    b.HasKey("Id");

                    b.HasIndex("BondAgentId");

                    b.ToTable("ssh_port");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.TaskEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<int>("Action")
                        .HasColumnType("integer")
                        .HasColumnName("action");

                    b.Property<string>("ApiUrl")
                        .HasColumnType("text")
                        .HasColumnName("api_url");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<int>("CurrentIteration")
                        .HasColumnType("integer")
                        .HasColumnName("current_iteration");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_modified");

                    b.Property<int>("DelaySeconds")
                        .HasColumnType("integer")
                        .HasColumnName("delay_seconds");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<string>("ExpectedOutput")
                        .HasColumnType("text")
                        .HasColumnName("expected_output");

                    b.Property<int>("ExpirationSeconds")
                        .HasColumnType("integer")
                        .HasColumnName("expiration_seconds");

                    b.Property<string>("InputString")
                        .HasColumnType("text")
                        .HasColumnName("input_string");

                    b.Property<int>("IntervalSeconds")
                        .HasColumnType("integer")
                        .HasColumnName("interval_seconds");

                    b.Property<int>("IterationTermination")
                        .HasColumnType("integer")
                        .HasColumnName("iteration_termination");

                    b.Property<int>("Iterations")
                        .HasColumnType("integer")
                        .HasColumnName("iterations");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("modified_by");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<bool>("Repeatable")
                        .HasColumnType("boolean")
                        .HasColumnName("repeatable");

                    b.Property<Guid?>("ScenarioId")
                        .HasColumnType("uuid")
                        .HasColumnName("scenario_id");

                    b.Property<Guid?>("ScenarioTemplateId")
                        .HasColumnType("uuid")
                        .HasColumnName("scenario_template_id");

                    b.Property<int>("Score")
                        .HasColumnType("integer")
                        .HasColumnName("score");

                    b.Property<int>("Status")
                        .HasColumnType("integer")
                        .HasColumnName("status");

                    b.Property<int>("TotalScore")
                        .HasColumnType("integer")
                        .HasColumnName("total_score");

                    b.Property<int>("TotalScoreEarned")
                        .HasColumnType("integer")
                        .HasColumnName("total_score_earned");

                    b.Property<int>("TotalStatus")
                        .HasColumnType("integer")
                        .HasColumnName("total_status");

                    b.Property<int>("TriggerCondition")
                        .HasColumnType("integer")
                        .HasColumnName("trigger_condition");

                    b.Property<Guid?>("TriggerTaskId")
                        .HasColumnType("uuid")
                        .HasColumnName("trigger_task_id");

                    b.Property<bool>("UserExecutable")
                        .HasColumnType("boolean")
                        .HasColumnName("user_executable");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

                    b.Property<string>("VmMask")
                        .HasColumnType("text")
                        .HasColumnName("vm_mask");

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
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_modified");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("modified_by");

                    b.Property<string>("Name")
                        .HasColumnType("text")
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
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("PermissionId")
                        .HasColumnType("uuid")
                        .HasColumnName("permission_id");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

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
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("ScenarioId")
                        .HasColumnType("uuid")
                        .HasColumnName("scenario_id");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid")
                        .HasColumnName("user_id");

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
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("uuid_generate_v4()");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<DateTime>("DateCreated")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_created");

                    b.Property<DateTime?>("DateModified")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_modified");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<Guid?>("ModifiedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("modified_by");

                    b.Property<string>("Password")
                        .HasColumnType("text")
                        .HasColumnName("password");

                    b.Property<Guid?>("ScenarioId")
                        .HasColumnType("uuid")
                        .HasColumnName("scenario_id");

                    b.Property<Guid?>("ScenarioTemplateId")
                        .HasColumnType("uuid")
                        .HasColumnName("scenario_template_id");

                    b.Property<string>("Username")
                        .HasColumnType("text")
                        .HasColumnName("username");

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

                    b.Navigation("OperatingSystem");
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

                    b.Navigation("Task");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.ScenarioEntity", b =>
                {
                    b.HasOne("Steamfitter.Api.Data.Models.ScenarioTemplateEntity", "ScenarioTemplate")
                        .WithMany("Scenarios")
                        .HasForeignKey("ScenarioTemplateId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("ScenarioTemplate");
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

                    b.Navigation("Scenario");

                    b.Navigation("ScenarioTemplate");

                    b.Navigation("TriggerTask");
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

                    b.Navigation("Permission");

                    b.Navigation("User");
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

                    b.Navigation("Scenario");

                    b.Navigation("User");
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

                    b.Navigation("Scenario");

                    b.Navigation("ScenarioTemplate");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.BondAgent", b =>
                {
                    b.Navigation("LocalUsers");

                    b.Navigation("MonitoredTools");

                    b.Navigation("SshPorts");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.PermissionEntity", b =>
                {
                    b.Navigation("UserPermissions");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.ScenarioEntity", b =>
                {
                    b.Navigation("Tasks");

                    b.Navigation("Users");

                    b.Navigation("VmCredentials");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.ScenarioTemplateEntity", b =>
                {
                    b.Navigation("Scenarios");

                    b.Navigation("Tasks");

                    b.Navigation("VmCredentials");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.TaskEntity", b =>
                {
                    b.Navigation("Children");

                    b.Navigation("Results");
                });

            modelBuilder.Entity("Steamfitter.Api.Data.Models.UserEntity", b =>
                {
                    b.Navigation("UserPermissions");

                    b.Navigation("UserScenarios");
                });
#pragma warning restore 612, 618
        }
    }
}
