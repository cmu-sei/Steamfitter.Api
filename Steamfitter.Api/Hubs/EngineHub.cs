// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using STT = System.Threading.Tasks;
using Steamfitter.Api.Data;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Infrastructure.Extensions;
using Steamfitter.Api.ViewModels;
using System;

namespace Steamfitter.Api.Hubs
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class EngineHub : Hub
    {
        private readonly SteamfitterContext _db;
        private readonly ISteamfitterAuthorizationService _authorizationService;
        private readonly ClaimsPrincipal _user;
        public const string SCENARIO_GROUP = "AdminScenarioGroup";
        public const string SCENARIO_TEMPLATE_GROUP = "AdminScenarioTemplateGroup";
        public const string GROUP_GROUP = "AdminGroupGroup";
        public const string ROLE_GROUP = "AdminRoleGroup";
        public const string USER_GROUP = "AdminUserGroup";

        public EngineHub(
            SteamfitterContext db,
            ISteamfitterAuthorizationService authorizationService,
            IPrincipal user)
        {
            _db = db;
            _authorizationService = authorizationService;
            _user = user as ClaimsPrincipal;
        }

        public async STT.Task JoinScenario(Guid scenarioId)
        {
            if (await _authorizationService.AuthorizeAsync<Scenario>(scenarioId, [SystemPermission.ViewScenarios], [ScenarioPermission.ViewScenario], new CancellationToken()))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, scenarioId.ToString());
            }
        }

        public async STT.Task LeaveScenario(Guid scenarioId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, scenarioId.ToString());
        }

        public async STT.Task JoinSystem()
        {
            var userId = _user.GetId();
            var ct = new CancellationToken();
            if (await _authorizationService.AuthorizeAsync([SystemPermission.ViewScenarios], ct))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, SCENARIO_GROUP);
            }
            else
            {
                var scenarioIds = await _db.ScenarioMemberships
                    .Where(x => x.UserId == userId)
                    .Select(x => x.ScenarioId)
                    .ToListAsync(ct);
                foreach (var item in scenarioIds)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, item.ToString());
                }
            }
            if (await _authorizationService.AuthorizeAsync([SystemPermission.ViewScenarioTemplates], ct))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, SCENARIO_TEMPLATE_GROUP);
            }
            else
            {
                var scenarioTemplateIds = await _db.ScenarioTemplateMemberships
                    .Where(x => x.UserId == userId)
                    .Select(x => x.ScenarioTemplateId)
                    .ToListAsync(ct);
                foreach (var item in scenarioTemplateIds)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, item.ToString());
                }
            }
            if (await _authorizationService.AuthorizeAsync([SystemPermission.ViewGroups], ct))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GROUP_GROUP);
            }
            if (await _authorizationService.AuthorizeAsync([SystemPermission.ViewRoles], ct))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, ROLE_GROUP);
            }
            if (await _authorizationService.AuthorizeAsync([SystemPermission.ViewUsers], ct))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, USER_GROUP);
            }
        }

        public async STT.Task LeaveSystem()
        {
            var userId = _user.GetId();
            var ct = new CancellationToken();
            if (await _authorizationService.AuthorizeAsync([SystemPermission.ViewScenarios], ct))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, SCENARIO_GROUP);
            }
            else
            {
                var scenarioIds = await _db.ScenarioMemberships
                    .Where(x => x.UserId == userId)
                    .Select(x => x.ScenarioId)
                    .ToListAsync(ct);
                foreach (var item in scenarioIds)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, item.ToString());
                }
            }
            if (await _authorizationService.AuthorizeAsync([SystemPermission.ViewScenarioTemplates], ct))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, SCENARIO_TEMPLATE_GROUP);
            }
            else
            {
                var scenarioTemplateIds = await _db.ScenarioTemplateMemberships
                    .Where(x => x.UserId == userId)
                    .Select(x => x.ScenarioTemplateId)
                    .ToListAsync(ct);
                foreach (var item in scenarioTemplateIds)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, item.ToString());
                }
            }
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GROUP_GROUP);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ROLE_GROUP);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, USER_GROUP);
        }
    }

    public static class EngineMethods
    {
        public const string ScenarioTemplateCreated = "ScenarioTemplateCreated";
        public const string ScenarioTemplateUpdated = "ScenarioTemplateUpdated";
        public const string ScenarioTemplateDeleted = "ScenarioTemplateDeleted";
        public const string ScenarioCreated = "ScenarioCreated";
        public const string ScenarioUpdated = "ScenarioUpdated";
        public const string ScenarioDeleted = "ScenarioDeleted";
        public const string TaskCreated = "TaskCreated";
        public const string TaskUpdated = "TaskUpdated";
        public const string TaskDeleted = "TaskDeleted";
        public const string ResultCreated = "ResultCreated";
        public const string ResultUpdated = "ResultUpdated";
        public const string ResultsUpdated = "ResultsUpdated";
        public const string ResultDeleted = "ResultDeleted";
        public const string GroupMembershipCreated = "GroupMembershipCreated";
        public const string GroupMembershipUpdated = "GroupMembershipUpdated";
        public const string GroupMembershipDeleted = "GroupMembershipDeleted";
        public const string ScenarioTemplateMembershipCreated = "ScenarioTemplateMembershipCreated";
        public const string ScenarioTemplateMembershipUpdated = "ScenarioTemplateMembershipUpdated";
        public const string ScenarioTemplateMembershipDeleted = "ScenarioTemplateMembershipDeleted";
        public const string ScenarioMembershipCreated = "ScenarioMembershipCreated";
        public const string ScenarioMembershipUpdated = "ScenarioMembershipUpdated";
        public const string ScenarioMembershipDeleted = "ScenarioMembershipDeleted";
    }
}
