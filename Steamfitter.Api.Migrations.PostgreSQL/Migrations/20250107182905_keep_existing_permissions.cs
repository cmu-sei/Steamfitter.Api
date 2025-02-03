/*
Copyright 2021 Carnegie Mellon University. All Rights Reserved. 
 Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.
*/

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Steamfitter.Api.Migrations.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class keep_existing_permissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE users
                SET role_id = (
                    SELECT id
                    FROM system_roles
                    WHERE name = 'Content Developer'
                )
                WHERE id IN (
                    SELECT user_id
                    FROM user_permissions
                    WHERE permission_id = (
                        SELECT id
                        FROM permissions
                        WHERE key = 'ContentDeveloper'
                    )
                )
            ");

            migrationBuilder.Sql(@"
                UPDATE users
                SET role_id = (
                    SELECT id
                    FROM system_roles
                    WHERE name = 'Administrator'
                )
                WHERE id IN (
                    SELECT user_id
                    FROM user_permissions
                    WHERE permission_id = (
                        SELECT id
                        FROM permissions
                        WHERE key = 'SystemAdmin'
                    )
                )
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE users SET role_id = null");
        }
    }
}
