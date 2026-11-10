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
    public class ScenarioMembershipCreatedSignalRHandler : INotificationHandler<EntityCreated<ScenarioMembershipEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;

        public ScenarioMembershipCreatedSignalRHandler(
            IHubContext<EngineHub> engineHub)
        {
            _engineHub = engineHub;
        }

        public async Task Handle(EntityCreated<ScenarioMembershipEntity> notification, CancellationToken cancellationToken)
        {
            var scenarioMembership = notification.Entity.ToScenarioMembership();
            await _engineHub.Clients
                .Groups(EngineHub.SCENARIO_GROUP)
                .SendAsync(EngineMethods.ScenarioMembershipCreated, scenarioMembership);
        }
    }

    public class ScenarioMembershipDeletedSignalRHandler : INotificationHandler<EntityDeleted<ScenarioMembershipEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;

        public ScenarioMembershipDeletedSignalRHandler(
            IHubContext<EngineHub> engineHub)
        {
            _engineHub = engineHub;
        }

        public async Task Handle(EntityDeleted<ScenarioMembershipEntity> notification, CancellationToken cancellationToken)
        {
            await _engineHub.Clients
                .Groups(EngineHub.SCENARIO_GROUP)
                .SendAsync(EngineMethods.ScenarioMembershipDeleted, notification.Entity.Id);
        }
    }

    public class ScenarioMembershipUpdatedSignalRHandler : INotificationHandler<EntityUpdated<ScenarioMembershipEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;

        public ScenarioMembershipUpdatedSignalRHandler(
            IHubContext<EngineHub> engineHub)
        {
            _engineHub = engineHub;
        }

        public async Task Handle(EntityUpdated<ScenarioMembershipEntity> notification, CancellationToken cancellationToken)
        {
            var scenarioMembership = notification.Entity.ToScenarioMembership();
            await _engineHub.Clients
                .Groups(EngineHub.SCENARIO_GROUP)
                .SendAsync(EngineMethods.ScenarioMembershipUpdated, scenarioMembership);
        }
    }
}
