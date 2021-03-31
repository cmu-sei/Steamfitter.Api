// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
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
                await _engineHub.Clients.Group(EngineGroups.SystemGroup).SendAsync(EngineMethods.TaskCreated, task);
            }
            else if (notification.Entity.ScenarioId.HasValue)
            {
                var taskSummary = _mapper.Map<ViewModels.Task>(
                    _mapper.Map<ViewModels.TaskSummary>(notification.Entity));

                await _engineHub.Clients
                    .Groups(
                        EngineGroups.GetSystemGroup(notification.Entity.ScenarioId.Value),
                        EngineGroups.SystemGroup)
                    .SendAsync(EngineMethods.TaskCreated, task);

                await _engineHub.Clients
                    .Group(notification.Entity.Id.ToString())
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
            var groups = new List<string>() { EngineGroups.SystemGroup };

            if (notification.Entity.ScenarioTemplateId.HasValue)
            {
                groups.Add(EngineGroups.GetSystemGroup(notification.Entity.ScenarioTemplateId.Value));
            }
            else if (notification.Entity.ScenarioId.HasValue)
            {
                groups.Add(EngineGroups.GetSystemGroup(notification.Entity.ScenarioId.Value));
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
                await _engineHub.Clients.Group(EngineGroups.SystemGroup).SendAsync(EngineMethods.TaskUpdated, task);
            }
            else if (notification.Entity.ScenarioId.HasValue)
            {
                var taskSummary = _mapper.Map<ViewModels.Task>(
                    _mapper.Map<ViewModels.TaskSummary>(notification.Entity));

                await _engineHub.Clients
                    .Groups(
                        EngineGroups.GetSystemGroup(notification.Entity.ScenarioId.Value),
                        EngineGroups.SystemGroup)
                    .SendAsync(EngineMethods.TaskUpdated, task);

                await _engineHub.Clients
                    .Group(notification.Entity.ScenarioId.ToString())
                    .SendAsync(EngineMethods.TaskUpdated, taskSummary);
            }
        }
    }
}
