// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Steamfitter.Api.Data.Models;
using Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Infrastructure.Mappings
{
    public class PermissionProfile : AutoMapper.Profile
    {
        public PermissionProfile()
        {
            CreateMap<PermissionEntity, Permission>();

            CreateMap<Permission, PermissionEntity>();
        }
    }
}


