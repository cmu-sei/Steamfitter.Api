using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Steamfitter.Api.Migrations.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class XapiFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_x_api_queued_statements_scenarios_scenario_id",
                table: "x_api_queued_statements");

            migrationBuilder.DropIndex(
                name: "IX_x_api_queued_statements_queued_at",
                table: "x_api_queued_statements");

            migrationBuilder.DropIndex(
                name: "IX_x_api_queued_statements_scenario_id",
                table: "x_api_queued_statements");

            migrationBuilder.DropIndex(
                name: "IX_x_api_queued_statements_status",
                table: "x_api_queued_statements");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_x_api_queued_statements_queued_at",
                table: "x_api_queued_statements",
                column: "queued_at");

            migrationBuilder.CreateIndex(
                name: "IX_x_api_queued_statements_scenario_id",
                table: "x_api_queued_statements",
                column: "scenario_id");

            migrationBuilder.CreateIndex(
                name: "IX_x_api_queued_statements_status",
                table: "x_api_queued_statements",
                column: "status");

            migrationBuilder.AddForeignKey(
                name: "FK_x_api_queued_statements_scenarios_scenario_id",
                table: "x_api_queued_statements",
                column: "scenario_id",
                principalTable: "scenarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
