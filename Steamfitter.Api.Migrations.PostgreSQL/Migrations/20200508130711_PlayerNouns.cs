// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.EntityFrameworkCore.Migrations;

namespace Steamfitter.Api.Migrations.PostgreSQL.Migrations
{
    public partial class PlayerNouns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "exercise_id",
                table: "scenarios",
                newName: "view_id");

            migrationBuilder.RenameColumn(
                name: "exercise",
                table: "scenarios",
                newName: "view");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "view_id",
                table: "scenarios",
                newName: "exercise_id");

            migrationBuilder.RenameColumn(
                name: "view",
                table: "scenarios",
                newName: "exercise");
        }
    }
}
