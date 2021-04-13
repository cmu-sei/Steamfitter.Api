// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.ViewModels;
using System.Linq;

namespace Steamfitter.Api.Infrastructure.Mappings
{
    public class UserProfile : AutoMapper.Profile
    {
        public UserProfile()
        {
            CreateMap<UserEntity, User>()
                .ForMember(m => m.Permissions, opt => opt.MapFrom(x => x.UserPermissions.Select(y => y.Permission)))
                .ForMember(m => m.Permissions, opt => opt.ExplicitExpansion());
            CreateMap<User, UserEntity>()
                .ForMember(m => m.UserPermissions, opt => opt.Ignore());
        }
    }
}


