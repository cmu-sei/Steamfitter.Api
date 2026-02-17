// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Crucible.Common.EntityEvents.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Hubs;

namespace Steamfitter.Api.Events.EventHandlers.SignalR
{
    public class TaskCreatedSignalRHandler : INotificationHandler<EntityCreated<TaskEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;
        private readonly IMapper _mapper;

        public TaskCreatedSignalRHandler(
            IHubContext<EngineHub> engineHub,
            IMapper mapper)
        {
            _engineHub = engineHub;
            _mapper = mapper;
        }

        public async Task Handle(EntityCreated<TaskEntity> notification, CancellationToken cancellationToken)
        {
            var task = _mapper.Map<ViewModels.Task>(notification.Entity);

            if (notification.Entity.ScenarioTemplateId.HasValue)
            {
                await _engineHub.Clients.Group(EngineHub.SCENARIO_TEMPLATE_GROUP).SendAsync(EngineMethods.TaskCreated, task);
                await _engineHub.Clients.Group(notification.Entity.ScenarioTemplateId.Value.ToString()).SendAsync(EngineMethods.TaskCreated, task);
            }
            else if (notification.Entity.ScenarioId.HasValue)
            {
                var taskSummary = _mapper.Map<ViewModels.Task>(
                    _mapper.Map<ViewModels.TaskSummary>(notification.Entity));

                await _engineHub.Clients
                    .Group(EngineHub.SCENARIO_GROUP)
                    .SendAsync(EngineMethods.TaskCreated, task);

                await _engineHub.Clients
                    .Group(notification.Entity.ScenarioId.Value.ToString())
                    .SendAsync(EngineMethods.TaskCreated, taskSummary);
            }
        }
    }

    public class TaskDeletedSignalRHandler : INotificationHandler<EntityDeleted<TaskEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;

        public TaskDeletedSignalRHandler(
            IHubContext<EngineHub> engineHub)
        {
            _engineHub = engineHub;
        }

        public async Task Handle(EntityDeleted<TaskEntity> notification, CancellationToken cancellationToken)
        {
            var groups = new List<string>();

            if (notification.Entity.ScenarioTemplateId.HasValue)
            {
                groups.Add(EngineHub.SCENARIO_TEMPLATE_GROUP);
                groups.Add(notification.Entity.ScenarioTemplateId.Value.ToString());
            }
            else if (notification.Entity.ScenarioId.HasValue)
            {
                groups.Add(EngineHub.SCENARIO_GROUP);
                groups.Add(notification.Entity.ScenarioId.Value.ToString());
            }

            await _engineHub.Clients
                .Groups(groups)
                .SendAsync(EngineMethods.TaskDeleted, notification.Entity.Id);
        }
    }

    public class TaskUpdatedSignalRHandler : INotificationHandler<EntityUpdated<TaskEntity>>
    {
        private readonly IHubContext<EngineHub> _engineHub;
        private readonly IMapper _mapper;

        public TaskUpdatedSignalRHandler(
            IHubContext<EngineHub> engineHub,
            IMapper mapper)
        {
            _engineHub = engineHub;
            _mapper = mapper;
        }

        public async Task Handle(EntityUpdated<TaskEntity> notification, CancellationToken cancellationToken)
        {
            var task = _mapper.Map<ViewModels.Task>(notification.Entity);

            if (notification.Entity.ScenarioTemplateId.HasValue)
            {
                await _engineHub.Clients.Group(EngineHub.SCENARIO_TEMPLATE_GROUP).SendAsync(EngineMethods.TaskUpdated, task);
            }
            else if (notification.Entity.ScenarioId.HasValue)
            {
                var taskSummary = _mapper.Map<ViewModels.Task>(
                    _mapper.Map<ViewModels.TaskSummary>(notification.Entity));

                await _engineHub.Clients
                    .Groups(EngineHub.SCENARIO_GROUP)
                    .SendAsync(EngineMethods.TaskUpdated, task);

                await _engineHub.Clients
                    .Group(notification.Entity.ScenarioId.Value.ToString())
                    .SendAsync(EngineMethods.TaskUpdated, taskSummary);
            }
        }
    }
}
