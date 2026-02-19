// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Crucible.Common.EntityEvents.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Extensions;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Services;

namespace Steamfitter.Api.Events.EventHandlers.Scoring
{
    public class ScenarioTemplateUpdatedScoringHandler : INotificationHandler<EntityUpdated<ScenarioTemplateEntity>>
    {
        private readonly IScoringService _scoringService;

        public ScenarioTemplateUpdatedScoringHandler(IScoringService scoringService)
        {
            _scoringService = scoringService;
        }

        public async Task Handle(EntityUpdated<ScenarioTemplateEntity> notification, CancellationToken cancellationToken)
        {
            if (notification.Entity.UpdateScores && notification.ModifiedProperties.Contains(nameof(ScenarioTemplateEntity.UpdateScores)))
            {
                await _scoringService.UpdateScenarioTemplateScores(notification.Entity.Id, cancellationToken);
            }
        }
    }
}
