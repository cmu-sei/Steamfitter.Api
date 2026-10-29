// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Services;
using Steamfitter.Api.ViewModels;
using System.Linq;
using System.Security.Claims;

namespace Steamfitter.Api.Infrastructure.Mappings
{
    public class ScenarioProfile : AutoMapper.Profile
    {
        public ScenarioProfile()
        {
            CreateMap<ScenarioSummary, Scenario>();
            CreateMap<ScenarioEntity, ScenarioSummary>();
            CreateMap<ScenarioEntity, Scenario>();
            CreateMap<Scenario, ScenarioEntity>();

            // a copy gets its own tasks, credentials and memberships; mapping the original's collections
            // would move whichever of their rows the context tracks onto the copy
            CreateMap<ScenarioEntity, ScenarioEntity>()
                .ForMember(e => e.Id, opt => opt.Ignore())
                .ForMember(e => e.Tasks, opt => opt.Ignore())
                .ForMember(e => e.VmCredentials, opt => opt.Ignore())
                .ForMember(e => e.Memberships, opt => opt.Ignore());

            CreateMap<ScenarioCreate, ScenarioEntity>(AutoMapper.MemberList.Source);
            CreateMap<ScenarioEdit, ScenarioEntity>(AutoMapper.MemberList.Source);
        }
    }
}
