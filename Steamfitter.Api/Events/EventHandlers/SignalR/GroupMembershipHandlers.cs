// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading;
using Steamfitter.Api.Infrastructure.Mappings;
using System.Threading.Tasks;
using MediatR;
using Crucible.Common.EntityEvents.Events;
using Microsoft.AspNetCore.SignalR;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Hubs;

namespace Steamfitter.Api.Events.EventHandlers.SignalR
{
    public class GroupMembershipCreatedSignalRHandler : INotificationHandler<EntityCreated<GroupMembershipEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;

        public GroupMembershipCreatedSignalRHandler(
            IHubContext<EngineHub> engineHub)
        {
            _engineHub = engineHub;
        }

        public async Task Handle(EntityCreated<GroupMembershipEntity> notification, CancellationToken cancellationToken)
        {
            var groupMembership = notification.Entity.ToGroupMembership();
            await _engineHub.Clients
                .Groups(EngineHub.GROUP_GROUP)
                .SendAsync(EngineMethods.GroupMembershipCreated, groupMembership);
        }
    }

    public class GroupMembershipDeletedSignalRHandler : INotificationHandler<EntityDeleted<GroupMembershipEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;

        public GroupMembershipDeletedSignalRHandler(
            IHubContext<EngineHub> engineHub)
        {
            _engineHub = engineHub;
        }

        public async Task Handle(EntityDeleted<GroupMembershipEntity> notification, CancellationToken cancellationToken)
        {
            await _engineHub.Clients
                .Groups(EngineHub.GROUP_GROUP)
                .SendAsync(EngineMethods.GroupMembershipDeleted, notification.Entity.Id);
        }
    }

    public class GroupMembershipUpdatedSignalRHandler : INotificationHandler<EntityUpdated<GroupMembershipEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;

        public GroupMembershipUpdatedSignalRHandler(
            IHubContext<EngineHub> engineHub)
        {
            _engineHub = engineHub;
        }

        public async Task Handle(EntityUpdated<GroupMembershipEntity> notification, CancellationToken cancellationToken)
        {
            var groupMembership = notification.Entity.ToGroupMembership();
            await _engineHub.Clients
                .Groups(EngineHub.GROUP_GROUP)
                .SendAsync(EngineMethods.GroupMembershipUpdated, groupMembership);
        }
    }
}
