using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Steamfitter.Api.Migrations.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class groups_and_roles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "role_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scenario_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    all_permissions = table.Column<bool>(type: "boolean", nullable: false),
                    permissions = table.Column<int[]>(type: "integer[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scenario_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scenario_template_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    all_permissions = table.Column<bool>(type: "boolean", nullable: false),
                    permissions = table.Column<int[]>(type: "integer[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scenario_template_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    all_permissions = table.Column<bool>(type: "boolean", nullable: false),
                    immutable = table.Column<bool>(type: "boolean", nullable: false),
                    permissions = table.Column<int[]>(type: "integer[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "group_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_memberships", x => x.id);
                    table.ForeignKey(
                        name: "FK_group_memberships_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scenario_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    scenario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValue: new Guid("f870d8ee-7332-4f7f-8ee0-63bd07cfd7e4"))
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scenario_memberships", x => x.id);
                    table.ForeignKey(
                        name: "FK_scenario_memberships_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_scenario_memberships_scenario_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "scenario_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_scenario_memberships_scenarios_scenario_id",
                        column: x => x.scenario_id,
                        principalTable: "scenarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_scenario_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "scenario_template_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    scenario_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValue: new Guid("f870d8ee-7332-4f7f-8ee0-63bd07cfd7e4"))
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scenario_template_memberships", x => x.id);
                    table.ForeignKey(
                        name: "FK_scenario_template_memberships_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_scenario_template_memberships_scenario_template_roles_role_~",
                        column: x => x.role_id,
                        principalTable: "scenario_template_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_scenario_template_memberships_scenario_templates_scenario_t~",
                        column: x => x.scenario_template_id,
                        principalTable: "scenario_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_scenario_template_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.InsertData(
                table: "scenario_roles",
                columns: new[] { "id", "all_permissions", "description", "name", "permissions" },
                values: new object[,]
                {
                    { new Guid("1a3f26cd-9d99-4b98-b914-12931e786198"), true, "Can perform all actions on the Scenario", "Manager", new int[0] },
                    { new Guid("39aa296e-05ba-4fb0-8d74-c92cf3354c6f"), false, "Has read only access to the Scenario", "Observer", new[] { 0 } },
                    { new Guid("f870d8ee-7332-4f7f-8ee0-63bd07cfd7e4"), false, "Has read only access to the Scenario", "Member", new[] { 0, 1 } }
                });

            migrationBuilder.InsertData(
                table: "scenario_template_roles",
                columns: new[] { "id", "all_permissions", "description", "name", "permissions" },
                values: new object[,]
                {
                    { new Guid("1a3f26cd-9d99-4b98-b914-12931e786198"), true, "Can perform all actions on the ScenarioTemplate", "Manager", new int[0] },
                    { new Guid("39aa296e-05ba-4fb0-8d74-c92cf3354c6f"), false, "Has read only access to the ScenarioTemplate", "Observer", new[] { 0 } },
                    { new Guid("f870d8ee-7332-4f7f-8ee0-63bd07cfd7e4"), false, "Has read only access to the ScenarioTemplate", "Member", new[] { 0, 1 } }
                });

            migrationBuilder.InsertData(
                table: "system_roles",
                columns: new[] { "id", "all_permissions", "description", "immutable", "name", "permissions" },
                values: new object[,]
                {
                    { new Guid("1da3027e-725d-4753-9455-a836ed9bdb1e"), false, "Can perform all View actions, but not make any changes.", false, "Observer", new[] { 1, 5, 10, 12, 14 } },
                    { new Guid("d80b73c3-95d7-4468-8650-c62bbd082507"), false, "Can create and manage their own Projects.", false, "Content Developer", new[] { 0, 4, 7 } },
                    { new Guid("f35e8fff-f996-4cba-b303-3ba515ad8d2f"), true, "Can perform all actions", true, "Administrator", new int[0] }
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_role_id",
                table: "users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_memberships_group_id_user_id",
                table: "group_memberships",
                columns: new[] { "group_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_memberships_user_id",
                table: "group_memberships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_scenario_memberships_group_id",
                table: "scenario_memberships",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_scenario_memberships_role_id",
                table: "scenario_memberships",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_scenario_memberships_scenario_id_user_id_group_id",
                table: "scenario_memberships",
                columns: new[] { "scenario_id", "user_id", "group_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scenario_memberships_user_id",
                table: "scenario_memberships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_scenario_template_memberships_group_id",
                table: "scenario_template_memberships",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_scenario_template_memberships_role_id",
                table: "scenario_template_memberships",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_scenario_template_memberships_scenario_template_id_user_id_~",
                table: "scenario_template_memberships",
                columns: new[] { "scenario_template_id", "user_id", "group_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scenario_template_memberships_user_id",
                table: "scenario_template_memberships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_system_roles_name",
                table: "system_roles",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_users_system_roles_role_id",
                table: "users",
                column: "role_id",
                principalTable: "system_roles",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_system_roles_role_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "group_memberships");

            migrationBuilder.DropTable(
                name: "scenario_memberships");

            migrationBuilder.DropTable(
                name: "scenario_template_memberships");

            migrationBuilder.DropTable(
                name: "system_roles");

            migrationBuilder.DropTable(
                name: "scenario_roles");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropTable(
                name: "scenario_template_roles");

            migrationBuilder.DropIndex(
                name: "IX_users_role_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "role_id",
                table: "users");
        }
    }
}
