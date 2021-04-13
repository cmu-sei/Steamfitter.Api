// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steamfitter.Api.Data;
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
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var task1 = ExpireTasks(scope);
                            var task2 = EndScenarios(scope);
                            await STT.Task.WhenAll(task1, task2);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Exception encountered in TaskMaintenanceService Run loop.", ex);
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
                            _engineHub.Clients.All.SendAsync(EngineMethods.ResultUpdated, _mapper.Map<ViewModels.Result>(resultEntity));
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
                            _engineHub.Clients.All.SendAsync(EngineMethods.ScenarioUpdated, _mapper.Map<ViewModels.Scenario>(scenarioEntity));
                            _logger.LogDebug($"TaskMaintenanceService ended Scenario {scenarioEntity.Id}.");
                        }
                    }
                }
            }
        }

    }

}
