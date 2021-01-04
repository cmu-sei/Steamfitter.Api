// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Steamfitter.Api.Data.Models;
using Steamfitter.Api.ViewModels;
using System;
using System.Text.Json;
using System.Collections.Generic;

namespace Steamfitter.Api.Infrastructure.Mappings
{
    public class VmCredentialProfile : AutoMapper.Profile
    {
        public VmCredentialProfile()
        {
            CreateMap<VmCredentialEntity, VmCredential>();

            CreateMap<VmCredential, VmCredentialEntity>();

            CreateMap<VmCredentialEntity, VmCredentialEntity>()
                .ForMember(e => e.Id, opt => opt.Ignore());

        }

    }
}
