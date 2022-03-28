// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Steamfitter.Api.Data.Models;
using System.Collections.Generic;

namespace Steamfitter.Api.Infrastructure.Options
{
    public class SeedDataOptions
    {
        public List<PermissionEntity> Permissions { get; set; } = new List<PermissionEntity>();
        public List<UserEntity> Users { get; set; } = new List<UserEntity>();
        public List<UserPermissionEntity> UserPermissions { get; set; } = new List<UserPermissionEntity>();
    }
}

