// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using STT = System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Hubs;
using Steamfitter.Api.Infrastructure.Extensions;
using Steamfitter.Api.Infrastructure.Exceptions;
using SAVM = Steamfitter.Api.ViewModels;
using Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Services
{
    public interface IScenarioTemplateService
    {
        STT.Task<IEnumerable<ViewModels.ScenarioTemplate>> GetAsync(CancellationToken ct);
        STT.Task<IEnumerable<ViewModels.ScenarioTemplate>> GetMineAsync(CancellationToken ct);
        STT.Task<ViewModels.ScenarioTemplate> GetAsync(Guid id, CancellationToken ct);
        STT.Task<ViewModels.ScenarioTemplate> CreateAsync(ViewModels.ScenarioTemplateForm scenarioTemplateForm, CancellationToken ct);
        STT.Task<ViewModels.ScenarioTemplate> CopyAsync(Guid id, CancellationToken ct);
        STT.Task<ViewModels.ScenarioTemplate> UpdateAsync(Guid id, ViewModels.ScenarioTemplateForm scenarioTemplateForm, CancellationToken ct);
        STT.Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }

    public class ScenarioTemplateService : IScenarioTemplateService
    {
        private readonly SteamfitterContext _context;
        private readonly ITaskService _taskService;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;
        private readonly IHubContext<EngineHub> _engineHub;

        public ScenarioTemplateService(
            SteamfitterContext context,
            ITaskService taskService,
            IPrincipal user,
            IMapper mapper,
            IHubContext<EngineHub> engineHub)
        {
            _context = context;
            _taskService = taskService;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
            _engineHub = engineHub;
        }

        public async STT.Task<IEnumerable<ViewModels.ScenarioTemplate>> GetAsync(CancellationToken ct)
        {
            var items = await _context.ScenarioTemplates.Include(st => st.VmCredentials)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<SAVM.ScenarioTemplate>>(items);
        }

        public async STT.Task<IEnumerable<ViewModels.ScenarioTemplate>> GetMineAsync(CancellationToken ct)
        {
            var userId = _user.GetId();
            var items = await _context.ScenarioTemplateMemberships
                .Where(m => m.ScenarioTemplate.CreatedBy == userId || m.UserId == userId)
                .Include(m => m.ScenarioTemplate)
                .ThenInclude(m => m.VmCredentials)
                .Select(m => m.ScenarioTemplate)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<SAVM.ScenarioTemplate>>(items);
        }

        public async STT.Task<ViewModels.ScenarioTemplate> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.ScenarioTemplates.Include(st => st.VmCredentials)
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            if (item == null)
                throw new EntityNotFoundException<ScenarioTemplate>();

            return _mapper.Map<SAVM.ScenarioTemplate>(item);
        }

        public async STT.Task<ViewModels.ScenarioTemplate> CreateAsync(ViewModels.ScenarioTemplateForm scenarioTemplateForm, CancellationToken ct)
        {
            var scenarioTemplateEntity = _mapper.Map<ScenarioTemplateEntity>(scenarioTemplateForm);
            scenarioTemplateEntity.DateCreated = DateTime.UtcNow;
            scenarioTemplateEntity.CreatedBy = _user.GetId();

            _context.ScenarioTemplates.Add(scenarioTemplateEntity);
            await _context.SaveChangesAsync(ct);
            var scenarioTemplate = await GetAsync(scenarioTemplateEntity.Id, ct);

            return scenarioTemplate;
        }

        public async STT.Task<ViewModels.ScenarioTemplate> CopyAsync(Guid oldScenarioTemplateId, CancellationToken ct)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            var oldScenarioTemplateEntity = _context.ScenarioTemplates.Find(oldScenarioTemplateId);
            if (oldScenarioTemplateEntity == null)
                throw new EntityNotFoundException<SAVM.ScenarioTemplate>($"ScenarioTemplate {oldScenarioTemplateId} was not found.");

            var newScenarioTemplateEntity = _mapper.Map<ScenarioTemplateEntity>(oldScenarioTemplateEntity);
            newScenarioTemplateEntity.CreatedBy = _user.GetId();
            newScenarioTemplateEntity.Name = $"{oldScenarioTemplateEntity.Name} - {_user.Claims.FirstOrDefault(c => c.Type == "name").Value}";

            _context.ScenarioTemplates.Add(newScenarioTemplateEntity);
            await _context.SaveChangesAsync(ct);

            // copy all of the VmCredentials
            var oldVmCredentials = _context.VmCredentials.Where(vmc => vmc.ScenarioTemplateId == oldScenarioTemplateId).ToList();
            var newDefaultVmCredentialId = await CopyVmCredentials(oldScenarioTemplateEntity.DefaultVmCredentialId, newScenarioTemplateEntity.Id, null, oldVmCredentials, ct);
            newScenarioTemplateEntity.DefaultVmCredentialId = newDefaultVmCredentialId;
            await _context.SaveChangesAsync(ct);

            // copy all of the Tasks, including children
            var oldTaskEntityIds = _context.Tasks.Where(dt => dt.ScenarioTemplateId == oldScenarioTemplateId && dt.TriggerTaskId == null).Select(s => s.Id).ToList();
            foreach (var oldTaskEntityId in oldTaskEntityIds)
            {
                await _taskService.CopyAsync(oldTaskEntityId, newScenarioTemplateEntity.Id, "scenarioTemplate", ct);
            }

            await transaction.CommitAsync(ct);
            var newScenarioTemplate = await GetAsync(newScenarioTemplateEntity.Id, ct);

            return newScenarioTemplate;
        }

        public async STT.Task<ViewModels.ScenarioTemplate> UpdateAsync(Guid id, ViewModels.ScenarioTemplateForm scenarioTemplateForm, CancellationToken ct)
        {
            var scenarioTemplateToUpdate = await _context.ScenarioTemplates.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (scenarioTemplateToUpdate == null)
                throw new EntityNotFoundException<SAVM.ScenarioTemplate>();

            _mapper.Map(scenarioTemplateForm, scenarioTemplateToUpdate);
            scenarioTemplateToUpdate.DateModified = DateTime.UtcNow;
            scenarioTemplateToUpdate.ModifiedBy = _user.GetId();

            _context.ScenarioTemplates.Update(scenarioTemplateToUpdate);
            await _context.SaveChangesAsync(ct);

            var scenarioTemplate = await GetAsync(scenarioTemplateToUpdate.Id, ct);

            return scenarioTemplate;
        }

        public async STT.Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            var scenarioTemplateToDelete = await _context.ScenarioTemplates.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (scenarioTemplateToDelete == null)
                throw new EntityNotFoundException<SAVM.ScenarioTemplate>();

            _context.ScenarioTemplates.Remove(scenarioTemplateToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        private async STT.Task<Guid?> CopyVmCredentials(Guid? oldDefaultVmCredentialId, Guid? scenarioTemplateId, Guid? scenarioId, List<VmCredentialEntity> oldVmCredentials, CancellationToken ct)
        {
            Guid? newDefaultVmCredentialId = null;
            foreach (var oldVmCredentialEntity in oldVmCredentials)
            {
                var newVmCredentialEntity = _mapper.Map<VmCredentialEntity>(oldVmCredentialEntity);
                newVmCredentialEntity.ScenarioTemplate = null;
                newVmCredentialEntity.Scenario = null;
                newVmCredentialEntity.ScenarioTemplateId = scenarioTemplateId;
                newVmCredentialEntity.ScenarioId = scenarioId;
                newVmCredentialEntity.CreatedBy = _user.GetId();
                _context.VmCredentials.Add(newVmCredentialEntity);
                // set the new default VM Credential
                if (oldDefaultVmCredentialId == oldVmCredentialEntity.Id)
                {
                    await _context.SaveChangesAsync(ct);
                    newDefaultVmCredentialId = newVmCredentialEntity.Id;
                }
            }
            await _context.SaveChangesAsync(ct);

            return newDefaultVmCredentialId;
        }

    }
}
