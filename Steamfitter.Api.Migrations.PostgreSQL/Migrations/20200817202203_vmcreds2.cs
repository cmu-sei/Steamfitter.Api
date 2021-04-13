// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.EntityFrameworkCore.Migrations;

namespace Steamfitter.Api.Migrations.PostgreSQL.Migrations
{
    public partial class vmcreds2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_vm_credential_entity_scenarios_scenario_id",
                table: "vm_credential_entity");

            migrationBuilder.DropForeignKey(
                name: "FK_vm_credential_entity_scenario_templates_scenario_template_id",
                table: "vm_credential_entity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_vm_credential_entity",
                table: "vm_credential_entity");

            migrationBuilder.RenameTable(
                name: "vm_credential_entity",
                newName: "vm_credentials");

            migrationBuilder.RenameIndex(
                name: "IX_vm_credential_entity_scenario_template_id",
                table: "vm_credentials",
                newName: "IX_vm_credentials_scenario_template_id");

            migrationBuilder.RenameIndex(
                name: "IX_vm_credential_entity_scenario_id",
                table: "vm_credentials",
                newName: "IX_vm_credentials_scenario_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_vm_credentials",
                table: "vm_credentials",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_vm_credentials_scenarios_scenario_id",
                table: "vm_credentials",
                column: "scenario_id",
                principalTable: "scenarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_vm_credentials_scenario_templates_scenario_template_id",
                table: "vm_credentials",
                column: "scenario_template_id",
                principalTable: "scenario_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_vm_credentials_scenarios_scenario_id",
                table: "vm_credentials");

            migrationBuilder.DropForeignKey(
                name: "FK_vm_credentials_scenario_templates_scenario_template_id",
                table: "vm_credentials");

            migrationBuilder.DropPrimaryKey(
                name: "PK_vm_credentials",
                table: "vm_credentials");

            migrationBuilder.RenameTable(
                name: "vm_credentials",
                newName: "vm_credential_entity");

            migrationBuilder.RenameIndex(
                name: "IX_vm_credentials_scenario_template_id",
                table: "vm_credential_entity",
                newName: "IX_vm_credential_entity_scenario_template_id");

            migrationBuilder.RenameIndex(
                name: "IX_vm_credentials_scenario_id",
                table: "vm_credential_entity",
                newName: "IX_vm_credential_entity_scenario_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_vm_credential_entity",
                table: "vm_credential_entity",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_vm_credential_entity_scenarios_scenario_id",
                table: "vm_credential_entity",
                column: "scenario_id",
                principalTable: "scenarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_vm_credential_entity_scenario_templates_scenario_template_id",
                table: "vm_credential_entity",
                column: "scenario_template_id",
                principalTable: "scenario_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
