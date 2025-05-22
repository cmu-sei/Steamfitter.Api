// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using STT = System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data;
using Steamfitter.Api.Infrastructure.Exceptions;
using SAVM = Steamfitter.Api.ViewModels;
using Steamfitter.Api.ViewModels;
using System.Linq;
using Steamfitter.Api.Data.Models;

namespace Steamfitter.Api.Services
{
    public interface IScenarioMembershipService
    {
        STT.Task<ViewModels.ScenarioMembership> GetAsync(Guid id, CancellationToken ct);
        STT.Task<IEnumerable<ViewModels.ScenarioMembership>> GetByScenarioAsync(Guid scenarioId, CancellationToken ct);
        STT.Task<ViewModels.ScenarioMembership> CreateAsync(ViewModels.ScenarioMembership scenarioMembership, CancellationToken ct);
        STT.Task<ViewModels.ScenarioMembership> UpdateAsync(Guid id, ViewModels.ScenarioMembership scenarioMembership, CancellationToken ct);
        STT.Task DeleteAsync(Guid id, CancellationToken ct);
    }

    public class ScenarioMembershipService : IScenarioMembershipService
    {
        private readonly SteamfitterContext _context;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;

        public ScenarioMembershipService(SteamfitterContext context, IPrincipal user, IMapper mapper)
        {
            _context = context;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
        }

        public async STT.Task<ViewModels.ScenarioMembership> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.ScenarioMemberships
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            if (item == null)
                throw new EntityNotFoundException<ScenarioMembership>();

            return _mapper.Map<SAVM.ScenarioMembership>(item);
        }

        public async STT.Task<IEnumerable<ViewModels.ScenarioMembership>> GetByScenarioAsync(Guid scenarioId, CancellationToken ct)
        {
            var items = await _context.ScenarioMemberships
                .Where(m => m.ScenarioId == scenarioId)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<SAVM.ScenarioMembership>>(items);
        }

        public async STT.Task<ViewModels.ScenarioMembership> CreateAsync(ViewModels.ScenarioMembership scenarioMembership, CancellationToken ct)
        {
            var scenarioMembershipEntity = _mapper.Map<ScenarioMembershipEntity>(scenarioMembership);

            _context.ScenarioMemberships.Add(scenarioMembershipEntity);
            await _context.SaveChangesAsync(ct);
            var scenario = await GetAsync(scenarioMembershipEntity.Id, ct);

            return scenario;
        }
        public async STT.Task<ViewModels.ScenarioMembership> UpdateAsync(Guid id, ViewModels.ScenarioMembership scenarioMembership, CancellationToken ct)
        {
            var scenarioMembershipToUpdate = await _context.ScenarioMemberships.SingleOrDefaultAsync(v => v.Id == id, ct);
            if (scenarioMembershipToUpdate == null)
                throw new EntityNotFoundException<SAVM.Scenario>();

            scenarioMembershipToUpdate.Role = null;
            scenarioMembershipToUpdate.RoleId = scenarioMembership.RoleId;
            await _context.SaveChangesAsync(ct);

            return _mapper.Map<SAVM.ScenarioMembership>(scenarioMembershipToUpdate);
        }
        public async STT.Task DeleteAsync(Guid id, CancellationToken ct)
        {
            var scenarioMembershipToDelete = await _context.ScenarioMemberships.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (scenarioMembershipToDelete == null)
                throw new EntityNotFoundException<SAVM.ScenarioMembership>();

            _context.ScenarioMemberships.Remove(scenarioMembershipToDelete);
            await _context.SaveChangesAsync(ct);

            return;
        }

    }
}
