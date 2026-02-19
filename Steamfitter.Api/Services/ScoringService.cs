// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Extensions;
using Steamfitter.Api.Data.Models;

namespace Steamfitter.Api.Services
{
    public interface IScoringService
    {
        Task UpdateScenarioScores(Guid scenarioId, CancellationToken ct = default);
        Task UpdateScenarioTemplateScores(Guid scenarioTemplateId, CancellationToken ct = default);
        Task UpdateAllScores(CancellationToken ct = default);
    }

    public class ScoringService : IScoringService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ScoringService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task UpdateAllScores(CancellationToken ct)
        {
            await UpdateScores(null, null, ct, 0);
        }

        public async Task UpdateScenarioScores(Guid scenarioId, CancellationToken ct)
        {
            await UpdateScores(scenarioId, typeof(ScenarioEntity), ct, 0);
        }

        public async Task UpdateScenarioTemplateScores(Guid scenarioTemplateId, CancellationToken ct)
        {
            await UpdateScores(scenarioTemplateId, typeof(ScenarioTemplateEntity), ct, 0);
        }

        private async Task UpdateScores(Guid? scenarioId, Type type, CancellationToken ct, int attempt = 0)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                using var db = scope.ServiceProvider.GetRequiredService<SteamfitterContext>();

                // create serializable transaction to prevent multiple scores from being changed concurrently,
                // causing incorrect total score calculations
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                // get all tasks in scenario or scenario template
                if (type == typeof(ScenarioEntity))
                {
                    var scenario = await db.Scenarios
                        .Include(x => x.Tasks)
                        .Where(x => x.Id == scenarioId)
                        .SingleOrDefaultAsync(ct);

                    scenario.CalculateScores();
                }
                else if (type == typeof(ScenarioTemplateEntity))
                {
                    var scenarioTemplate = await db.ScenarioTemplates
                        .Include(x => x.Tasks)
                        .Where(x => x.Id == scenarioId)
                        .SingleOrDefaultAsync(ct);

                    scenarioTemplate.CalculateScores();
                }
                else if (scenarioId == null && type == null)
                {
                    // get scenarios and scenarioTemplates that need updating
                    var scenarios = await db.Scenarios
                        .Include(x => x.Tasks)
                        .Where(x => x.UpdateScores)
                        .ToListAsync(ct);

                    var scenarioTemplates = await db.ScenarioTemplates
                        .Include(x => x.Tasks)
                        .Where(x => x.UpdateScores)
                        .ToListAsync(ct);

                    foreach (var scenario in scenarios)
                    {
                        scenario.CalculateScores();
                    }

                    foreach (var scenarioTemplate in scenarioTemplates)
                    {
                        scenarioTemplate.CalculateScores();
                    }
                }

                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                if (ex.IsTransientDatabaseException())
                {
                    attempt = 0;
                }

                if (attempt <= 10)
                {
                    await UpdateScores(scenarioId, type, ct, attempt + 1);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
