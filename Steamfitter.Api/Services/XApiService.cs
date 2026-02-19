// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Infrastructure.Extensions;
using Steamfitter.Api.Infrastructure.Options;
using TinCan;

namespace Steamfitter.Api.Services
{
    public interface IXApiService
    {
        Boolean IsConfigured();
        Task<Boolean> ScenarioStartedAsync(Guid scenarioId, CancellationToken ct);
        Task<Boolean> ScenarioEndedAsync(Guid scenarioId, CancellationToken ct);
        Task<Boolean> TaskExecutedAsync(Guid taskId, Guid scenarioId, Guid? userId, CancellationToken ct);
    }

    public class XApiService : IXApiService
    {
        private readonly SteamfitterContext _context;
        private readonly ClaimsPrincipal _user;
        private readonly XApiOptions _xApiOptions;
        private readonly IXApiQueueService _queueService;
        private Agent _agent;
        private AgentAccount _account;
        private Context _xApiContext;
        private Guid? _currentUserId;
        private readonly ILogger<XApiService> _logger;

        public XApiService(
            SteamfitterContext context,
            IPrincipal user,
            XApiOptions xApiOptions,
            IXApiQueueService queueService,
            ILogger<XApiService> logger)
        {
            _context = context;
            _user = user as ClaimsPrincipal;
            _xApiOptions = xApiOptions;
            _queueService = queueService;
            _logger = logger;
        }

        private void EnsureAgentInitialized(Guid? userIdOverride = null)
        {
            if (!IsConfigured())
                return;

            // Reset agent if we're initializing for a different user context
            if (_agent != null && userIdOverride.HasValue)
            {
                _agent = null;
                _account = null;
            }
            else if (_agent != null)
            {
                return;
            }

            // Determine the user ID - use override if provided, otherwise get from user principal
            Guid userId;
            string subjectId;
            string issuer;

            if (userIdOverride.HasValue)
            {
                // Background task scenario - use provided user ID
                userId = userIdOverride.Value;
                var user = _context.Users.Find(userId);
                subjectId = userId.ToString();
                issuer = _xApiOptions.IssuerUrl ?? "http://localhost";
            }
            else if (_user != null)
            {
                // HTTP request scenario - use claims principal
                subjectId = _user.Identities.First().Claims.First(c => c.Type == "sub")?.Value;
                issuer = _user.Identities.First().Claims.First(c => c.Type == "iss")?.Value;
                userId = _user.GetId();
            }
            else
            {
                _logger.LogWarning("No user context available for xAPI statement");
                return;
            }

            // configure AgentAccount
            _account = new TinCan.AgentAccount();
            _account.name = subjectId;
            if (!string.IsNullOrWhiteSpace(_xApiOptions.IssuerUrl))
            {
                _account.homePage = new Uri(_xApiOptions.IssuerUrl);
            }
            else if (issuer?.Contains("http") == true)
            {
                _account.homePage = new Uri(issuer);
            }
            else
            {
                _account.homePage = new Uri("http://" + (issuer ?? "localhost"));
            }

            // configure Agent
            _agent = new TinCan.Agent();
            var userEntity = _context.Users.Find(userId);
            _agent.name = userEntity?.Name ?? "Unknown User";
            _agent.account = _account;

            // Store the current user ID for use in queuing statements
            _currentUserId = userId;

            // Initialize the Context
            _xApiContext = new Context();
            _xApiContext.platform = _xApiOptions.Platform;
            _xApiContext.language = "en-US";
        }

        public Boolean IsConfigured()
        {
            return !string.IsNullOrWhiteSpace(_xApiOptions.Username);
        }

        public async Task<Boolean> ScenarioStartedAsync(Guid scenarioId, CancellationToken ct)
        {
            if (!IsConfigured())
            {
                return false;
            }

            EnsureAgentInitialized();

            var scenario = await _context.Scenarios.FindAsync(new object[] { scenarioId }, ct);
            if (scenario == null)
            {
                _logger.LogWarning("Cannot create xAPI statement for non-existent scenario {ScenarioId}", scenarioId);
                return false;
            }

            var verb = new Verb();
            verb.id = new Uri("http://adlnet.gov/expapi/verbs/launched");
            verb.display = new LanguageMap();
            verb.display.Add("en-US", "launched");

            var activity = new Activity();
            activity.id = _xApiOptions.ApiUrl + "scenarios/" + scenarioId;
            activity.definition = new TinCan.ActivityDefinition();
            activity.definition.type = new Uri("http://adlnet.gov/expapi/activities/simulation");
            activity.definition.name = new LanguageMap();
            activity.definition.name.Add("en-US", scenario.Name);
            activity.definition.description = new LanguageMap();
            activity.definition.description.Add("en-US", scenario.Description ?? scenario.Name);

            if (!string.IsNullOrWhiteSpace(_xApiOptions.UiUrl))
            {
                activity.definition.moreInfo = new Uri(_xApiOptions.UiUrl + "/scenario/" + scenarioId);
            }

            var context = new Context();
            context.platform = _xApiContext.platform;
            context.language = _xApiContext.language;
            context.registration = scenarioId; // Group statements by scenario session

            var statement = new Statement();
            statement.actor = _agent;
            statement.verb = verb;
            statement.target = activity;
            statement.context = context;

            return await QueueStatementAsync(statement, verb.id, activity.id, scenarioId, ct);
        }

        public async Task<Boolean> ScenarioEndedAsync(Guid scenarioId, CancellationToken ct)
        {
            if (!IsConfigured())
            {
                return false;
            }

            EnsureAgentInitialized();

            var scenario = await _context.Scenarios.FindAsync(new object[] { scenarioId }, ct);
            if (scenario == null)
            {
                _logger.LogWarning("Cannot create xAPI statement for non-existent scenario {ScenarioId}", scenarioId);
                return false;
            }

            var verb = new Verb();
            verb.id = new Uri("http://adlnet.gov/expapi/verbs/terminated");
            verb.display = new LanguageMap();
            verb.display.Add("en-US", "terminated");

            var activity = new Activity();
            activity.id = _xApiOptions.ApiUrl + "scenarios/" + scenarioId;
            activity.definition = new TinCan.ActivityDefinition();
            activity.definition.type = new Uri("http://adlnet.gov/expapi/activities/simulation");
            activity.definition.name = new LanguageMap();
            activity.definition.name.Add("en-US", scenario.Name);
            activity.definition.description = new LanguageMap();
            activity.definition.description.Add("en-US", scenario.Description ?? scenario.Name);

            if (!string.IsNullOrWhiteSpace(_xApiOptions.UiUrl))
            {
                activity.definition.moreInfo = new Uri(_xApiOptions.UiUrl + "/scenario/" + scenarioId);
            }

            var context = new Context();
            context.platform = _xApiContext.platform;
            context.language = _xApiContext.language;
            context.registration = scenarioId; // Group statements by scenario session

            var statement = new Statement();
            statement.actor = _agent;
            statement.verb = verb;
            statement.target = activity;
            statement.context = context;

            return await QueueStatementAsync(statement, verb.id, activity.id, scenarioId, ct);
        }

        public async Task<Boolean> TaskExecutedAsync(Guid taskId, Guid scenarioId, Guid? userId, CancellationToken ct)
        {
            if (!IsConfigured())
            {
                _logger.LogDebug("xAPI is not configured, skipping TaskExecutedAsync for task {TaskId}", taskId);
                return false;
            }

            var task = await _context.Tasks.FindAsync(new object[] { taskId }, ct);
            if (task == null)
            {
                _logger.LogWarning("Cannot create xAPI statement for non-existent task {TaskId}", taskId);
                return false;
            }

            var scenario = await _context.Scenarios.FindAsync(new object[] { scenarioId }, ct);
            if (scenario == null)
            {
                _logger.LogWarning("Cannot create xAPI statement for non-existent scenario {ScenarioId}", scenarioId);
                return false;
            }

            // If task doesn't have a userId, use the scenario creator as fallback
            var effectiveUserId = userId ?? scenario.CreatedBy;

            _logger.LogInformation("Creating xAPI statement for task {TaskId} executed by user {UserId}", taskId, effectiveUserId);

            EnsureAgentInitialized(effectiveUserId);

            if (_agent == null)
            {
                _logger.LogWarning("Failed to initialize xAPI agent for task {TaskId}", taskId);
                return false;
            }

            var verb = new Verb();
            verb.id = new Uri("http://adlnet.gov/expapi/verbs/executed");
            verb.display = new LanguageMap();
            verb.display.Add("en-US", "executed");

            var activity = new Activity();
            activity.id = _xApiOptions.ApiUrl + "tasks/" + taskId;
            activity.definition = new TinCan.ActivityDefinition();
            activity.definition.type = new Uri("http://adlnet.gov/expapi/activities/cmi.interaction");
            activity.definition.name = new LanguageMap();
            activity.definition.name.Add("en-US", task.Name);
            activity.definition.description = new LanguageMap();
            activity.definition.description.Add("en-US", task.Description ?? task.Name);

            var context = new Context();
            context.platform = _xApiContext.platform;
            context.language = _xApiContext.language;
            context.registration = scenarioId; // Group statements by scenario session

            // Add scenario as parent activity
            var contextActivities = new ContextActivities();
            var parentActivity = new Activity();
            parentActivity.id = _xApiOptions.ApiUrl + "scenarios/" + scenarioId;
            parentActivity.definition = new ActivityDefinition();
            parentActivity.definition.type = new Uri("http://adlnet.gov/expapi/activities/simulation");
            parentActivity.definition.name = new LanguageMap();
            parentActivity.definition.name.Add("en-US", scenario.Name);
            parentActivity.definition.description = new LanguageMap();
            parentActivity.definition.description.Add("en-US", scenario.Description ?? scenario.Name);
            contextActivities.parent = new List<Activity> { parentActivity };
            context.contextActivities = contextActivities;

            var statement = new Statement();
            statement.actor = _agent;
            statement.verb = verb;
            statement.target = activity;
            statement.context = context;

            return await QueueStatementAsync(statement, verb.id, activity.id, scenarioId, ct);
        }

        private async Task<Boolean> QueueStatementAsync(
            Statement statement,
            Uri verbUri,
            string activityId,
            Guid scenarioId,
            CancellationToken ct)
        {
            try
            {
                var queuedStatement = new XApiQueuedStatementEntity
                {
                    StatementJson = statement.ToJSON(true),
                    Verb = verbUri.ToString(),
                    ActivityId = activityId,
                    ScenarioId = scenarioId,
                    CreatedBy = _currentUserId ?? (_user != null ? _user.GetId() : Guid.Empty)
                };

                await _queueService.EnqueueAsync(queuedStatement, ct);
                _logger.LogInformation("Enqueued xAPI statement for verb {Verb}", verbUri);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue xAPI statement");
                return false;
            }
        }
    }
}
