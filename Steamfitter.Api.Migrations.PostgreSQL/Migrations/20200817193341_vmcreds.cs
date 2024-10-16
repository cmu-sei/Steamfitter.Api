// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Steamfitter.Api.Migrations.PostgreSQL.Migrations
{
    public partial class Vmcreds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "default_vm_credential_id",
                table: "scenarios",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "default_vm_credential_id",
                table: "scenario_templates",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "vm_credential_entity",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    date_created = table.Column<DateTime>(nullable: false),
                    date_modified = table.Column<DateTime>(nullable: true),
                    created_by = table.Column<Guid>(nullable: false),
                    modified_by = table.Column<Guid>(nullable: true),
                    scenario_template_id = table.Column<Guid>(nullable: true),
                    scenario_id = table.Column<Guid>(nullable: true),
                    username = table.Column<string>(nullable: true),
                    password = table.Column<string>(nullable: true),
                    description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vm_credential_entity", x => x.id);
                    table.ForeignKey(
                        name: "FK_vm_credential_entity_scenarios_scenario_id",
                        column: x => x.scenario_id,
                        principalTable: "scenarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vm_credential_entity_scenario_templates_scenario_template_id",
                        column: x => x.scenario_template_id,
                        principalTable: "scenario_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vm_credential_entity_scenario_id",
                table: "vm_credential_entity",
                column: "scenario_id");

            migrationBuilder.CreateIndex(
                name: "IX_vm_credential_entity_scenario_template_id",
                table: "vm_credential_entity",
                column: "scenario_template_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vm_credential_entity");

            migrationBuilder.DropColumn(
                name: "default_vm_credential_id",
                table: "scenarios");

            migrationBuilder.DropColumn(
                name: "default_vm_credential_id",
                table: "scenario_templates");
        }
    }
}
