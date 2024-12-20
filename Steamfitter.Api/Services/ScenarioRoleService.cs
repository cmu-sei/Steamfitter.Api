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
    public interface IScenarioRoleService
    {
        STT.Task<IEnumerable<ViewModels.ScenarioRole>> GetAsync(CancellationToken ct);
        STT.Task<ViewModels.ScenarioRole> GetAsync(Guid id, CancellationToken ct);
    }

    public class ScenarioRoleService : IScenarioRoleService
    {
        private readonly SteamfitterContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;

        public ScenarioRoleService(SteamfitterContext context, IAuthorizationService authorizationService, IPrincipal user, IMapper mapper)
        {
            _context = context;
            _authorizationService = authorizationService;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
        }

        public async STT.Task<IEnumerable<ViewModels.ScenarioRole>> GetAsync(CancellationToken ct)
        {
            var items = await _context.ScenarioRoles
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<SAVM.ScenarioRole>>(items);
        }

        public async STT.Task<ViewModels.ScenarioRole> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.ScenarioRoles
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            if (item == null)
                throw new EntityNotFoundException<ScenarioRole>();

            return _mapper.Map<SAVM.ScenarioRole>(item);
        }

    }
}
