// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Steamfitter.Api.ViewModels;
using Steamfitter.Api.Data.Models;

namespace Steamfitter.Api.Infrastructure.Mapping
{
    public class ScenarioTemplateMembershipProfile : Profile
    {
        public ScenarioTemplateMembershipProfile()
        {
            CreateMap<ScenarioTemplateMembershipEntity, ScenarioTemplateMembership>();
            CreateMap<ScenarioTemplateMembership, ScenarioTemplateMembershipEntity>()
                .ForMember(dest => dest.ScenarioTemplate, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Group, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore());
        }
    }
}