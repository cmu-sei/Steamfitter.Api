// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Services;
using Steamfitter.Api.ViewModels;
using System.Security.Claims;

namespace Steamfitter.Api.Infrastructure.Mappings
{
    public class ScenarioProfile : AutoMapper.Profile
    {
        public ScenarioProfile()
        {
            CreateMap<ScenarioEntity, Scenario>();

            CreateMap<Scenario, ScenarioEntity>();

            CreateMap<ScenarioEntity, ScenarioEntity>()
                .ForMember(e => e.Id, opt => opt.Ignore());

        }
    }
}


