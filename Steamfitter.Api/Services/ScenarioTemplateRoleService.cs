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
    public interface IScenarioTemplateRoleService
    {
        STT.Task<IEnumerable<ViewModels.ScenarioTemplateRole>> GetAsync(CancellationToken ct);
        STT.Task<ViewModels.ScenarioTemplateRole> GetAsync(Guid id, CancellationToken ct);
    }

    public class ScenarioTemplateRoleService : IScenarioTemplateRoleService
    {
        private readonly SteamfitterContext _context;
        private readonly ClaimsPrincipal _user;

        public ScenarioTemplateRoleService(SteamfitterContext context, IPrincipal user)
        {
            _context = context;
            _user = user as ClaimsPrincipal;
        }

        public async STT.Task<IEnumerable<ViewModels.ScenarioTemplateRole>> GetAsync(CancellationToken ct)
        {
            var items = await _context.ScenarioTemplateRoles
                .ToListAsync(ct);

            return items.Select(x => x.ToScenarioTemplateRole()).ToList();
        }

        public async STT.Task<ViewModels.ScenarioTemplateRole> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.ScenarioTemplateRoles
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            if (item == null)
                throw new EntityNotFoundException<ScenarioTemplateRole>();

            return item.ToScenarioTemplateRole();
        }

    }
}
