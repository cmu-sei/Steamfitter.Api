// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Services;

namespace Steamfitter.Api.Events.EventHandlers.Scoring
{
    public class ScenarioCreatedScoringHandler : INotificationHandler<EntityCreated<ScenarioEntity>>
    {
        private readonly IScoringService _scoringService;

        public ScenarioCreatedScoringHandler(IScoringService scoringService)
        {
            _scoringService = scoringService;
        }

        public async Task Handle(EntityCreated<ScenarioEntity> notification, CancellationToken cancellationToken)
        {
            if (notification.Entity.UpdateScores)
            {
                await _scoringService.UpdateScenarioScores(notification.Entity.Id, cancellationToken);
            }
        }
    }

    public class ScenarioUpdatedScoringHandler : INotificationHandler<EntityUpdated<ScenarioEntity>>
    {
        private readonly IScoringService _scoringService;

        public ScenarioUpdatedScoringHandler(IScoringService scoringService)
        {
            _scoringService = scoringService;
        }

        public async Task Handle(EntityUpdated<ScenarioEntity> notification, CancellationToken cancellationToken)
        {
            if (notification.Entity.UpdateScores && notification.ModifiedProperties.Contains(nameof(ScenarioEntity.UpdateScores)))
            {
                await _scoringService.UpdateScenarioScores(notification.Entity.Id, cancellationToken);
            }
        }
    }
}
