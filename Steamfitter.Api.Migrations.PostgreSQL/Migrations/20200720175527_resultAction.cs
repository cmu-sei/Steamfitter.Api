// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.EntityFrameworkCore.Migrations;

namespace Steamfitter.Api.Migrations.PostgreSQL.Migrations
{
    public partial class resultAction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "action",
                table: "results",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("update results r set action = t.action from tasks t where r.task_id = t.id;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "action",
                table: "results");
        }
    }
}
