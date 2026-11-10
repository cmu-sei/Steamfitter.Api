// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using Steamfitter.Api.Infrastructure.Mappings;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Crucible.Common.EntityEvents.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Hubs;

namespace Steamfitter.Api.Events.EventHandlers.SignalR
{
    public class ScenarioCreatedSignalRHandler : INotificationHandler<EntityCreated<ScenarioEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;

        public ScenarioCreatedSignalRHandler(
            IHubContext<EngineHub> engineHub)
        {
            _engineHub = engineHub;
        }

        public async Task Handle(EntityCreated<ScenarioEntity> notification, CancellationToken cancellationToken)
        {
            var scenarioSummary = notification.Entity.ToScenarioSummary().ToScenario();
            await _engineHub.Clients.Group(notification.Entity.Id.ToString())
                .SendAsync(EngineMethods.ScenarioCreated, scenarioSummary);

            var scenario = notification.Entity.ToScenario();
            await _engineHub.Clients
                .Groups(EngineHub.SCENARIO_GROUP)
                .SendAsync(EngineMethods.ScenarioCreated, scenario);
            await _engineHub.Clients
                .Group(notification.Entity.Id.ToString())
                .SendAsync(EngineMethods.ScenarioCreated, scenario);
        }
    }

    public class ScenarioDeletedSignalRHandler : INotificationHandler<EntityDeleted<ScenarioEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;

        public ScenarioDeletedSignalRHandler(
            IHubContext<EngineHub> engineHub)
        {
            _engineHub = engineHub;
        }

        public async Task Handle(EntityDeleted<ScenarioEntity> notification, CancellationToken cancellationToken)
        {
            await _engineHub.Clients
                .Groups(EngineHub.SCENARIO_GROUP)
                .SendAsync(EngineMethods.ScenarioDeleted, notification.Entity.Id);
            await _engineHub.Clients
                .Group(notification.Entity.Id.ToString())
                .SendAsync(EngineMethods.ScenarioDeleted, notification.Entity.Id);
        }
    }

    public class ScenarioUpdatedSignalRHandler : INotificationHandler<EntityUpdated<ScenarioEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;

        public ScenarioUpdatedSignalRHandler(
            IHubContext<EngineHub> engineHub)
        {
            _engineHub = engineHub;
        }

        public async Task Handle(EntityUpdated<ScenarioEntity> notification, CancellationToken cancellationToken)
        {
            var scenarioSummary = notification.Entity.ToScenarioSummary().ToScenario();
            await _engineHub.Clients.Group(notification.Entity.Id.ToString())
                .SendAsync(EngineMethods.ScenarioUpdated, scenarioSummary);

            var scenario = notification.Entity.ToScenario();
            await _engineHub.Clients
                .Groups(EngineHub.SCENARIO_GROUP)
                .SendAsync(EngineMethods.ScenarioUpdated, scenario);
            await _engineHub.Clients
                .Group(notification.Entity.Id.ToString())
                .SendAsync(EngineMethods.ScenarioUpdated, scenario);
        }
    }
}
