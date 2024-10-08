// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Steamfitter.Api.Migrations.PostgreSQL.Migrations
{
    public partial class CascadeDeletes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_dispatch_task_results_dispatch_tasks_dispatch_task_id",
                table: "dispatch_task_results");

            migrationBuilder.DropForeignKey(
                name: "FK_dispatch_tasks_scenarios_scenario_id",
                table: "dispatch_tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_dispatch_tasks_sessions_session_id",
                table: "dispatch_tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_dispatch_tasks_dispatch_tasks_trigger_task_id",
                table: "dispatch_tasks");

            migrationBuilder.AlterColumn<Guid>(
                name: "dispatch_task_id",
                table: "dispatch_task_results",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_dispatch_tasks_user_id",
                table: "dispatch_tasks",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_dispatch_task_results_dispatch_tasks_dispatch_task_id",
                table: "dispatch_task_results",
                column: "dispatch_task_id",
                principalTable: "dispatch_tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_dispatch_tasks_scenarios_scenario_id",
                table: "dispatch_tasks",
                column: "scenario_id",
                principalTable: "scenarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_dispatch_tasks_sessions_session_id",
                table: "dispatch_tasks",
                column: "session_id",
                principalTable: "sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_dispatch_tasks_dispatch_tasks_trigger_task_id",
                table: "dispatch_tasks",
                column: "trigger_task_id",
                principalTable: "dispatch_tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_dispatch_task_results_dispatch_tasks_dispatch_task_id",
                table: "dispatch_task_results");

            migrationBuilder.DropForeignKey(
                name: "FK_dispatch_tasks_scenarios_scenario_id",
                table: "dispatch_tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_dispatch_tasks_sessions_session_id",
                table: "dispatch_tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_dispatch_tasks_dispatch_tasks_trigger_task_id",
                table: "dispatch_tasks");

            migrationBuilder.DropIndex(
                name: "IX_dispatch_tasks_user_id",
                table: "dispatch_tasks");

            migrationBuilder.AlterColumn<Guid>(
                name: "dispatch_task_id",
                table: "dispatch_task_results",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AddForeignKey(
                name: "FK_dispatch_task_results_dispatch_tasks_dispatch_task_id",
                table: "dispatch_task_results",
                column: "dispatch_task_id",
                principalTable: "dispatch_tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dispatch_tasks_scenarios_scenario_id",
                table: "dispatch_tasks",
                column: "scenario_id",
                principalTable: "scenarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dispatch_tasks_sessions_session_id",
                table: "dispatch_tasks",
                column: "session_id",
                principalTable: "sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dispatch_tasks_dispatch_tasks_trigger_task_id",
                table: "dispatch_tasks",
                column: "trigger_task_id",
                principalTable: "dispatch_tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
