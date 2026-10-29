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
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Infrastructure.Extensions;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Infrastructure.Exceptions;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Services
{
    public interface IScenarioService
    {
        STT.Task<IEnumerable<ViewModels.Scenario>> GetAsync(CancellationToken ct);
        STT.Task<IEnumerable<ViewModels.Scenario>> GetByUserAsync(CancellationToken ct);
        STT.Task<IEnumerable<ViewModels.Scenario>> GetByViewIdAsync(Guid viewId, CancellationToken ct);
        STT.Task<ViewModels.Scenario> GetAsync(Guid Id, CancellationToken ct);
        STT.Task<ViewModels.Scenario> GetMyScenarioAsync(CancellationToken ct);
        STT.Task<ViewModels.Scenario> CreateAsync(ViewModels.ScenarioCreate scenarioForm, CancellationToken ct);
        STT.Task<ViewModels.Scenario> CreateFromScenarioTemplateAsync(Guid scenarioTemplateId, SAVM.ScenarioCloneOptions options, CancellationToken ct);
        STT.Task<ViewModels.Scenario> CreateFromScenarioAsync(Guid scenarioId, CancellationToken ct);
        STT.Task<ViewModels.Scenario> UpdateAsync(Guid Id, ViewModels.ScenarioEdit scenarioForm, CancellationToken ct);
        STT.Task<bool> DeleteAsync(Guid Id, CancellationToken ct);
        STT.Task<ViewModels.Scenario> StartAsync(Guid Id, CancellationToken ct);
        STT.Task<ViewModels.Scenario> PauseAsync(Guid Id, CancellationToken ct);
        STT.Task<ViewModels.Scenario> ContinueAsync(Guid Id, CancellationToken ct);
        STT.Task<ViewModels.Scenario> EndAsync(Guid Id, CancellationToken ct);
    }

    public class ScenarioService : IScenarioService
    {
        private readonly SteamfitterContext _context;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;
        private readonly ITaskService _taskService;
        private readonly IXApiService _xApiService;
        private readonly IPlayerService _playerService;
        private readonly ISteamfitterAuthorizationService _authorizationService;
        private readonly IUserClaimsService _userClaimsService;

        public ScenarioService(SteamfitterContext context,
                                IPrincipal user,
                                IMapper mapper,
                                ITaskService taskService,
                                IXApiService xApiService,
                                IPlayerService playerService,
                                ISteamfitterAuthorizationService authorizationService,
                                IUserClaimsService userClaimsService)
        {
            _playerService = playerService;
            _userClaimsService = userClaimsService;
            _authorizationService = authorizationService;
            _context = context;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
            _taskService = taskService;
            _xApiService = xApiService;
        }

        public async STT.Task<IEnumerable<ViewModels.Scenario>> GetAsync(CancellationToken ct)
        {
            var items = await _context.Scenarios
                .Include(st => st.VmCredentials)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<SAVM.Scenario>>(items);
        }

        public async STT.Task<IEnumerable<ViewModels.Scenario>> GetByUserAsync(CancellationToken ct)
        {
            // the Scenarios the caller may view; membership alone is not enough
            var scenarioIds = _authorizationService.GetAuthorizedScenarioIds(ScenarioPermission.ViewScenario);
            var items = await _context.Scenarios
                .Where(s => scenarioIds.Contains(s.Id))
                .Include(s => s.VmCredentials)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<SAVM.Scenario>>(items);
        }

        public async STT.Task<IEnumerable<ViewModels.Scenario>> GetByViewIdAsync(Guid viewId, CancellationToken ct)
        {
            var items = await _context.Scenarios
                .Include(st => st.VmCredentials)
                .Where(x => x.ViewId == viewId)
                .ToListAsync(ct);

            return _mapper.Map<IEnumerable<SAVM.Scenario>>(items);
        }

        public async STT.Task<ViewModels.Scenario> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.Scenarios.Include(st => st.VmCredentials)
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            return _mapper.Map<SAVM.Scenario>(item);
        }

        /// <summary>
        /// The caller's personal Task Builder Scenario, whose Id is their User Id, made on first use. A Scenario with
        /// that Id is theirs only if they created it: one made by another caller before they first came is refused,
        /// rather than handed to them with whatever View, Tasks and credentials it was given.
        /// </summary>
        public async STT.Task<ViewModels.Scenario> GetMyScenarioAsync(CancellationToken ct)
        {
            var id = _user.GetId();
            var item = await _context.Scenarios.Include(st => st.VmCredentials)
                .SingleOrDefaultAsync(o => o.Id == id, ct);
            if (item != null && item.CreatedBy != id)
                throw new ForbiddenException("The Id of your personal Task Builder Scenario is taken by a Scenario another User created.");

            if (item == null)
            {
                var createdDate = DateTime.UtcNow;
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

            // the owner manages it, as a creator manages any Scenario; one made before that is given the membership now
            if (!await _context.ScenarioMemberships.AnyAsync(x => x.ScenarioId == id && x.UserId == id, ct))
            {
                _context.ScenarioMemberships.Add(new ScenarioMembershipEntity()
                {
                    UserId = id,
                    ScenarioId = id,
                    RoleId = ScenarioRoleDefaults.ScenarioCreatorRoleId
                });
                await _context.SaveChangesAsync(ct);
                await _userClaimsService.RefreshCurrentClaimsAsync();
            }

            return _mapper.Map<SAVM.Scenario>(item);
        }

        public async STT.Task<ViewModels.Scenario> CreateAsync(ViewModels.ScenarioCreate scenarioForm, CancellationToken ct)
        {
            await EnsureIdIsFreeAsync(scenarioForm.Id, ct);
            await EnsureCanBindViewAsync(scenarioForm.ViewId, ct);
            var scenarioEntity = _mapper.Map<ScenarioEntity>(scenarioForm);
            scenarioEntity.Id = scenarioForm.Id.HasValue ? scenarioForm.Id.Value : Guid.NewGuid();
            // a new scenario has no credentials for a default to name
            scenarioEntity.DefaultVmCredentialId = null;
            scenarioEntity.DateCreated = DateTime.UtcNow;
            scenarioEntity.CreatedBy = _user.GetId();
            scenarioEntity.StartDate = scenarioEntity.StartDate.ToUniversalTime();
            scenarioEntity.EndDate = scenarioEntity.EndDate.ToUniversalTime();
            scenarioEntity.Status = ScenarioStatus.ready;
            _context.Scenarios.Add(scenarioEntity);
            await _context.SaveChangesAsync(ct);
            var createOwnerMembership = new ScenarioMembershipEntity() {
                UserId = _user.GetId(),
                ScenarioId = scenarioEntity.Id,
                RoleId = ScenarioRoleDefaults.ScenarioCreatorRoleId
            };
            await _context.ScenarioMemberships.AddAsync(createOwnerMembership, ct);
            await _context.SaveChangesAsync(ct);
            await _userClaimsService.RefreshCurrentClaimsAsync();
            var scenario = await GetAsync(scenarioEntity.Id, ct);

            return scenario;
        }

        public async STT.Task<ViewModels.Scenario> CreateFromScenarioTemplateAsync(Guid scenarioTemplateId, SAVM.ScenarioCloneOptions options, CancellationToken ct)
        {
            await EnsureCanBindViewAsync(options?.ViewId, ct);
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            var scenarioTemplateEntity = _context.ScenarioTemplates.Find(scenarioTemplateId);
            if (scenarioTemplateEntity == null)
                throw new EntityNotFoundException<SAVM.ScenarioTemplate>($"ScenarioTemplate {scenarioTemplateId} was not found.");

            var scenarioEntity = new ScenarioEntity()
            {
                CreatedBy = _user.GetId(),
                Name = $"{scenarioTemplateEntity.Name} - {_user.Claims.FirstOrDefault(c => c.Type == "name").Value}",
                Description = scenarioTemplateEntity.Description,
                OnDemand = true,
                ScenarioTemplateId = scenarioTemplateId,
                Status = ScenarioStatus.ready
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

            if (options != null)
            {
                if (!string.IsNullOrEmpty(options.NameSuffix))
                {
                    scenarioEntity.Name = scenarioTemplateEntity.Name + options.NameSuffix;
                }

                if (options.ViewId.HasValue)
                {
                    scenarioEntity.ViewId = options.ViewId;
                }

                await _context.SaveChangesAsync(ct);
            }
            var createOwnerMembership = new ScenarioMembershipEntity() {
                UserId = _user.GetId(),
                ScenarioId = scenarioEntity.Id,
                RoleId = ScenarioRoleDefaults.ScenarioCreatorRoleId
            };
            await _context.ScenarioMemberships.AddAsync(createOwnerMembership, ct);
            await _context.SaveChangesAsync(ct);

            if (options != null)
            {
                await AddScenarioMembersAsync(scenarioEntity.Id, options, ct);
            }

            await transaction.CommitAsync(ct);
            await _userClaimsService.RefreshCurrentClaimsAsync();
            var scenario = _mapper.Map<SAVM.Scenario>(scenarioEntity);

            return scenario;
        }

        public async STT.Task<ViewModels.Scenario> CreateFromScenarioAsync(Guid oldScenarioId, CancellationToken ct)
        {
            var oldScenarioEntity = _context.Scenarios.Find(oldScenarioId);
            if (oldScenarioEntity == null)
                throw new EntityNotFoundException<SAVM.Scenario>($"Scenario {oldScenarioId} was not found.");
            // the copy runs its Tasks on the original's View, so viewing the original is not enough. Player is
            // asked before the transaction opens, so that no connection is held open while it answers.
            await EnsureCanBindViewAsync(oldScenarioEntity.ViewId, ct);

            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            var newScenarioEntity = _mapper.Map<ScenarioEntity>(oldScenarioEntity);
            newScenarioEntity.CreatedBy = _user.GetId();
            newScenarioEntity.Name = $"{oldScenarioEntity.Name} - {_user.Claims.FirstOrDefault(c => c.Type == "name").Value}";
            newScenarioEntity.OnDemand = true;
            newScenarioEntity.Status = ScenarioStatus.ready;

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
            var createOwnerMembership = new ScenarioMembershipEntity() {
                UserId = _user.GetId(),
                ScenarioId = newScenarioEntity.Id,
                RoleId = ScenarioRoleDefaults.ScenarioCreatorRoleId
            };
            await _context.ScenarioMemberships.AddAsync(createOwnerMembership, ct);
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            await _userClaimsService.RefreshCurrentClaimsAsync();
            var scenario = await GetAsync(newScenarioEntity.Id, ct);

            return scenario;
        }

        public async STT.Task<SAVM.Scenario> UpdateAsync(Guid id, ViewModels.ScenarioEdit scenarioForm, CancellationToken ct)
        {
            var scenarioToUpdate = await _context.Scenarios.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (scenarioToUpdate == null)
                throw new EntityNotFoundException<SAVM.Scenario>();

            if (scenarioForm.ViewId != scenarioToUpdate.ViewId)
                await EnsureCanBindViewAsync(scenarioForm.ViewId, ct);

            await ScopeValidation.EnsureVmCredentialIsInParentAsync(scenarioForm.DefaultVmCredentialId, id, null, _context, ct);
            _mapper.Map(scenarioForm, scenarioToUpdate);
            scenarioToUpdate.DateModified = DateTime.UtcNow;
            scenarioToUpdate.ModifiedBy = _user.GetId();

            await _context.SaveChangesAsync(ct);

            var updatedScenario = _mapper.Map<SAVM.Scenario>(scenarioToUpdate);
            return updatedScenario;
        }

        public async STT.Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            var scenarioToDelete = await _context.Scenarios.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (scenarioToDelete == null)
                throw new EntityNotFoundException<SAVM.Scenario>();

            // the children in other scopes are detached and the scenario deleted together, or neither is
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            await _taskService.DetachChildrenInOtherScopesAsync(id, null, ct);
            _context.Scenarios.Remove(scenarioToDelete);
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return true;
        }

        public async STT.Task<ViewModels.Scenario> StartAsync(Guid id, CancellationToken ct)
        {
            var scenario = await _context.Scenarios
                .SingleAsync(o => o.Id == id, ct);
            var dateTimeStart = DateTime.UtcNow;
            scenario.DateModified = dateTimeStart;
            scenario.ModifiedBy = _user.GetId();
            scenario.StartDate = dateTimeStart;
            scenario.Status = ScenarioStatus.active;

            await _context.SaveChangesAsync(ct);

            // Log xAPI scenario started statement
            await _xApiService.ScenarioStartedAsync(scenario.Id, ct);

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
            var scenario = await _context.Scenarios
                .SingleAsync(o => o.Id == id, ct);
            var endDateTime = DateTime.UtcNow;
            scenario.DateModified = endDateTime;
            scenario.ModifiedBy = _user.GetId();
            scenario.Status = ScenarioStatus.ended;
            scenario.EndDate = endDateTime;

            await _context.SaveChangesAsync(ct);

            // Log xAPI scenario ended statement
            await _xApiService.ScenarioEndedAsync(scenario.Id, ct);

            var updatedScenario = _mapper.Map<SAVM.Scenario>(scenario);

            return updatedScenario;
        }

        /// <summary>
        /// A caller may choose a new Scenario's Id, as Blueprint does. It must not be another Scenario's, nor a
        /// User's Id, which is the Id of that User's personal Task Builder Scenario, nor a Scenario Template's,
        /// which names the same hub group as the template's events. The Id of a User not yet seen cannot be
        /// told from a fresh one; <see cref="GetMyScenarioAsync"/> refuses such a Scenario as anyone's own.
        /// </summary>
        private async STT.Task EnsureIdIsFreeAsync(Guid? id, CancellationToken ct)
        {
            if (id == null)
                return;

            if (await _context.Scenarios.AnyAsync(x => x.Id == id, ct)
                || await _context.Users.AnyAsync(x => x.Id == id, ct)
                || await _context.ScenarioTemplates.AnyAsync(x => x.Id == id, ct))
                throw new ForbiddenException($"The Id {id} cannot be used for a new Scenario.");
        }

        /// <summary>
        /// A Scenario's Tasks run on every VM of its View with the service account, so binding one needs
        /// ManageView on that View in Player. A caller holding ManageScenarios is trusted to bind any View.
        /// </summary>
        private async STT.Task EnsureCanBindViewAsync(Guid? viewId, CancellationToken ct)
        {
            var trustedWithAnyView = await _authorizationService.AuthorizeAsync([SystemPermission.ManageScenarios], ct);
            if (viewId == null || trustedWithAnyView)
                return;

            if (!await _playerService.CanManageViewAsync(viewId.Value, ct))
                throw new ForbiddenException($"Binding a Scenario to View {viewId} needs ManageView on it in Player.");
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

        /// <summary>
        /// Gives each User named in the clone options a Member Membership on the new Scenario,
        /// creating the Steamfitter User record first if the User has never been seen before.
        /// The calling account keeps the Manager Membership it was given as the creator.
        /// </summary>
        private async STT.Task AddScenarioMembersAsync(Guid scenarioId, SAVM.ScenarioCloneOptions options, CancellationToken ct)
        {
            // Users carries a name to create missing Users with, UserIds does not, so let Users win
            var requestedNamesByUserId = new Dictionary<Guid, string>();

            if (options.UserIds != null)
            {
                foreach (var userId in options.UserIds.Where(x => x != Guid.Empty))
                {
                    requestedNamesByUserId[userId] = null;
                }
            }

            if (options.Users != null)
            {
                foreach (var user in options.Users.Where(x => x != null && x.Id != Guid.Empty))
                {
                    requestedNamesByUserId[user.Id] = string.IsNullOrWhiteSpace(user.Name) ? null : user.Name.Trim();
                }
            }

            // the caller is already a member as the creator of the Scenario
            requestedNamesByUserId.Remove(_user.GetId());

            if (!requestedNamesByUserId.Any())
                return;

            var userIds = requestedNamesByUserId.Keys.ToList();
            var existingUsers = await _context.Users
                .Where(x => userIds.Contains(x.Id))
                .ToListAsync(ct);

            foreach (var requestedUser in requestedNamesByUserId)
            {
                var userEntity = existingUsers.SingleOrDefault(x => x.Id == requestedUser.Key);

                // the name is used only to create a missing User; renaming one takes ManageUsers
                if (userEntity == null)
                {
                    _context.Users.Add(new UserEntity()
                    {
                        Id = requestedUser.Key,
                        Name = requestedUser.Value ?? requestedUser.Key.ToString(),
                        CreatedBy = _user.GetId()
                    });
                }
            }

            var alreadyMemberUserIds = await _context.ScenarioMemberships
                .Where(x => x.ScenarioId == scenarioId && x.UserId.HasValue && userIds.Contains(x.UserId.Value))
                .Select(x => x.UserId.Value)
                .ToListAsync(ct);

            foreach (var userId in userIds.Where(x => !alreadyMemberUserIds.Contains(x)))
            {
                _context.ScenarioMemberships.Add(new ScenarioMembershipEntity()
                {
                    UserId = userId,
                    ScenarioId = scenarioId,
                    RoleId = ScenarioRoleDefaults.ScenarioMemberRoleId
                });
            }

            await _context.SaveChangesAsync(ct);
        }

    }
}
