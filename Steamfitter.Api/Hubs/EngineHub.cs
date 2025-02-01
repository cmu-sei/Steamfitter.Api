// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using STT = System.Threading.Tasks;
using Steamfitter.Api.Data;
using System.Security.Claims;
using System.Security.Principal;
using Steamfitter.Api.Infrastructure.Authorization;

namespace Steamfitter.Api.Hubs
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class EngineHub : Hub
    {
        private readonly SteamfitterContext _db;
        private readonly ISteamfitterAuthorizationService _authorizationService;
        private readonly ClaimsPrincipal _user;

        public EngineHub(
            SteamfitterContext db,
            ISteamfitterAuthorizationService authorizationService,
            IPrincipal user)
        {
            _db = db;
            _authorizationService = authorizationService;
            _user = user as ClaimsPrincipal;
        }

        public async STT.Task JoinSystem()
        {
            // TODO: add the correct authorization
            // if ((await _authorizationService.AuthorizeAsync(_user, null, new ContentDeveloperRequirement())).Succeeded)
            // {
            await Groups.AddToGroupAsync(Context.ConnectionId, EngineGroups.SystemGroup);
            // }
        }

        public async STT.Task LeaveSystem()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, EngineGroups.SystemGroup);
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

    public static class EngineGroups
    {
        public const string SystemGroup = "System";

        public static string GetSystemGroup(Guid groupId)
        {
            return $"{groupId}-{SystemGroup}";
        }
    }
}
