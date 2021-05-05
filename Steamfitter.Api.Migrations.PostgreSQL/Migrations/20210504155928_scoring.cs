/*
Copyright 2021 Carnegie Mellon University. All Rights Reserved. 
 Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.
*/

﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Steamfitter.Api.Migrations.PostgreSQL.Migrations
{
    public partial class scoring : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "repeatable",
                table: "tasks",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "score",
                table: "tasks",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "tasks",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "total_score",
                table: "tasks",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "total_score_earned",
                table: "tasks",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "total_status",
                table: "tasks",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "user_executable",
                table: "tasks",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "score",
                table: "scenarios",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "score_earned",
                table: "scenarios",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "update_scores",
                table: "scenarios",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "score",
                table: "scenario_templates",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "update_scores",
                table: "scenario_templates",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "user_scenario_entity",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(nullable: false),
                    scenario_id = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_scenario_entity", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_scenario_entity_scenarios_scenario_id",
                        column: x => x.scenario_id,
                        principalTable: "scenarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_scenario_entity_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_scenario_entity_scenario_id",
                table: "user_scenario_entity",
                column: "scenario_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_scenario_entity_user_id_scenario_id",
                table: "user_scenario_entity",
                columns: new[] { "user_id", "scenario_id" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_scenario_entity");

            migrationBuilder.DropColumn(
                name: "repeatable",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "score",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "status",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "total_score",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "total_score_earned",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "total_status",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "user_executable",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "score",
                table: "scenarios");

            migrationBuilder.DropColumn(
                name: "score_earned",
                table: "scenarios");

            migrationBuilder.DropColumn(
                name: "update_scores",
                table: "scenarios");

            migrationBuilder.DropColumn(
                name: "score",
                table: "scenario_templates");

            migrationBuilder.DropColumn(
                name: "update_scores",
                table: "scenario_templates");
        }
    }
}
