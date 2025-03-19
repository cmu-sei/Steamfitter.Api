// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Hubs;

namespace Steamfitter.Api.Events.EventHandlers.SignalR
{
    public class GroupMembershipCreatedSignalRHandler : INotificationHandler<EntityCreated<GroupMembershipEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;
        private readonly IMapper _mapper;

        public GroupMembershipCreatedSignalRHandler(
            IHubContext<EngineHub> engineHub,
            IMapper mapper)
        {
            _engineHub = engineHub;
            _mapper = mapper;
        }

        public async Task Handle(EntityCreated<GroupMembershipEntity> notification, CancellationToken cancellationToken)
        {
            var groupMembership = _mapper.Map<ViewModels.GroupMembership>(notification.Entity);
            await _engineHub.Clients
                .Groups(
                    EngineGroups.GetSystemGroup(notification.Entity.Id),
                    EngineGroups.SystemGroup)
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
                .Groups(
                    notification.Entity.Id.ToString(),
                    EngineGroups.GetSystemGroup(notification.Entity.Id),
                    EngineGroups.SystemGroup
                    )
                .SendAsync(EngineMethods.GroupMembershipDeleted, notification.Entity.Id);
        }
    }

    public class GroupMembershipUpdatedSignalRHandler : INotificationHandler<EntityUpdated<GroupMembershipEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;
        private readonly IMapper _mapper;

        public GroupMembershipUpdatedSignalRHandler(
            IHubContext<EngineHub> engineHub,
            IMapper mapper)
        {
            _engineHub = engineHub;
            _mapper = mapper;
        }

        public async Task Handle(EntityUpdated<GroupMembershipEntity> notification, CancellationToken cancellationToken)
        {
            var groupMembership = _mapper.Map<ViewModels.GroupMembership>(notification.Entity);
            await _engineHub.Clients
                .Groups(
                    EngineGroups.GetSystemGroup(notification.Entity.Id),
                    EngineGroups.SystemGroup)
                .SendAsync(EngineMethods.GroupMembershipUpdated, groupMembership);
        }
    }
}
