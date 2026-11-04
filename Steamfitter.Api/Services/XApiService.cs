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
            _account.Name = subjectId;
            if (!string.IsNullOrWhiteSpace(_xApiOptions.IssuerUrl))
            {
                _account.HomePage = new Uri(_xApiOptions.IssuerUrl);
            }
            else if (issuer?.Contains("http") == true)
            {
                _account.HomePage = new Uri(issuer);
            }
            else
            {
                _account.HomePage = new Uri("http://" + (issuer ?? "localhost"));
            }

            // configure Agent
            _agent = new TinCan.Agent();
            var userEntity = _context.Users.Find(userId);
            _agent.Name = userEntity?.Name ?? "Unknown User";
            _agent.Account = _account;

            // Store the current user ID for use in queuing statements
            _currentUserId = userId;

            // Initialize the Context
            _xApiContext = new Context();
            _xApiContext.Platform = _xApiOptions.Platform;
            _xApiContext.Language = "en-US";
        }

        public Boolean IsConfigured()
        {
            return _xApiOptions.Enabled && !string.IsNullOrWhiteSpace(_xApiOptions.Username);
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
            verb.Id = new Uri("http://adlnet.gov/expapi/verbs/launched");
            verb.Display = new LanguageMap();
            verb.Display.Add("en-US", "launched");

            var activity = new Activity();
            activity.Id = _xApiOptions.ApiUrl + "scenarios/" + scenarioId;
            activity.Definition = new TinCan.ActivityDefinition();
            activity.Definition.Type = new Uri("http://adlnet.gov/expapi/activities/simulation");
            activity.Definition.Name = new LanguageMap();
            activity.Definition.Name.Add("en-US", scenario.Name);
            activity.Definition.Description = new LanguageMap();
            activity.Definition.Description.Add("en-US", scenario.Description ?? scenario.Name);

            if (!string.IsNullOrWhiteSpace(_xApiOptions.UiUrl))
            {
                activity.Definition.MoreInfo = new Uri(_xApiOptions.UiUrl.TrimEnd('/') + "/scenario/" + scenarioId);
            }

            var context = new Context();
            context.Platform = _xApiContext.Platform;
            context.Language = _xApiContext.Language;
            context.Registration = scenarioId; // Group statements by scenario session

            var statement = new Statement();
            statement.Actor = _agent;
            statement.Verb = verb;
            statement.Target = activity;
            statement.Context = context;

            return await QueueStatementAsync(statement, verb.Id, activity.Id, scenarioId, ct);
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
            verb.Id = new Uri("http://adlnet.gov/expapi/verbs/terminated");
            verb.Display = new LanguageMap();
            verb.Display.Add("en-US", "terminated");

            var activity = new Activity();
            activity.Id = _xApiOptions.ApiUrl + "scenarios/" + scenarioId;
            activity.Definition = new TinCan.ActivityDefinition();
            activity.Definition.Type = new Uri("http://adlnet.gov/expapi/activities/simulation");
            activity.Definition.Name = new LanguageMap();
            activity.Definition.Name.Add("en-US", scenario.Name);
            activity.Definition.Description = new LanguageMap();
            activity.Definition.Description.Add("en-US", scenario.Description ?? scenario.Name);

            if (!string.IsNullOrWhiteSpace(_xApiOptions.UiUrl))
            {
                activity.Definition.MoreInfo = new Uri(_xApiOptions.UiUrl.TrimEnd('/') + "/scenario/" + scenarioId);
            }

            var context = new Context();
            context.Platform = _xApiContext.Platform;
            context.Language = _xApiContext.Language;
            context.Registration = scenarioId; // Group statements by scenario session

            var statement = new Statement();
            statement.Actor = _agent;
            statement.Verb = verb;
            statement.Target = activity;
            statement.Context = context;

            return await QueueStatementAsync(statement, verb.Id, activity.Id, scenarioId, ct);
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
            verb.Id = new Uri("http://adlnet.gov/expapi/verbs/executed");
            verb.Display = new LanguageMap();
            verb.Display.Add("en-US", "executed");

            var activity = new Activity();
            activity.Id = _xApiOptions.ApiUrl + "tasks/" + taskId;
            activity.Definition = new TinCan.ActivityDefinition();
            activity.Definition.Type = new Uri("http://adlnet.gov/expapi/activities/cmi.interaction");
            activity.Definition.Name = new LanguageMap();
            activity.Definition.Name.Add("en-US", task.Name);
            activity.Definition.Description = new LanguageMap();
            activity.Definition.Description.Add("en-US", task.Description ?? task.Name);

            var activityExtensions = new Newtonsoft.Json.Linq.JObject();
            activityExtensions["https://crucible.sei.cmu.edu/xapi/extensions/task-action"] = task.Action.ToString();
            if (!string.IsNullOrWhiteSpace(task.VmMask))
            {
                activityExtensions["https://crucible.sei.cmu.edu/xapi/extensions/vm-mask"] = task.VmMask;
            }
            if (!string.IsNullOrWhiteSpace(task.ApiUrl))
            {
                activityExtensions["https://crucible.sei.cmu.edu/xapi/extensions/api-url"] = task.ApiUrl;
            }
            if (!string.IsNullOrWhiteSpace(task.ExpectedOutput))
            {
                activityExtensions["https://crucible.sei.cmu.edu/xapi/extensions/expected-output"] = task.ExpectedOutput;
            }
            activity.Definition.Extensions = new TinCan.Extensions(activityExtensions);

            var result = new Result();
            result.Completion = task.IsComplete;
            result.Success = task.Status == Data.TaskStatus.succeeded;
            if (task.Score > 0)
            {
                result.Score = new Score();
                result.Score.Raw = task.ScoreEarned;
                result.Score.Max = task.Score;
                result.Score.Min = 0;
                result.Score.Scaled = task.Score > 0 ? (double)task.ScoreEarned / task.Score : 0;
            }

            var context = new Context();
            context.Platform = _xApiContext.Platform;
            context.Language = _xApiContext.Language;
            context.Registration = scenarioId;

            var contextActivities = new ContextActivities();
            var parentActivity = new Activity();
            parentActivity.Id = _xApiOptions.ApiUrl + "scenarios/" + scenarioId;
            parentActivity.Definition = new ActivityDefinition();
            parentActivity.Definition.Type = new Uri("http://adlnet.gov/expapi/activities/simulation");
            parentActivity.Definition.Name = new LanguageMap();
            parentActivity.Definition.Name.Add("en-US", scenario.Name);
            parentActivity.Definition.Description = new LanguageMap();
            parentActivity.Definition.Description.Add("en-US", scenario.Description ?? scenario.Name);
            contextActivities.Parent = new List<Activity> { parentActivity };

            var groupingList = new List<Activity>();
            var moveGroupMatch = System.Text.RegularExpressions.Regex.Match(task.Name ?? "", @"^(\d+)-(\d+)\s");
            if (moveGroupMatch.Success)
            {
                var moveNumber = int.Parse(moveGroupMatch.Groups[1].Value);
                var groupNumber = int.Parse(moveGroupMatch.Groups[2].Value);

                var moveActivity = new Activity();
                moveActivity.Id = _xApiOptions.ApiUrl + "scenarios/" + scenarioId + "/move/" + moveNumber;
                moveActivity.Definition = new ActivityDefinition();
                moveActivity.Definition.Type = new Uri("http://id.tincanapi.com/activitytype/collection-simple");
                moveActivity.Definition.Name = new LanguageMap();
                moveActivity.Definition.Name.Add("en-US", $"Move {moveNumber}");
                groupingList.Add(moveActivity);

                var groupActivity = new Activity();
                groupActivity.Id = _xApiOptions.ApiUrl + "scenarios/" + scenarioId + "/move/" + moveNumber + "/group/" + groupNumber;
                groupActivity.Definition = new ActivityDefinition();
                groupActivity.Definition.Type = new Uri("http://id.tincanapi.com/activitytype/collection-simple");
                groupActivity.Definition.Name = new LanguageMap();
                groupActivity.Definition.Name.Add("en-US", $"Group {groupNumber}");
                groupingList.Add(groupActivity);
            }
            if (groupingList.Count > 0)
            {
                contextActivities.Grouping = groupingList;
            }

            context.ContextActivities = contextActivities;

            var statement = new Statement();
            statement.Actor = _agent;
            statement.Verb = verb;
            statement.Target = activity;
            statement.Result = result;
            statement.Context = context;

            return await QueueStatementAsync(statement, verb.Id, activity.Id, scenarioId, ct);
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
                    StatementJson = statement.ToJson(true),
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
