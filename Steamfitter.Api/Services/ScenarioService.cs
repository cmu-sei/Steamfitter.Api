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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Hubs;
using Steamfitter.Api.Infrastructure.Extensions;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Infrastructure.Exceptions;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Services
{
    public interface IScenarioService
    {
        STT.Task<IEnumerable<ViewModels.Scenario>> GetAsync(CancellationToken ct);
        STT.Task<IEnumerable<ViewModels.Scenario>> GetByViewIdAsync(Guid viewId, CancellationToken ct);
        STT.Task<ViewModels.Scenario> GetAsync(Guid Id, CancellationToken ct);
        STT.Task<ViewModels.Scenario> GetMineAsync(CancellationToken ct);
        STT.Task<ViewModels.Scenario> CreateAsync(ViewModels.ScenarioForm scenarioForm, CancellationToken ct);
        STT.Task<ViewModels.Scenario> CreateFromScenarioTemplateAsync(Guid scenarioTemplateId, CancellationToken ct);
        STT.Task<ViewModels.Scenario> CreateFromScenarioAsync(Guid scenarioId, CancellationToken ct);
        STT.Task<ViewModels.Scenario> UpdateAsync(Guid Id, ViewModels.ScenarioForm scenarioForm, CancellationToken ct);
        STT.Task<ViewModels.Scenario> AddUsersAsync(Guid Id, IEnumerable<Guid> userIds, CancellationToken ct);
        STT.Task<bool> DeleteAsync(Guid Id, CancellationToken ct);
        STT.Task<ViewModels.Scenario> StartAsync(Guid Id, CancellationToken ct);
        STT.Task<ViewModels.Scenario> PauseAsync(Guid Id, CancellationToken ct);
        STT.Task<ViewModels.Scenario> ContinueAsync(Guid Id, CancellationToken ct);
        STT.Task<ViewModels.Scenario> EndAsync(Guid Id, CancellationToken ct);
    }

    public class ScenarioService : IScenarioService
    {
        private readonly SteamfitterContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;
        private readonly ITaskService _taskService;
        private readonly IStackStormService _stackstormService;

        public ScenarioService(SteamfitterContext context,
                                IAuthorizationService authorizationService,
                                IPrincipal user,
                                IMapper mapper,
                                ITaskService taskService,
                                IStackStormService stackstormService)
        {
            _context = context;
            _authorizationService = authorizationService;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
            _taskService = taskService;
            _stackstormService = stackstormService;
        }

        public async STT.Task<IEnumerable<ViewModels.Scenario>> GetAsync(CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var items = await _context.Scenarios
                .Include(st => st.VmCredentials)
                .Include(x => x.Users)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<SAVM.Scenario>>(items);
        }

        public async STT.Task<IEnumerable<ViewModels.Scenario>> GetByViewIdAsync(Guid viewId, CancellationToken ct)
        {
            var items = await _context.Scenarios
                .Include(st => st.VmCredentials)
                .Include(x => x.Users)
                .Where(x => x.ViewId == viewId)
                .ToListAsync(ct);

            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
            {
                var allowedScenarios = new List<ViewModels.Scenario>();

                foreach (var scenario in items)
                {
                    if (scenario.Users.Any(x => x.UserId == _user.GetId()))
                    {
                        allowedScenarios.Add(
                            _mapper.Map<ViewModels.Scenario>(
                                _mapper.Map<ViewModels.ScenarioSummary>(scenario)));
                    }
                }

                return allowedScenarios;
            }

            return _mapper.Map<IEnumerable<SAVM.Scenario>>(items);
        }

        public async STT.Task<ViewModels.Scenario> GetAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var item = await _context.Scenarios.Include(st => st.VmCredentials)
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            return _mapper.Map<SAVM.Scenario>(item);
        }

        public async STT.Task<ViewModels.Scenario> GetMineAsync(CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var item = await _context.Scenarios.Include(st => st.VmCredentials)
                .SingleOrDefaultAsync(o => o.Id == _user.GetId(), ct);
            if (item == null)
            {
                var createdDate = DateTime.UtcNow;
                var id = _user.GetId();
                var name = _user.Claims.FirstOrDefault(c => c.Type == "name").Value;
                item = new ScenarioEntity()
                {
                    Id = id,
                    Name = $"{name} User Scenario",
                    Description = "Personal Task Builder Scenario",
                    StartDate = createdDate,
                    EndDate = createdDate.AddYears(100),
                    Status = ScenarioStatus.active,
                    OnDemand = false,
                    DateCreated = createdDate,
                    DateModified = createdDate,
                    CreatedBy = id,
                    ModifiedBy = id
                };
                _context.Scenarios.Add(item);
                await _context.SaveChangesAsync(ct);
            }

            return _mapper.Map<SAVM.Scenario>(item);
        }

        public async STT.Task<ViewModels.Scenario> CreateAsync(ViewModels.ScenarioForm scenarioForm, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var scenarioEntity = _mapper.Map<ScenarioEntity>(scenarioForm);
            scenarioEntity.DateCreated = DateTime.UtcNow;
            scenarioEntity.CreatedBy = _user.GetId();

            //TODO: add permissions
            // var ScenarioAdminPermission = await _context.Permissions
            //     .Where(p => p.Key == PlayerClaimTypes.ScenarioAdmin.ToString())
            //     .FirstOrDefaultAsync();

            // if (ScenarioAdminPermission == null)
            //     throw new EntityNotFoundException<Permission>($"{PlayerClaimTypes.ScenarioAdmin.ToString()} Permission not found.");

            _context.Scenarios.Add(scenarioEntity);
            await _context.SaveChangesAsync(ct);
            var scenario = await GetAsync(scenarioEntity.Id, ct);

            return scenario;
        }

        public async STT.Task<ViewModels.Scenario> CreateFromScenarioTemplateAsync(Guid scenarioTemplateId, CancellationToken ct)
        {
            var scenarioTemplateEntity = _context.ScenarioTemplates.Find(scenarioTemplateId);
            if (scenarioTemplateEntity == null)
                throw new EntityNotFoundException<SAVM.ScenarioTemplate>($"ScenarioTemplate {scenarioTemplateId} was not found.");

            var scenarioEntity = new ScenarioEntity()
            {
                CreatedBy = _user.GetId(),
                Name = $"{scenarioTemplateEntity.Name} - {_user.Claims.FirstOrDefault(c => c.Type == "name").Value}",
                Description = scenarioTemplateEntity.Description,
                OnDemand = true,
                ScenarioTemplateId = scenarioTemplateId
            };
            var durationHours = scenarioTemplateEntity.DurationHours != null ? (int)scenarioTemplateEntity.DurationHours : 720;
            scenarioEntity.EndDate = scenarioEntity.StartDate.AddHours(durationHours);
            _context.Scenarios.Add(scenarioEntity);
            await _context.SaveChangesAsync(ct);

            // copy all of the VmCredentials
            var oldVmCredentials = _context.VmCredentials.Where(vmc => vmc.ScenarioTemplateId == scenarioTemplateId).ToList();
            var newDefaultVmCredentialId = await CopyVmCredentials(scenarioTemplateEntity.DefaultVmCredentialId, null, scenarioEntity.Id, oldVmCredentials, ct);
            scenarioEntity.DefaultVmCredentialId = newDefaultVmCredentialId;
            await _context.SaveChangesAsync(ct);

            // copy all of the Tasks
            var oldTaskEntities = _context.Tasks.Where(dt => dt.ScenarioTemplateId == scenarioTemplateId && dt.TriggerTaskId == null).ToList();
            // copy the PARENT Tasks
            foreach (var oldTaskEntity in oldTaskEntities)
            {
                await _taskService.CopyAsync(oldTaskEntity.Id, scenarioEntity.Id, "scenario", ct);
            }

            var scenario = _mapper.Map<SAVM.Scenario>(scenarioEntity);

            return scenario;
        }

        public async STT.Task<ViewModels.Scenario> CreateFromScenarioAsync(Guid oldScenarioId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var oldScenarioEntity = _context.Scenarios.Find(oldScenarioId);
            if (oldScenarioEntity == null)
                throw new EntityNotFoundException<SAVM.Scenario>($"Scenario {oldScenarioId} was not found.");

            var newScenarioEntity = _mapper.Map<ScenarioEntity>(oldScenarioEntity);
            newScenarioEntity.CreatedBy = _user.GetId();
            newScenarioEntity.Name = $"{oldScenarioEntity.Name} - {_user.Claims.FirstOrDefault(c => c.Type == "name").Value}";
            newScenarioEntity.OnDemand = true;

            _context.Scenarios.Add(newScenarioEntity);
            await _context.SaveChangesAsync(ct);

            // copy all of the VmCredentials
            var oldVmCredentials = _context.VmCredentials.Where(vmc => vmc.ScenarioId == oldScenarioId).ToList();
            var newDefaultVmCredentialId = await CopyVmCredentials(oldScenarioEntity.DefaultVmCredentialId, null, newScenarioEntity.Id, oldVmCredentials, ct);
            newScenarioEntity.DefaultVmCredentialId = newDefaultVmCredentialId;
            await _context.SaveChangesAsync(ct);

            // copy all of the Tasks
            var oldTaskEntities = _context.Tasks.Where(dt => dt.ScenarioId == oldScenarioId && dt.TriggerTaskId == null).ToList();
            // copy the PARENT Tasks
            foreach (var oldTaskEntity in oldTaskEntities)
            {
                await _taskService.CopyAsync(oldTaskEntity.Id, newScenarioEntity.Id, "scenario", ct);
            }

            var scenario = await GetAsync(newScenarioEntity.Id, ct);

            return scenario;
        }

        public async STT.Task<SAVM.Scenario> UpdateAsync(Guid id, ViewModels.ScenarioForm scenarioForm, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var scenarioToUpdate = await _context.Scenarios.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (scenarioToUpdate == null)
                throw new EntityNotFoundException<SAVM.Scenario>();

            _mapper.Map(scenarioForm, scenarioToUpdate);
            scenarioToUpdate.DateModified = DateTime.UtcNow;
            scenarioToUpdate.ModifiedBy = _user.GetId();

            await _context.SaveChangesAsync(ct);

            var updatedScenario = _mapper.Map<SAVM.Scenario>(scenarioToUpdate);
            return updatedScenario;
        }

        public async STT.Task<ViewModels.Scenario> AddUsersAsync(Guid id, IEnumerable<Guid> userIds, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var scenarioToUpdate = await _context.Scenarios
                .Include(x => x.Users)
                .SingleOrDefaultAsync(v => v.Id == id, ct);

            if (scenarioToUpdate == null)
                throw new EntityNotFoundException<SAVM.Scenario>();

            var users = await _context.Users
                .Where(x => userIds.Contains(x.Id))
                .ToListAsync(ct);

            // Ensure users exist
            var usersToAdd = new List<UserEntity>();

            foreach (var userId in userIds)
            {
                if (!users.Any(x => x.Id == userId))
                {
                    usersToAdd.Add(new UserEntity
                    {
                        CreatedBy = _user.GetId(),
                        DateCreated = DateTime.UtcNow,
                        Name = "",
                        Id = userId
                    });
                }
            }

            await _context.Users.AddRangeAsync(usersToAdd);

            scenarioToUpdate.DateModified = DateTime.UtcNow;
            scenarioToUpdate.ModifiedBy = _user.GetId();
            scenarioToUpdate.AddUsers(userIds);

            await _context.SaveChangesAsync(ct);

            var updatedScenario = _mapper.Map<SAVM.Scenario>(scenarioToUpdate);

            return updatedScenario;
        }

        public async STT.Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var scenarioToDelete = await _context.Scenarios.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (scenarioToDelete == null)
                throw new EntityNotFoundException<SAVM.Scenario>();

            _context.Scenarios.Remove(scenarioToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async STT.Task<ViewModels.Scenario> StartAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var scenario = await _context.Scenarios
                .SingleAsync(o => o.Id == id, ct);
            var dateTimeStart = DateTime.UtcNow;
            scenario.DateModified = dateTimeStart;
            scenario.ModifiedBy = _user.GetId();
            scenario.StartDate = dateTimeStart;
            scenario.Status = ScenarioStatus.active;

            await _context.SaveChangesAsync(ct);
            // TODO:  create a better way to do this that doesn't require getting ALL of the VM's
            // We just need to grab all of the VM's from the scenario view
            await _stackstormService.GetStackstormVms();
            var tasks = await _taskService.GetByScenarioIdAsync(scenario.Id, ct);
            foreach (var task in tasks)
            {
                if (task.TriggerTaskId is null && task.TriggerCondition != TaskTrigger.Manual)
                {
                    await _taskService.ExecuteAsync(task.Id, ct);
                }
            }

            var updatedScenario = _mapper.Map<SAVM.Scenario>(scenario);

            return updatedScenario;
        }

        public async STT.Task<ViewModels.Scenario> PauseAsync(Guid id, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public async STT.Task<ViewModels.Scenario> ContinueAsync(Guid id, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public async STT.Task<ViewModels.Scenario> EndAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
                throw new ForbiddenException();

            var scenario = await _context.Scenarios
                .SingleAsync(o => o.Id == id, ct);
            var endDateTime = DateTime.UtcNow;
            scenario.DateModified = endDateTime;
            scenario.ModifiedBy = _user.GetId();
            scenario.Status = ScenarioStatus.ended;
            scenario.EndDate = endDateTime;

            await _context.SaveChangesAsync(ct);

            var updatedScenario = _mapper.Map<SAVM.Scenario>(scenario);

            return updatedScenario;
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
