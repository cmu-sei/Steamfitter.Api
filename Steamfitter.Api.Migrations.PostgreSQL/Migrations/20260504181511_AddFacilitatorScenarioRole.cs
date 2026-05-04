using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Steamfitter.Api.Migrations.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddFacilitatorScenarioRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "scenario_roles",
                keyColumn: "id",
                keyValue: new Guid("f870d8ee-7332-4f7f-8ee0-63bd07cfd7e4"),
                column: "description",
                value: "Can edit the Scenario");

            migrationBuilder.InsertData(
                table: "scenario_roles",
                columns: new[] { "id", "all_permissions", "description", "name", "permissions" },
                values: new object[] { new Guid("b6b4a1f2-8f3b-4d1a-9a5c-3e7b0d4f2c81"), false, "Can view and execute the Scenario, but cannot edit it", "Facilitator", new[] { 0, 2 } });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "scenario_roles",
                keyColumn: "id",
                keyValue: new Guid("b6b4a1f2-8f3b-4d1a-9a5c-3e7b0d4f2c81"));

            migrationBuilder.UpdateData(
                table: "scenario_roles",
                keyColumn: "id",
                keyValue: new Guid("f870d8ee-7332-4f7f-8ee0-63bd07cfd7e4"),
                column: "description",
                value: "Has read only access to the Scenario");
        }
    }
}
