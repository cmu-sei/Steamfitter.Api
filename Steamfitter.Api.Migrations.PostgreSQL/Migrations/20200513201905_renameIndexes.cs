// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.EntityFrameworkCore.Migrations;

namespace Steamfitter.Api.Migrations.PostgreSQL.Migrations
{
    public partial class renameIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename Indexes
            migrationBuilder.RenameIndex(
                name: "IX_exercise_agents_operating_system_id",
                table: "bond_agents",
                newName: "IX_bond_agents_operating_system_id");

            migrationBuilder.RenameIndex(
                name: "IX_local_user_exercise_agent_id",
                table: "local_user",
                newName: "IX_local_user_bond_agent_id");

            migrationBuilder.RenameIndex(
                name: "IX_monitored_tool_exercise_agent_id",
                table: "monitored_tool",
                newName: "IX_monitored_tool_bond_agent_id");

            migrationBuilder.RenameIndex(
                name: "IX_dispatch_task_results_dispatch_task_id",
                table: "results",
                newName: "IX_results_task_id");

            migrationBuilder.RenameIndex(
                name: "IX_sessions_scenario_id",
                table: "scenarios",
                newName: "IX_scenarios_scenario_template_id");

            migrationBuilder.RenameIndex(
                name: "IX_ssh_port_exercise_agent_id",
                table: "ssh_port",
                newName: "IX_ssh_port_bond_agent_id");

            migrationBuilder.RenameIndex(
                name: "IX_dispatch_tasks_scenario_id",
                table: "tasks",
                newName: "IX_tasks_scenario_template_id");

            migrationBuilder.RenameIndex(
                name: "IX_dispatch_tasks_session_id",
                table: "tasks",
                newName: "IX_tasks_scenario_id");

            migrationBuilder.RenameIndex(
                name: "IX_dispatch_tasks_trigger_task_id",
                table: "tasks",
                newName: "IX_tasks_trigger_task_id");

            migrationBuilder.RenameIndex(
                name: "IX_dispatch_tasks_user_id",
                table: "tasks",
                newName: "IX_tasks_user_id");

            // Drop old name foreign keys
            migrationBuilder.DropForeignKey(
                name: "FK_exercise_agents_os_operating_system_id",
                table: "bond_agents");

            migrationBuilder.DropForeignKey(
                name: "FK_local_user_exercise_agents_exercise_agent_id",
                table: "local_user");

            migrationBuilder.DropForeignKey(
                name: "FK_monitored_tool_exercise_agents_exercise_agent_id",
                table: "monitored_tool");

            migrationBuilder.DropForeignKey(
                name: "FK_dispatch_task_results_dispatch_tasks_dispatch_task_id",
                table: "results");

            migrationBuilder.DropForeignKey(
                name: "FK_sessions_scenarios_scenario_id",
                table: "scenarios");

            migrationBuilder.DropForeignKey(
                name: "FK_ssh_port_exercise_agents_exercise_agent_id",
                table: "ssh_port");

            migrationBuilder.DropForeignKey(
                name: "FK_dispatch_tasks_dispatch_tasks_trigger_task_id",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_dispatch_tasks_scenarios_scenario_id",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_dispatch_tasks_sessions_session_id",
                table: "tasks");

            // drop old name primary keys
            migrationBuilder.DropPrimaryKey(
                name: "PK_exercise_agents",
                table: "bond_agents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_dispatch_task_results",
                table: "results");

            migrationBuilder.DropPrimaryKey(
                name: "PK_scenarios",
                table: "scenario_templates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sessions",
                table: "scenarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_dispatch_tasks",
                table: "tasks");

            // add new name primary keys
            migrationBuilder.AddPrimaryKey(
                name: "PK_bond_agents",
                table: "bond_agents",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_results",
                table: "results",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_scenarios",
                table: "scenarios",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_scenario_templates",
                table: "scenario_templates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tasks",
                table: "tasks",
                column: "id");

            // add new name foreign keys
            migrationBuilder.AddForeignKey(
                name: "FK_bond_agents_os_operating_system_id",
                table: "bond_agents",
                column: "operating_system_id",
                principalTable: "os",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_local_user_bond_agents_bond_agent_id",
                table: "local_user",
                column: "bond_agent_id",
                principalTable: "bond_agents",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_monitored_tool_bond_agents_bond_agent_id",
                table: "monitored_tool",
                column: "bond_agent_id",
                principalTable: "bond_agents",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_results_tasks_task_id",
                table: "results",
                column: "task_id",
                principalTable: "tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_scenarios_scenario_templates_scenario_template_id",
                table: "scenarios",
                column: "scenario_template_id",
                principalTable: "scenario_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ssh_port_bond_agents_bond_agent_id",
                table: "ssh_port",
                column: "bond_agent_id",
                principalTable: "bond_agents",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_tasks_trigger_task_id",
                table: "tasks",
                column: "trigger_task_id",
                principalTable: "tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_scenario_templates_scenario_template_id",
                table: "tasks",
                column: "scenario_template_id",
                principalTable: "scenario_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_scenarios_scenario_id",
                table: "tasks",
                column: "scenario_id",
                principalTable: "scenarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rename Indexes
            migrationBuilder.RenameIndex(
                newName: "IX_exercise_agents_operating_system_id",
                table: "bond_agents",
                name: "IX_bond_agents_operating_system_id");

            migrationBuilder.RenameIndex(
                newName: "IX_local_user_exercise_agent_id",
                table: "local_user",
                name: "IX_local_user_bond_agent_id");

            migrationBuilder.RenameIndex(
                newName: "IX_monitored_tool_exercise_agent_id",
                table: "monitored_tool",
                name: "IX_monitored_tool_bond_agent_id");

            migrationBuilder.RenameIndex(
                newName: "IX_dispatch_task_results_dispatch_task_id",
                table: "results",
                name: "IX_results_task_id");

            migrationBuilder.RenameIndex(
                newName: "IX_sessions_scenario_id",
                table: "scenarios",
                name: "IX_scenarios_scenario_template_id");

            migrationBuilder.RenameIndex(
                newName: "IX_ssh_port_exercise_agent_id",
                table: "ssh_port",
                name: "IX_ssh_port_bond_agent_id");

            migrationBuilder.RenameIndex(
                newName: "IX_dispatch_tasks_scenario_id",
                table: "tasks",
                name: "IX_tasks_scenario_template_id");

            migrationBuilder.RenameIndex(
                newName: "IX_dispatch_tasks_session_id",
                table: "tasks",
                name: "IX_tasks_scenario_id");

            migrationBuilder.RenameIndex(
                newName: "IX_dispatch_tasks_trigger_task_id",
                table: "tasks",
                name: "IX_tasks_trigger_task_id");

            migrationBuilder.RenameIndex(
                newName: "IX_dispatch_tasks_user_id",
                table: "tasks",
                name: "IX_tasks_user_id");

            // Drop new name foreign keys
            migrationBuilder.DropForeignKey(
                name: "FK_bond_agents_os_operating_system_id",
                table: "bond_agents");

            migrationBuilder.DropForeignKey(
                name: "FK_local_user_bond_agents_bond_agent_id",
                table: "local_user");

            migrationBuilder.DropForeignKey(
                name: "FK_monitored_tool_bond_agents_bond_agent_id",
                table: "monitored_tool");

            migrationBuilder.DropForeignKey(
                name: "FK_results_tasks_task_id",
                table: "results");

            migrationBuilder.DropForeignKey(
                name: "FK_scenarios_scenario_templates_scenario_template_id",
                table: "scenarios");

            migrationBuilder.DropForeignKey(
                name: "FK_ssh_port_bond_agents_bond_agent_id",
                table: "ssh_port");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_tasks_trigger_task_id",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_scenario_templates_scenario_template_id",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_scenarios_scenario_id",
                table: "tasks");

            // drop new name primary keys
            migrationBuilder.DropPrimaryKey(
                name: "PK_bond_agents",
                table: "bond_agents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_results",
                table: "results");

            migrationBuilder.DropPrimaryKey(
                name: "PK_scenario_Templates",
                table: "scenario_templates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_scenarios",
                table: "scenarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tasks",
                table: "tasks");

            // add old name primary keys
            migrationBuilder.AddPrimaryKey(
                name: "PK_exercise_agents",
                table: "bond_agents",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_dispatch_task_results",
                table: "results",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sessions",
                table: "scenarios",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_scenarios",
                table: "scenario_templates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_dispatch_tasks",
                table: "tasks",
                column: "id");

            // add old name foreign keys
            migrationBuilder.AddForeignKey(
                name: "FK_exercise_agents_os_operating_system_id",
                table: "exercise_agents",
                column: "operating_system_id",
                principalTable: "os",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_local_user_exercise_agents_exercise_agent_id",
                table: "local_user",
                column: "bond_agent_id",
                principalTable: "bond_agents",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_monitored_tool_exercise_agents_exercise_agent_id",
                table: "monitored_tool",
                column: "bond_agent_id",
                principalTable: "bond_agents",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dispatch_task_results_dispatch_tasks_dispatch_task_id",
                table: "results",
                column: "task_id",
                principalTable: "tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_sessions_scenarios_session_template_id",
                table: "scenarios",
                column: "scenario_template_id",
                principalTable: "scenario_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ssh_port_exercise_agents_exercise_agent_id",
                table: "ssh_port",
                column: "bond_agent_id",
                principalTable: "bond_agents",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dispatch_tasks_dispatch_tasks_trigger_task_id",
                table: "tasks",
                column: "trigger_task_id",
                principalTable: "tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_dispatch_tasks_scenarios_scenario_id",
                table: "tasks",
                column: "scenario_template_id",
                principalTable: "scenario_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_dispatch_tasks_sessions_session_id",
                table: "tasks",
                column: "scenario_id",
                principalTable: "scenarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

        }
    }
}
