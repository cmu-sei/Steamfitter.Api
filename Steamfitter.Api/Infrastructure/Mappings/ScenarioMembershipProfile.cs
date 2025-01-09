// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Steamfitter.Api.ViewModels;
using Steamfitter.Api.Data.Models;

namespace Steamfitter.Api.Infrastructure.Mapping
{
    public class ScenarioMembershipProfile : Profile
    {
        public ScenarioMembershipProfile()
        {
            CreateMap<ScenarioMembershipEntity, ScenarioMembership>();
        }
    }
}