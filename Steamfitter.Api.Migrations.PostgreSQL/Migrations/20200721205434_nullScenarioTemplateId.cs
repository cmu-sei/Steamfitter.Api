// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.
using Microsoft.EntityFrameworkCore.Migrations;

namespace Steamfitter.Api.Migrations.PostgreSQL.Migrations
{
    public partial class nullScenarioTemplateId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_scenarios_scenario_templates_scenario_template_id",
                table: "scenarios");

            migrationBuilder.AddForeignKey(
                name: "FK_scenarios_scenario_templates_scenario_template_id",
                table: "scenarios",
                column: "scenario_template_id",
                principalTable: "scenario_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_scenarios_scenario_templates_scenario_template_id",
                table: "scenarios");

            migrationBuilder.AddForeignKey(
                name: "FK_scenarios_scenario_templates_scenario_template_id",
                table: "scenarios",
                column: "scenario_template_id",
                principalTable: "scenario_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
