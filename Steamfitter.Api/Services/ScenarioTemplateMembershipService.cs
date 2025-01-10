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
    public interface IScenarioTemplateMembershipService
    {
        STT.Task<ViewModels.ScenarioTemplateMembership> GetAsync(Guid id, CancellationToken ct);
        STT.Task<IEnumerable<ViewModels.ScenarioTemplateMembership>> GetByScenarioTemplateAsync(Guid scenarioTemplateId, CancellationToken ct);
        STT.Task<ViewModels.ScenarioTemplateMembership> CreateAsync(ViewModels.ScenarioTemplateMembership scenarioTemplateMembership, CancellationToken ct);
        STT.Task<ViewModels.ScenarioTemplateMembership> UpdateAsync(Guid id, ViewModels.ScenarioTemplateMembership scenarioTemplateMembership, CancellationToken ct);
        STT.Task DeleteAsync(Guid id, CancellationToken ct);
    }

    public class ScenarioTemplateMembershipService : IScenarioTemplateMembershipService
    {
        private readonly SteamfitterContext _context;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;

        public ScenarioTemplateMembershipService(SteamfitterContext context, IPrincipal user, IMapper mapper)
        {
            _context = context;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
        }

        public async STT.Task<ViewModels.ScenarioTemplateMembership> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.ScenarioTemplateMemberships
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            if (item == null)
                throw new EntityNotFoundException<ScenarioTemplateMembership>();

            return _mapper.Map<SAVM.ScenarioTemplateMembership>(item);
        }

        public async STT.Task<IEnumerable<ViewModels.ScenarioTemplateMembership>> GetByScenarioTemplateAsync(Guid scenarioTemplateId, CancellationToken ct)
        {
            var items = await _context.ScenarioTemplateMemberships
                .Where(m => m.ScenarioTemplateId == scenarioTemplateId)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<SAVM.ScenarioTemplateMembership>>(items);
        }

        public async STT.Task<ViewModels.ScenarioTemplateMembership> CreateAsync(ViewModels.ScenarioTemplateMembership scenarioTemplateMembership, CancellationToken ct)
        {
            var scenarioTemplateMembershipEntity = _mapper.Map<ScenarioTemplateMembershipEntity>(scenarioTemplateMembership);

            _context.ScenarioTemplateMemberships.Add(scenarioTemplateMembershipEntity);
            await _context.SaveChangesAsync(ct);
            var scenario = await GetAsync(scenarioTemplateMembershipEntity.Id, ct);

            return scenario;
        }
        public async STT.Task<ViewModels.ScenarioTemplateMembership> UpdateAsync(Guid id, ViewModels.ScenarioTemplateMembership scenarioTemplateMembership, CancellationToken ct)
        {
            var scenarioTemplateMembershipToUpdate = await _context.ScenarioTemplateMemberships.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (scenarioTemplateMembershipToUpdate == null)
                throw new EntityNotFoundException<SAVM.Scenario>();

            _mapper.Map(scenarioTemplateMembership, scenarioTemplateMembershipToUpdate);

            await _context.SaveChangesAsync(ct);

            return _mapper.Map<SAVM.ScenarioTemplateMembership>(scenarioTemplateMembershipToUpdate);
        }
        public async STT.Task DeleteAsync(Guid id, CancellationToken ct)
        {
            var scenarioTemplateMembershipToDelete = await _context.ScenarioTemplateMemberships.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (scenarioTemplateMembershipToDelete == null)
                throw new EntityNotFoundException<SAVM.ScenarioTemplateMembership>();

            _context.ScenarioTemplateMemberships.Remove(scenarioTemplateMembershipToDelete);
            await _context.SaveChangesAsync(ct);

            return;
        }

    }
}
