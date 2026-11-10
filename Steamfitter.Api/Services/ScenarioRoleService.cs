// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Steamfitter.Api.Infrastructure.Mappings;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using STT = System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data;
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
        private readonly ClaimsPrincipal _user;

        public ScenarioRoleService(SteamfitterContext context, IPrincipal user)
        {
            _context = context;
            _user = user as ClaimsPrincipal;
        }

        public async STT.Task<IEnumerable<ViewModels.ScenarioRole>> GetAsync(CancellationToken ct)
        {
            var items = await _context.ScenarioRoles
                .ToListAsync(ct);

            return items.Select(x => x.ToScenarioRole()).ToList();
        }

        public async STT.Task<ViewModels.ScenarioRole> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.ScenarioRoles
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            if (item == null)
                throw new EntityNotFoundException<ScenarioRole>();

            return item.ToScenarioRole();
        }

    }
}
