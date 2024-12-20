// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using STT = System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Infrastructure.Exceptions;
using SAVM = Steamfitter.Api.ViewModels;
using Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Services
{
    public interface IScenarioTemplateRoleService
    {
        STT.Task<IEnumerable<ViewModels.ScenarioTemplateRole>> GetAsync(CancellationToken ct);
        STT.Task<ViewModels.ScenarioTemplateRole> GetAsync(Guid id, CancellationToken ct);
    }

    public class ScenarioTemplateRoleService : IScenarioTemplateRoleService
    {
        private readonly SteamfitterContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;

        public ScenarioTemplateRoleService(SteamfitterContext context, IAuthorizationService authorizationService, IPrincipal user, IMapper mapper)
        {
            _context = context;
            _authorizationService = authorizationService;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
        }

        public async STT.Task<IEnumerable<ViewModels.ScenarioTemplateRole>> GetAsync(CancellationToken ct)
        {
            var items = await _context.ScenarioTemplateRoles
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<SAVM.ScenarioTemplateRole>>(items);
        }

        public async STT.Task<ViewModels.ScenarioTemplateRole> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.ScenarioTemplateRoles
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            if (item == null)
                throw new EntityNotFoundException<ScenarioTemplateRole>();

            return _mapper.Map<SAVM.ScenarioTemplateRole>(item);
        }

    }
}
