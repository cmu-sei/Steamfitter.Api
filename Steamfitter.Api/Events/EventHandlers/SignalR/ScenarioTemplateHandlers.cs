// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
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
    public class ScenarioTemplateCreatedSignalRHandler : INotificationHandler<EntityCreated<ScenarioTemplateEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;
        private readonly IMapper _mapper;

        public ScenarioTemplateCreatedSignalRHandler(
            IHubContext<EngineHub> engineHub,
            IMapper mapper)
        {
            _engineHub = engineHub;
            _mapper = mapper;
        }

        public async Task Handle(EntityCreated<ScenarioTemplateEntity> notification, CancellationToken cancellationToken)
        {
            var scenarioTemplate = _mapper.Map<ViewModels.ScenarioTemplate>(notification.Entity);
            await _engineHub.Clients
                .Groups(EngineHub.SCENARIO_TEMPLATE_GROUP)
                .SendAsync(EngineMethods.ScenarioTemplateCreated, scenarioTemplate);
            await _engineHub.Clients
                .Groups(scenarioTemplate.Id.ToString())
                .SendAsync(EngineMethods.ScenarioTemplateCreated, scenarioTemplate);
        }
    }

    public class ScenarioTemplateDeletedSignalRHandler : INotificationHandler<EntityDeleted<ScenarioTemplateEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;

        public ScenarioTemplateDeletedSignalRHandler(
            IHubContext<EngineHub> engineHub)
        {
            _engineHub = engineHub;
        }

        public async Task Handle(EntityDeleted<ScenarioTemplateEntity> notification, CancellationToken cancellationToken)
        {
            await _engineHub.Clients
                .Groups(EngineHub.SCENARIO_TEMPLATE_GROUP)
                .SendAsync(EngineMethods.ScenarioTemplateDeleted, notification.Entity.Id);
            await _engineHub.Clients
                .Groups(notification.Entity.Id.ToString())
                .SendAsync(EngineMethods.ScenarioTemplateDeleted, notification.Entity.Id);
        }
    }

    public class ScenarioTemplateUpdatedSignalRHandler : INotificationHandler<EntityUpdated<ScenarioTemplateEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;
        private readonly IMapper _mapper;

        public ScenarioTemplateUpdatedSignalRHandler(
            IHubContext<EngineHub> engineHub,
            IMapper mapper)
        {
            _engineHub = engineHub;
            _mapper = mapper;
        }

        public async Task Handle(EntityUpdated<ScenarioTemplateEntity> notification, CancellationToken cancellationToken)
        {
            var scenarioTemplate = _mapper.Map<ViewModels.ScenarioTemplate>(notification.Entity);
            await _engineHub.Clients
                .Groups(EngineHub.SCENARIO_TEMPLATE_GROUP)
                .SendAsync(EngineMethods.ScenarioTemplateUpdated, scenarioTemplate);
            await _engineHub.Clients
                .Groups(scenarioTemplate.Id.ToString())
                .SendAsync(EngineMethods.ScenarioTemplateUpdated, scenarioTemplate);
        }
    }
}
