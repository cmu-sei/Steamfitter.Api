// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Steamfitter.Api.Migrations.PostgreSQL.Migrations
{
    public partial class renameNouns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable("scenarios", null, "scenario_templates", null);

            migrationBuilder.RenameTable("sessions", null, "scenarios", null);
            migrationBuilder.RenameColumn("scenario_id", "scenarios", "scenario_template_id", null);

            migrationBuilder.RenameTable("dispatch_tasks", null, "tasks", null);
            migrationBuilder.RenameColumn("scenario_id", "tasks", "scenario_template_id", null);
            migrationBuilder.RenameColumn("session_id", "tasks", "scenario_id", null);

            migrationBuilder.RenameTable("dispatch_task_results", null, "results", null);
            migrationBuilder.RenameColumn("dispatch_task_id", "results", "task_id", null);

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn("task_id", "results", "dispatch_task_id", null);
            migrationBuilder.RenameTable("results", null, "dispatch_task_results", null);

            migrationBuilder.RenameColumn("scenario_id", "tasks", "session_id", null);
            migrationBuilder.RenameColumn("scenario_template_id", "tasks", "scenario_id", null);
            migrationBuilder.RenameTable("tasks", null, "dispatch_tasks", null);

            migrationBuilder.RenameColumn("scenario_template_id", "scenarios", "scenario_id", null);
            migrationBuilder.RenameTable("scenarios", null, "sessions", null);

            migrationBuilder.RenameTable("scenario_templates", null, "scenarios", null);

        }
    }
}
