// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
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
        private readonly IMapper _mapper;

        public ScenarioCreatedSignalRHandler(
            IHubContext<EngineHub> engineHub,
            IMapper mapper)
        {
            _engineHub = engineHub;
            _mapper = mapper;
        }

        public async Task Handle(EntityCreated<ScenarioEntity> notification, CancellationToken cancellationToken)
        {
            var scenarioSummary = _mapper.Map<ViewModels.Scenario>(
                _mapper.Map<ViewModels.ScenarioSummary>(notification.Entity));
            await _engineHub.Clients.Group(notification.Entity.Id.ToString())
                .SendAsync(EngineMethods.ScenarioCreated, scenarioSummary);

            var scenario = _mapper.Map<ViewModels.Scenario>(notification.Entity);
            await _engineHub.Clients
                .Groups(
                    EngineGroups.GetSystemGroup(notification.Entity.Id),
                    EngineGroups.SystemGroup)
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
                .Groups(
                    notification.Entity.Id.ToString(),
                    EngineGroups.GetSystemGroup(notification.Entity.Id),
                    EngineGroups.SystemGroup
                    )
                .SendAsync(EngineMethods.ScenarioDeleted, notification.Entity.Id);
        }
    }

    public class ScenarioUpdatedSignalRHandler : INotificationHandler<EntityUpdated<ScenarioEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;
        private readonly IMapper _mapper;

        public ScenarioUpdatedSignalRHandler(
            IHubContext<EngineHub> engineHub,
            IMapper mapper)
        {
            _engineHub = engineHub;
            _mapper = mapper;
        }

        public async Task Handle(EntityUpdated<ScenarioEntity> notification, CancellationToken cancellationToken)
        {
            var scenarioSummary = _mapper.Map<ViewModels.Scenario>(
                _mapper.Map<ViewModels.ScenarioSummary>(notification.Entity));
            await _engineHub.Clients.Group(notification.Entity.Id.ToString())
                .SendAsync(EngineMethods.ScenarioUpdated, scenarioSummary);

            var scenario = _mapper.Map<ViewModels.Scenario>(notification.Entity);
            await _engineHub.Clients
                .Groups(
                    EngineGroups.GetSystemGroup(notification.Entity.Id),
                    EngineGroups.SystemGroup)
                .SendAsync(EngineMethods.ScenarioUpdated, scenario);
        }
    }
}
