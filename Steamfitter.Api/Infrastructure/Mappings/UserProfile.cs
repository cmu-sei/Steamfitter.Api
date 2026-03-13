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
            CreateMap<UserEntity, User>();
            CreateMap<User, UserEntity>()
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.ScenarioMemberships, opt => opt.Ignore())
                .ForMember(dest => dest.ScenarioTemplateMemberships, opt => opt.Ignore())
                .ForMember(dest => dest.GroupMemberships, opt => opt.Ignore());
        }
    }
}
