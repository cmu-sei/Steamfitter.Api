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
    public class ScenarioTemplateMembershipCreatedSignalRHandler : INotificationHandler<EntityCreated<ScenarioTemplateMembershipEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;

        public ScenarioTemplateMembershipCreatedSignalRHandler(
            IHubContext<EngineHub> engineHub)
        {
            _engineHub = engineHub;
        }

        public async Task Handle(EntityCreated<ScenarioTemplateMembershipEntity> notification, CancellationToken cancellationToken)
        {
            var scenarioTemplateMembership = notification.Entity.ToScenarioTemplateMembership();
            await _engineHub.Clients
                .Groups(EngineHub.SCENARIO_TEMPLATE_GROUP)
                .SendAsync(EngineMethods.ScenarioTemplateMembershipCreated, scenarioTemplateMembership);
        }
    }

    public class ScenarioTemplateMembershipDeletedSignalRHandler : INotificationHandler<EntityDeleted<ScenarioTemplateMembershipEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;

        public ScenarioTemplateMembershipDeletedSignalRHandler(
            IHubContext<EngineHub> engineHub)
        {
            _engineHub = engineHub;
        }

        public async Task Handle(EntityDeleted<ScenarioTemplateMembershipEntity> notification, CancellationToken cancellationToken)
        {
            await _engineHub.Clients
                .Groups(EngineHub.SCENARIO_TEMPLATE_GROUP)
                .SendAsync(EngineMethods.ScenarioTemplateMembershipDeleted, notification.Entity.Id);
        }
    }

    public class ScenarioTemplateMembershipUpdatedSignalRHandler : INotificationHandler<EntityUpdated<ScenarioTemplateMembershipEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;

        public ScenarioTemplateMembershipUpdatedSignalRHandler(
            IHubContext<EngineHub> engineHub)
        {
            _engineHub = engineHub;
        }

        public async Task Handle(EntityUpdated<ScenarioTemplateMembershipEntity> notification, CancellationToken cancellationToken)
        {
            var scenarioTemplateMembership = notification.Entity.ToScenarioTemplateMembership();
            await _engineHub.Clients
                .Groups(EngineHub.SCENARIO_TEMPLATE_GROUP)
                .SendAsync(EngineMethods.ScenarioTemplateMembershipUpdated, scenarioTemplateMembership);
        }
    }
}
