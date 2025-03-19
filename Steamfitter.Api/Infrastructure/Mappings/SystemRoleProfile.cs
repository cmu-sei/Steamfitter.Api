// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Steamfitter.Api.Data;
using Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Infrastructure.Mappings
{

    public class SystemRoleProfile : Profile
    {
        public SystemRoleProfile()
        {
            CreateMap<Data.Models.SystemRoleEntity, SystemRole>();
            CreateMap<SystemRole, Data.Models.SystemRoleEntity>();
        }
    }
}