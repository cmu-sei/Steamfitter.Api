// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.
using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Steamfitter.Api.Migrations.PostgreSQL.Migrations
{
    public partial class NullTaskId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_results_tasks_task_id",
                table: "results");

            migrationBuilder.AlterColumn<Guid>(
                name: "task_id",
                table: "results",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_results_tasks_task_id",
                table: "results",
                column: "task_id",
                principalTable: "tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_results_tasks_task_id",
                table: "results");

            migrationBuilder.AlterColumn<Guid>(
                name: "task_id",
                table: "results",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_results_tasks_task_id",
                table: "results",
                column: "task_id",
                principalTable: "tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
