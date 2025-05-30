// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Hubs;
using Steamfitter.Api.Infrastructure.HealthChecks;
using System;
using System.Linq;
using System.Threading;
using STT = System.Threading.Tasks;

namespace Steamfitter.Api.Services
{
    public interface ITaskMaintenanceService : IHostedService
    {
    }

    public class TaskMaintenanceService : ITaskMaintenanceService
    {
        private readonly ILogger<TaskMaintenanceService> _logger;
        private readonly IOptionsMonitor<Infrastructure.Options.VmTaskProcessingOptions> _vmTaskProcessingOptions;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMapper _mapper;
        private readonly IHubContext<EngineHub> _engineHub;
        private readonly TaskMaintenanceServiceHealthCheck _taskMaintenanceServiceHealthCheck;

        public TaskMaintenanceService(
            ILogger<TaskMaintenanceService> logger,
            IOptionsMonitor<Infrastructure.Options.VmTaskProcessingOptions> vmTaskProcessingOptions,
            IServiceScopeFactory scopeFactory,
            IMapper mapper,
            IHubContext<EngineHub> engineHub,
            TaskMaintenanceServiceHealthCheck taskMaintenanceServiceHealthCheck)
        {
            _logger = logger;
            _vmTaskProcessingOptions = vmTaskProcessingOptions;
            _scopeFactory = scopeFactory;
            _mapper = mapper;
            _engineHub = engineHub;
            _taskMaintenanceServiceHealthCheck = taskMaintenanceServiceHealthCheck;
            _taskMaintenanceServiceHealthCheck.HealthAllowance = _vmTaskProcessingOptions.CurrentValue.HealthCheckTimeoutSeconds > 0 ? _vmTaskProcessingOptions.CurrentValue.HealthCheckTimeoutSeconds : 90;
        }

        public STT.Task StartAsync(CancellationToken cancellationToken)
        {
            _ = Run();

            return STT.Task.CompletedTask;
        }

        public STT.Task StopAsync(CancellationToken cancellationToken)
        {
            return STT.Task.CompletedTask;
        }

        private async STT.Task Run()
        {
            await STT.Task.Run(async () =>
            {
                _logger.LogDebug("The TaskMaintenanceService is ready to process tasks.");
                while (true)
                {
                    try
                    {
                        // Two scopes are needed because DBContext is not thread-safe
                        using (var scope1 = _scopeFactory.CreateScope())
                        using (var scope2 = _scopeFactory.CreateScope())
                        {
                            var task1 = ExpireTasks(scope1);
                            var task2 = EndScenarios(scope2);
                            await STT.Task.WhenAll(task1, task2);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception encountered in TaskMaintenanceService Run loop.");
                    }
                    var delaySeconds = _vmTaskProcessingOptions.CurrentValue.ExpirationCheckSeconds > 0 ? _vmTaskProcessingOptions.CurrentValue.ExpirationCheckSeconds : 60;
                    _taskMaintenanceServiceHealthCheck.CompletedRun();
                    await STT.Task.Delay(new TimeSpan(0, 0, _vmTaskProcessingOptions.CurrentValue.ExpirationCheckSeconds));
                }
            });
        }

        private async STT.Task ExpireTasks(IServiceScope scope)
        {
            using (var steamfitterContext = scope.ServiceProvider.GetRequiredService<SteamfitterContext>())
            {
                // get results that are currently pending
                var now = DateTime.UtcNow;
                var pendingResultEntities = steamfitterContext.Results.Where(result => result.Status == TaskStatus.pending).ToList();
                if (pendingResultEntities.Any())
                {
                    foreach (var resultEntity in pendingResultEntities)
                    {
                        // set expired results status to expired
                        if (now.Subtract(resultEntity.StatusDate.AddSeconds(resultEntity.ExpirationSeconds)).TotalSeconds >= 0)
                        {
                            resultEntity.Status = TaskStatus.expired;
                            resultEntity.StatusDate = now;
                            await steamfitterContext.SaveChangesAsync();
                            await SendNotificationAsync(resultEntity);
                            _logger.LogDebug($"TaskMaintenanceService expired Result {resultEntity.Id}.");
                        }
                    }
                }
            }
        }

        private async STT.Task EndScenarios(IServiceScope scope)
        {
            using (var steamfitterContext = scope.ServiceProvider.GetRequiredService<SteamfitterContext>())
            {
                // get scenarios that are currently active
                var now = DateTime.UtcNow;
                var activeScenarioEntities = steamfitterContext.Scenarios.Where(scenario => scenario.Status == ScenarioStatus.active).ToList();
                if (activeScenarioEntities.Any())
                {
                    foreach (var scenarioEntity in activeScenarioEntities)
                    {
                        // set expired scenario status to ended
                        if (now.Subtract(scenarioEntity.EndDate).TotalSeconds >= 0)
                        {
                            scenarioEntity.Status = ScenarioStatus.ended;
                            await steamfitterContext.SaveChangesAsync();
                            _logger.LogDebug($"TaskMaintenanceService ended Scenario {scenarioEntity.Id}.");
                        }
                    }
                }
            }
        }

        private async STT.Task SendNotificationAsync(ResultEntity resultEntity)
        {
            await _engineHub.Clients.Group(EngineHub.SCENARIO_GROUP)
                .SendAsync(EngineMethods.ResultsUpdated,
                _mapper.Map<ViewModels.Result>(resultEntity));
            await _engineHub.Clients.Group(resultEntity.Task.ScenarioId.ToString())
                .SendAsync(EngineMethods.ResultsUpdated,
                _mapper.Map<ViewModels.Result>(resultEntity));
        }

    }

}
