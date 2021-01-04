// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Steamfitter.Api.Migrations.PostgreSQL.Migrations
{
    public partial class BondAgents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "exercise_agents",
                newName: "bond_agents");

            migrationBuilder.RenameColumn(
                name: "exercise_agent_id",
                table: "ssh_port",
                newName: "bond_agent_id");

            migrationBuilder.RenameColumn(
                name: "exercise_agent_id",
                table: "monitored_tool",
                newName: "bond_agent_id");

            migrationBuilder.RenameColumn(
                name: "exercise_agent_id",
                table: "local_user",
                newName: "bond_agent_id");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "bond_agent_id",
                table: "local_user",
                newName: "exercise_agent_id");

            migrationBuilder.RenameColumn(
                name: "bond_agent_id",
                table: "monitored_tool",
                newName: "exercise_agent_id");

            migrationBuilder.RenameColumn(
                name: "bond_agent_id",
                table: "ssh_port",
                newName: "exercise_agent_id");

            migrationBuilder.RenameTable(
                name: "bond_agents",
                newName: "exercise_agents");

        }
    }
}
