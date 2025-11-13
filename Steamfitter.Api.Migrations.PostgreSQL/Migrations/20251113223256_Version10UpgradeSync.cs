using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Steamfitter.Api.Migrations.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class Version10UpgradeSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "system_roles",
                keyColumn: "id",
                keyValue: new Guid("1da3027e-725d-4753-9455-a836ed9bdb1e"),
                column: "description",
                value: "Can View all Scenario Templates and Scenarios, but cannot make any changes.");

            migrationBuilder.UpdateData(
                table: "system_roles",
                keyColumn: "id",
                keyValue: new Guid("d80b73c3-95d7-4468-8650-c62bbd082507"),
                column: "description",
                value: "Can create and manage their own Scenario Templates and Scenarios.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "system_roles",
                keyColumn: "id",
                keyValue: new Guid("1da3027e-725d-4753-9455-a836ed9bdb1e"),
                column: "description",
                value: "Can perform all View actions, but not make any changes.");

            migrationBuilder.UpdateData(
                table: "system_roles",
                keyColumn: "id",
                keyValue: new Guid("d80b73c3-95d7-4468-8650-c62bbd082507"),
                column: "description",
                value: "Can create and manage their own Projects.");
        }
    }
}
