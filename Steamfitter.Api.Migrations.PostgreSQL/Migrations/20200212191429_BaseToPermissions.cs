// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Steamfitter.Api.Migrations.PostgreSQL.Migrations
{
    public partial class BaseToPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "permissions",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "date_created",
                table: "permissions",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "date_modified",
                table: "permissions",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "modified_by",
                table: "permissions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "date_created",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "date_modified",
                table: "permissions");

            migrationBuilder.DropColumn(
                name: "modified_by",
                table: "permissions");
        }
    }
}

