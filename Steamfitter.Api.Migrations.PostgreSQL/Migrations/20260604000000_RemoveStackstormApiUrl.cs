// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Steamfitter.Api.Migrations.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStackstormApiUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Action enum mapping (see Steamfitter.Api.Data.Enumerations.TaskAction):
            //   100 guest_process_run
            //   101 guest_file_read
            //   102 guest_file_write
            //   103 vm_hw_power_off
            //   104 vm_hw_power_on
            //   105 vm_create_from_template
            //   106 vm_hw_remove
            //   107 guest_process_run_fast
            //   108 guest_file_upload_content
            //   109 send_email
            //   110 az_vm_shell_script
            //   111 az_get_vms
            //   112 az_vm_power_off
            //   113 az_vm_power_on
            //   114 guest_file_upload_file
            //   115 linux_file_touch
            //   116 linux_rm
            //   117 core_remote
            const string vmActions = "(100,101,102,103,104,105,106,107,108,114)";
            const string emailActions = "(109)";
            const string azureActions = "(110,111,112,113)";
            const string sshActions = "(115,116,117)";

            foreach (var table in new[] { "tasks", "results" })
            {
                migrationBuilder.Sql($"UPDATE {table} SET api_url = 'vm'    WHERE api_url = 'stackstorm' AND action IN {vmActions};");
                migrationBuilder.Sql($"UPDATE {table} SET api_url = 'email' WHERE api_url = 'stackstorm' AND action IN {emailActions};");
                migrationBuilder.Sql($"UPDATE {table} SET api_url = 'azure' WHERE api_url = 'stackstorm' AND action IN {azureActions};");
                migrationBuilder.Sql($"UPDATE {table} SET api_url = 'ssh'   WHERE api_url = 'stackstorm' AND action IN {sshActions};");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore prior 'stackstorm' value for any rows whose api_url was rewritten.
            migrationBuilder.Sql("UPDATE tasks   SET api_url = 'stackstorm' WHERE api_url IN ('vm','email','azure','ssh');");
            migrationBuilder.Sql("UPDATE results SET api_url = 'stackstorm' WHERE api_url IN ('vm','email','azure','ssh');");
        }
    }
}
