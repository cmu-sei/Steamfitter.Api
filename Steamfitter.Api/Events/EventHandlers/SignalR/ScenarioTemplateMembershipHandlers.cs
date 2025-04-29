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
    public class ScenarioTemplateMembershipCreatedSignalRHandler : INotificationHandler<EntityCreated<ScenarioTemplateMembershipEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;
        private readonly IMapper _mapper;

        public ScenarioTemplateMembershipCreatedSignalRHandler(
            IHubContext<EngineHub> engineHub,
            IMapper mapper)
        {
            _engineHub = engineHub;
            _mapper = mapper;
        }

        public async Task Handle(EntityCreated<ScenarioTemplateMembershipEntity> notification, CancellationToken cancellationToken)
        {
            var scenarioTemplateMembership = _mapper.Map<ViewModels.ScenarioTemplateMembership>(notification.Entity);
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
        private readonly IMapper _mapper;

        public ScenarioTemplateMembershipUpdatedSignalRHandler(
            IHubContext<EngineHub> engineHub,
            IMapper mapper)
        {
            _engineHub = engineHub;
            _mapper = mapper;
        }

        public async Task Handle(EntityUpdated<ScenarioTemplateMembershipEntity> notification, CancellationToken cancellationToken)
        {
            var scenarioTemplateMembership = _mapper.Map<ViewModels.ScenarioTemplateMembership>(notification.Entity);
            await _engineHub.Clients
                .Groups(EngineHub.SCENARIO_TEMPLATE_GROUP)
                .SendAsync(EngineMethods.ScenarioTemplateMembershipUpdated, scenarioTemplateMembership);
        }
    }
}
