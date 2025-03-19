// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Steamfitter.Api.Infrastructure.Mappings
{
    using System.Linq;
    using AutoMapper;
    using Steamfitter.Api.Data.Models;
    using Steamfitter.Api.ViewModels;

    public class GroupMembershipProfile : AutoMapper.Profile
    {
        public GroupMembershipProfile()
        {
            CreateMap<GroupMembershipEntity, GroupMembership>();
            CreateMap<GroupMembership, GroupMembershipEntity>();
        }
    }
}