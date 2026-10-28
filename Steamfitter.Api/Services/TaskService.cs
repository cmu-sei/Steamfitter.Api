// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using STT = System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Infrastructure.Extensions;
using Steamfitter.Api.Infrastructure.Exceptions;
using Steamfitter.Api.Infrastructure.Options;
using SAVM = Steamfitter.Api.ViewModels;
using System.Data;
using Steamfitter.Api.Data.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Steamfitter.Api.Services
{
    public interface ITaskService
    {
        STT.Task<IEnumerable<SAVM.Task>> GetAsync(CancellationToken ct);
        STT.Task<SAVM.Task> GetAsync(Guid Id, CancellationToken ct);
        STT.Task<IEnumerable<SAVM.Task>> GetByScenarioTemplateIdAsync(Guid scenarioTemplateId, CancellationToken ct);
        STT.Task<IEnumerable<SAVM.Task>> GetByScenarioIdAsync(Guid scenarioId, CancellationToken ct);
        STT.Task<IEnumerable<SAVM.Task>> GetByViewIdAsync(Guid viewId, IReadOnlyCollection<Guid> scenarioIds, CancellationToken ct);
        STT.Task<IEnumerable<SAVM.Task>> GetByUserIdAsync(Guid userId, CancellationToken ct);
        STT.Task<IEnumerable<SAVM.Task>> GetByVmIdAsync(Guid vmId, CancellationToken ct);
        STT.Task<IEnumerable<SAVM.Task>> GetSubtasksAsync(Guid triggerTaskId, CancellationToken ct);
        STT.Task<SAVM.Task> CreateAsync(SAVM.TaskCreate taskForm, CancellationToken ct);
        STT.Task<IEnumerable<SAVM.Result>> CreateAndExecuteAsync(SAVM.TaskCreate taskForm, CancellationToken ct);
        STT.Task<IEnumerable<SAVM.Result>> ExecuteAsync(Guid id, CancellationToken ct);
        STT.Task<Guid?> ExecuteWithSubstitutionsAsync(Guid id, Dictionary<string, string> taskSubstitutions, CancellationToken ct);
        STT.Task<Guid?> ExecuteForGradeAsync(GradedExecutionInfo gradedExecutionInfo, CancellationToken ct);
        STT.Task<SAVM.Task> UpdateAsync(Guid Id, SAVM.TaskEdit taskForm, CancellationToken ct);
        STT.Task<bool> DeleteAsync(Guid Id, CancellationToken ct);
        STT.Task DetachChildrenInOtherScopesAsync(IReadOnlyCollection<Guid> taskIds, Guid? scenarioId, Guid? scenarioTemplateId, CancellationToken ct);
        STT.Task<IEnumerable<SAVM.Task>> CopyAsync(Guid id, Guid newLocationId, string newLocationType, CancellationToken ct);
        STT.Task<SAVM.Task> CreateFromResultAsync(Guid resultId, Guid newLocationId, string newLocationType, CancellationToken ct);
        STT.Task<IEnumerable<SAVM.Task>> MoveAsync(Guid id, Guid newLocationId, string newLocationType, CancellationToken ct);
    }

    public class TaskService : ITaskService
    {
        private readonly SteamfitterContext _context;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;
        private readonly VmTaskProcessingOptions _options;
        private readonly IResultService _resultService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TaskService> _logger;
        private readonly IPlayerService _playerService;
        private readonly IPlayerVmService _playerVmService;
        private readonly bool _isHttps;
        private readonly ITaskExecutionQueue _taskExecutionQueue;
        private readonly IServiceScopeFactory _scopeFactory;

        public TaskService(
            SteamfitterContext context,
            IPrincipal user,
            IMapper mapper,
            IOptions<VmTaskProcessingOptions> options,
            IResultService resultService,
            ILogger<TaskService> logger,
            IPlayerService playerService,
            IPlayerVmService playerVmService,
            IHttpClientFactory httpClientFactory,
            ClientOptions clientSettings,
            ITaskExecutionQueue taskexecutionQueue,
            IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
            _options = options.Value;
            _resultService = resultService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _playerService = playerService;
            _playerVmService = playerVmService;
            _isHttps = clientSettings.urls.playerApi.ToLower().StartsWith("https:");
            _taskExecutionQueue = taskexecutionQueue;
            _scopeFactory = scopeFactory;
        }

        public async STT.Task<IEnumerable<SAVM.Task>> GetAsync(CancellationToken ct)
        {
            var items = await _context.Tasks.ToListAsync(ct);

            return _mapper.Map<IEnumerable<SAVM.Task>>(items);
        }

        public async STT.Task<SAVM.Task> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.Tasks
                .SingleOrDefaultAsync(o => o.Id == id, ct);
            if (item == null)
                throw new EntityNotFoundException<SAVM.Task>();

            return _mapper.Map<SAVM.Task>(item);
        }

        public async STT.Task<IEnumerable<SAVM.Task>> GetByScenarioTemplateIdAsync(Guid scenarioTemplateId, CancellationToken ct)
        {
            var Tasks = _context.Tasks.Where(x => x.ScenarioTemplateId == scenarioTemplateId);

            return _mapper.Map<IEnumerable<SAVM.Task>>(Tasks);
        }

        public async STT.Task<IEnumerable<SAVM.Task>> GetByScenarioIdAsync(Guid scenarioId, CancellationToken ct)
        {
            var tasks = await _context.Tasks
                .Where(x => x.ScenarioId == scenarioId)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SAVM.Task>>(tasks);
        }

        /// <summary>The user-executable Tasks of the View's Scenarios, of only the given Scenarios when they are given.</summary>
        public async STT.Task<IEnumerable<SAVM.Task>> GetByViewIdAsync(Guid viewId, IReadOnlyCollection<Guid> scenarioIds, CancellationToken ct)
        {
            var tasks = _context.Tasks.Where(x => x.Scenario.ViewId == viewId && x.UserExecutable);
            if (scenarioIds != null)
                tasks = tasks.Where(x => scenarioIds.Contains(x.ScenarioId.Value));

            return _mapper.Map<IEnumerable<SAVM.Task>>(await tasks.ToListAsync(ct));
        }

        public async STT.Task<IEnumerable<SAVM.Task>> GetByUserIdAsync(Guid userId, CancellationToken ct)
        {
            // the user's personal scenario used in Task Builder is the user ID.
            var Tasks = _context.Tasks.Where(dt => dt.UserId == userId && dt.ScenarioTemplateId == null && (dt.ScenarioId == null || dt.ScenarioId == userId));

            return _mapper.Map<IEnumerable<SAVM.Task>>(Tasks);
        }

        public async STT.Task<IEnumerable<SAVM.Task>> GetByVmIdAsync(Guid vmId, CancellationToken ct)
        {
            var vmIdList = _context.Results.Where(r => r.VmId == vmId).Select(r => r.Id.ToString()).ToList();
            var Tasks = _context.Tasks.Where(dt => vmIdList.Contains(dt.Id.ToString()));

            return _mapper.Map<IEnumerable<SAVM.Task>>(Tasks);
        }

        public async STT.Task<IEnumerable<SAVM.Task>> GetSubtasksAsync(Guid triggerTaskId, CancellationToken ct)
        {
            var Tasks = _context.Tasks.Where(x => x.TriggerTaskId == triggerTaskId);

            return _mapper.Map<IEnumerable<SAVM.Task>>(Tasks);
        }

        public async STT.Task<SAVM.Task> CreateAsync(SAVM.TaskCreate taskForm, CancellationToken ct)
        {
            if ((taskForm.ScenarioId == null) == (taskForm.ScenarioTemplateId == null))
                throw new ForbiddenException("A Task must belong to exactly one Scenario or Scenario Template.");
            await ScopeValidation.EnsureTaskIsInParentAsync(taskForm.TriggerTaskId, taskForm.ScenarioId, taskForm.ScenarioTemplateId, _context, ct);

            var vmListCount = taskForm.VmList != null ? taskForm.VmList.Count : 0;
            if (vmListCount > 0)
            {
                if (taskForm.VmMask != "")
                    throw new InvalidOperationException("A Task cannot have BOTH a VmMask and a VmList!");
                // convert the list of vm guids into a comma separated string and save it in VmMask
                var vmIdString = "";
                foreach (var vmId in taskForm.VmList)
                {
                    vmIdString = vmIdString + vmId + ",";
                }
                taskForm.VmMask = vmIdString.Remove(vmIdString.Count() - 1);
            }
            TaskVmParameters.Reserve(taskForm.ActionParameters);

            var taskEntity = _mapper.Map<TaskEntity>(taskForm);
            taskEntity.DateCreated = DateTime.UtcNow;
            taskEntity.CreatedBy = _user.GetId();
            taskEntity.UserId = _user.GetId();
            taskEntity.Iterations = taskForm.Iterations > 0 ? taskForm.Iterations : 1;
            taskEntity.CurrentIteration = 0;

            _context.Tasks.Add(taskEntity);
            await _context.SaveChangesAsync(ct);
            var task = await GetAsync(taskEntity.Id, ct);

            return task;
        }

        public async STT.Task<IEnumerable<SAVM.Result>> CreateAndExecuteAsync(SAVM.TaskCreate taskForm, CancellationToken ct)
        {
            // create the TaskEntity
            var task = await CreateAsync(taskForm, ct);
            // execute the TaskEntity.  Authorization is null, because there can't be any subtasks on a CreateAndExecute.
            var resultList = await ExecuteAsync(task.Id, ct);

            return resultList;
        }

        public async STT.Task<IEnumerable<SAVM.Result>> ExecuteAsync(Guid id, CancellationToken ct)
        {
            var taskToExecute = await PrepareTaskToExecute(id, ct);
            _taskExecutionQueue.Add(taskToExecute);
            return new List<SAVM.Result>();
        }

        private async STT.Task<TaskEntity> PrepareTaskToExecute(Guid id, CancellationToken ct, int attempt = 0)
        {
            TaskEntity taskToExecute;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = _context;

                if (attempt > 0)
                {
                    // Get a new context to avoid retrieving stale data on subsequent attempts
                    dbContext = scope.ServiceProvider.GetRequiredService<SteamfitterContext>();
                }

                // Score calculations are idempotent, so READ COMMITTED is sufficient
                // and avoids serialization conflicts that cause request hangs
                await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

                taskToExecute = await dbContext.Tasks
                    .Include(x => x.Scenario)
                    .Include(x => x.Scenario)
                        .ThenInclude(x => x.Tasks)
                    .SingleOrDefaultAsync(x => x.Id == id, ct);

                if (!taskToExecute.Executable)
                {
                    throw new ForbiddenException("Task cannot be executed in it's current state");
                }

                taskToExecute.ResetTree(_user.GetId());
                taskToExecute.Status = TaskStatus.pending;
                taskToExecute.Scenario.CalculateScores();

                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                if (attempt < 10 && ex.IsTransientDatabaseException())
                {
                    var delay = Math.Min(1000, (int)Math.Pow(2, attempt) * 50);
                    var jitter = Random.Shared.Next(0, delay);
                    await STT.Task.Delay(delay + jitter, ct);
                    taskToExecute = await PrepareTaskToExecute(id, ct, attempt + 1);
                }
                else
                {
                    throw;
                }
            }

            return taskToExecute;
        }

        public async STT.Task<Guid?> ExecuteWithSubstitutionsAsync(Guid id, Dictionary<string, string> taskSubstitutions, CancellationToken ct)
        {
            EnsureNoReservedSubstitutions(taskSubstitutions);
            var taskToExecute = await PrepareTaskToExecute(id, ct);
            // make substitutions
            taskToExecute = MakeSubstitutions(taskToExecute, taskSubstitutions, ct);
            // add the task to the execution queue
            _taskExecutionQueue.Add(taskToExecute);

            return id;
        }

        public async STT.Task<Guid?> ExecuteForGradeAsync(GradedExecutionInfo gradedExecutionInfo, CancellationToken ct)
        {
            EnsureNoReservedSubstitutions(gradedExecutionInfo.TaskSubstitutions);
            var scenarioId = gradedExecutionInfo.ScenarioId;
            var scenario = await _context.Scenarios.FindAsync(scenarioId);
            // verify permissions and scenario to be graded
            if (scenario == null)
                throw new ApplicationException("No Scenario found for grading.");
            // get the start task to be executed
            var tasks = await _context.Tasks.Where(t =>
                t.ScenarioId == scenarioId &&
                t.TriggerCondition == TaskTrigger.Manual &&
                t.Name == gradedExecutionInfo.StartTaskName)
                .Select(x => x.Id)
                .ToListAsync();
            // verify the start task to execute exists and is unique
            if (tasks.Count() == 0)
                throw new ApplicationException("No Start Task found for grading.");
            if (tasks.Count() > 1)
                throw new ApplicationException("Multiple Start Tasks found for grading.");
            // get the start Task ID
            var startTaskId = tasks[0];
            // get the graded task to be executed
            tasks = await _context.Tasks.Where(t =>
                t.ScenarioId == scenarioId &&
                t.Name == gradedExecutionInfo.GradedTaskName)
                .Select(x => x.Id)
                .ToListAsync();
            // verify the graded task to execute exists and is unique
            if (tasks.Count() == 0)
                throw new ApplicationException("No Graded Task found for grading.");
            if (tasks.Count() > 1)
                throw new ApplicationException("Multiple Graded Tasks found for grading.");
            // get the graded Task ID
            var gradedTaskId = tasks[0];
            // prepare the task to execute
            var taskToExecute = await PrepareTaskToExecute(startTaskId, ct);
            // make substitutions
            taskToExecute = MakeSubstitutions(taskToExecute, gradedExecutionInfo.TaskSubstitutions, ct);
            // add the task to the execution queue
            _taskExecutionQueue.Add(taskToExecute);

            return gradedTaskId;
        }

        public async STT.Task<SAVM.Task> UpdateAsync(Guid id, SAVM.TaskEdit taskForm, CancellationToken ct)
        {
            var taskToUpdate = await _context.Tasks.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (taskToUpdate == null)
                throw new EntityNotFoundException<SAVM.Task>();

            var vmListCount = taskForm.VmList != null ? taskForm.VmList.Count : 0;
            if (vmListCount > 0)
            {
                if (taskForm.VmMask != "")
                    throw new InvalidOperationException("A Task cannot have BOTH a VmMask and a VmList!");
                // convert the list of vm guids into a comma separated string and save it in VmMask
                var vmIdString = "";
                foreach (var vmId in taskForm.VmList)
                {
                    vmIdString = vmIdString + vmId + ",";
                }
                taskForm.VmMask = vmIdString.Remove(vmIdString.Count() - 1);
            }
            TaskVmParameters.Reserve(taskForm.ActionParameters);

            _mapper.Map(taskForm, taskToUpdate);
            taskToUpdate.DateModified = DateTime.UtcNow;
            taskToUpdate.ModifiedBy = _user.GetId();
            await _context.SaveChangesAsync(ct);

            var updatedTask = _mapper.Map<SAVM.Task>(taskToUpdate);
            updatedTask.VmList = null;

            return updatedTask;
        }

        public async STT.Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            var taskToDelete = await _context.Tasks.SingleOrDefaultAsync(v => v.Id == id, ct);
            if (taskToDelete == null)
                throw new EntityNotFoundException<SAVM.Task>();

            var deletedTaskIds = new List<Guid> { id };
            deletedTaskIds.AddRange(await GetDescendantIdsInScopeAsync(id, taskToDelete.ScenarioId, taskToDelete.ScenarioTemplateId, ct));
            await DetachChildrenInOtherScopesAsync(deletedTaskIds, taskToDelete.ScenarioId, taskToDelete.ScenarioTemplateId, ct);
            _context.Tasks.Remove(taskToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async STT.Task<IEnumerable<SAVM.Task>> CopyAsync(Guid id, Guid newLocationId, string newLocationType, CancellationToken ct)
        {
            var items = await CopyTaskAsync(id, newLocationId, newLocationType, ct);

            return _mapper.Map<IEnumerable<SAVM.Task>>(items);
        }

        public async STT.Task<IEnumerable<SAVM.Task>> MoveAsync(Guid id, Guid newLocationId, string newLocationType, CancellationToken ct)
        {
            var items = await MoveTaskAsync(id, newLocationId, newLocationType, ct);

            return _mapper.Map<IEnumerable<SAVM.Task>>(items);
        }

        public async STT.Task<SAVM.Task> CreateFromResultAsync(Guid id, Guid newLocationId, string newLocationType, CancellationToken ct)
        {
            // check for existing result
            var resultEntity = await _context.Results.SingleAsync(v => v.Id == id, ct);
            if (resultEntity == null)
                throw new EntityNotFoundException<STT.Task>();
            // determine where the new Task goes
            Guid? triggerTaskId = null;
            Guid? scenarioTemplateId = null;
            Guid? scenarioId = null;
            switch (newLocationType)
            {
                case "task":
                    triggerTaskId = newLocationId;
                    var newLocationTaskEntity = await _context.Tasks.SingleAsync(v => v.Id == triggerTaskId, ct);
                    scenarioTemplateId = newLocationTaskEntity.ScenarioTemplateId;
                    scenarioId = newLocationTaskEntity.ScenarioId;
                    break;
                case "scenarioTemplate":
                    scenarioTemplateId = newLocationId;
                    break;
                case "scenario":
                    scenarioId = newLocationId;
                    break;
                default:
                    break;
            }
            // create the new Task
            var newTaskEntity = new TaskEntity();
            newTaskEntity.ScenarioTemplate = null;
            newTaskEntity.ScenarioTemplateId = scenarioTemplateId;
            newTaskEntity.Scenario = null;
            newTaskEntity.ScenarioId = scenarioId;
            newTaskEntity.TriggerTask = null;
            newTaskEntity.TriggerTaskId = triggerTaskId;
            newTaskEntity.TriggerCondition = TaskTrigger.Manual;
            newTaskEntity.Name = "New Task";
            newTaskEntity.Description = "Created from old execution";
            newTaskEntity.UserId = _user.GetId();
            newTaskEntity.Action = resultEntity.Action;
            newTaskEntity.VmMask = resultEntity.VmId.ToString();
            newTaskEntity.ApiUrl = resultEntity.ApiUrl;
            newTaskEntity.InputString = resultEntity.InputString;
            newTaskEntity.ExpectedOutput = resultEntity.ExpectedOutput;
            newTaskEntity.ExpirationSeconds = resultEntity.ExpirationSeconds;
            newTaskEntity.DelaySeconds = 0;
            newTaskEntity.IntervalSeconds = 0;
            newTaskEntity.Iterations = 1;
            newTaskEntity.IterationTermination = TaskIterationTermination.IterationCount;
            newTaskEntity.CurrentIteration = 0;
            newTaskEntity.CreatedBy = _user.GetId();
            newTaskEntity.ModifiedBy = _user.GetId();
            newTaskEntity.DateCreated = DateTime.UtcNow;
            newTaskEntity.DateModified = newTaskEntity.DateCreated;
            // save new task to the database
            _context.Tasks.Add(newTaskEntity);
            await _context.SaveChangesAsync();
            var newTask = _mapper.Map<SAVM.Task>(newTaskEntity);

            return _mapper.Map<SAVM.Task>(newTask);
        }

        private async STT.Task<IEnumerable<TaskEntity>> CopyTaskAsync(Guid id, Guid newLocationId, string newLocationType, CancellationToken ct)
        {
            // check for existing task
            var existingTaskEntity = await _context.Tasks.SingleAsync(v => v.Id == id, ct);
            if (existingTaskEntity == null)
                throw new EntityNotFoundException<STT.Task>();
            // determine where the copy goes
            Guid? triggerTaskId = null;
            Guid? scenarioTemplateId = null;
            Guid? scenarioId = null;
            switch (newLocationType)
            {
                case "task":
                    triggerTaskId = newLocationId;
                    var newLocationTaskEntity = await _context.Tasks.SingleAsync(v => v.Id == triggerTaskId, ct);
                    if (await PreventAddingToSelfAsync(existingTaskEntity.Id, newLocationTaskEntity, ct))
                    {
                        throw new Exception("Cannot copy a Task underneath itself!");
                    }
                    scenarioTemplateId = newLocationTaskEntity.ScenarioTemplateId;
                    scenarioId = newLocationTaskEntity.ScenarioId;
                    break;
                case "scenarioTemplate":
                    scenarioTemplateId = newLocationId;
                    break;
                case "scenario":
                    scenarioId = newLocationId;
                    break;
                default:
                    break;
            }
            // create the copy
            var newTaskEntity = _mapper.Map<TaskEntity, TaskEntity>(existingTaskEntity);
            // set the new task relationships
            newTaskEntity.ScenarioTemplate = null;
            newTaskEntity.ScenarioTemplateId = scenarioTemplateId;
            newTaskEntity.Scenario = null;
            newTaskEntity.ScenarioId = scenarioId;
            newTaskEntity.TriggerTask = null;
            newTaskEntity.TriggerTaskId = triggerTaskId;
            newTaskEntity.UserId = _user.GetId();
            newTaskEntity.CreatedBy = _user.GetId();
            newTaskEntity.ModifiedBy = _user.GetId();
            newTaskEntity.DateCreated = DateTime.UtcNow;
            newTaskEntity.DateModified = newTaskEntity.DateCreated;
            newTaskEntity.Status = TaskStatus.none;
            // save new task to the database
            _context.Tasks.Add(newTaskEntity);
            await _context.SaveChangesAsync();
            var newTask = _mapper.Map<SAVM.Task>(newTaskEntity);
            // return the new task with all of its new subtasks
            var entities = new List<TaskEntity>();
            entities.Add(newTaskEntity);
            entities.AddRange(await CopySubTasks(existingTaskEntity, newTaskEntity.Id, ct));

            return entities;
        }

        private async STT.Task<IEnumerable<TaskEntity>> CopySubTasks(TaskEntity oldTaskEntity, Guid newTaskEntityId, CancellationToken ct)
        {
            var oldSubTaskEntityIds = await ChildrenInScope([oldTaskEntity.Id], oldTaskEntity.ScenarioId, oldTaskEntity.ScenarioTemplateId)
                .Select(dt => dt.Id)
                .ToListAsync(ct);
            var subEntities = new List<TaskEntity>();
            foreach (var oldSubTaskEntityId in oldSubTaskEntityIds)
            {
                var newEntities = await CopyTaskAsync(oldSubTaskEntityId, newTaskEntityId, "task", ct);
                subEntities.AddRange(newEntities);
            }

            return subEntities;
        }

        private async STT.Task<IEnumerable<TaskEntity>> MoveTaskAsync(Guid id, Guid newLocationId, string newLocationType, CancellationToken ct)
        {
            // check for existing task
            var existingTaskEntity = await _context.Tasks.SingleAsync(v => v.Id == id, ct);
            if (existingTaskEntity == null)
                throw new EntityNotFoundException<SAVM.Task>();
            var sourceScenarioId = existingTaskEntity.ScenarioId;
            var sourceScenarioTemplateId = existingTaskEntity.ScenarioTemplateId;
            // determine where the task goes
            existingTaskEntity.TriggerTaskId = null;
            existingTaskEntity.ScenarioTemplateId = null;
            existingTaskEntity.ScenarioId = null;
            switch (newLocationType)
            {
                case "task":
                    var newLocationTaskEntity = await _context.Tasks.SingleAsync(v => v.Id == newLocationId, ct);
                    if (await PreventAddingToSelfAsync(existingTaskEntity.Id, newLocationTaskEntity, ct))
                    {
                        throw new Exception("Cannot move a Task underneath itself!");
                    }
                    existingTaskEntity.TriggerTaskId = newLocationId;
                    existingTaskEntity.ScenarioTemplateId = newLocationTaskEntity.ScenarioTemplateId;
                    existingTaskEntity.ScenarioId = newLocationTaskEntity.ScenarioId;
                    break;
                case "scenarioTemplate":
                    existingTaskEntity.ScenarioTemplateId = newLocationId;
                    break;
                case "scenario":
                    existingTaskEntity.ScenarioId = newLocationId;
                    break;
                default:
                    break;
            }

            await _context.SaveChangesAsync();
            var movedTask = _mapper.Map<SAVM.Task>(existingTaskEntity);
            var entities = new List<TaskEntity>();
            entities.Add(existingTaskEntity);
            entities.AddRange(await MoveSubTasks(existingTaskEntity.Id, sourceScenarioId, sourceScenarioTemplateId, ct));

            return entities;
        }

        private async STT.Task<IEnumerable<TaskEntity>> MoveSubTasks(Guid taskEntityId, Guid? sourceScenarioId, Guid? sourceScenarioTemplateId, CancellationToken ct)
        {
            // only the subtasks that were in the moved task's Scenario or Scenario Template move with it
            var subTaskEntityIds = await ChildrenInScope([taskEntityId], sourceScenarioId, sourceScenarioTemplateId)
                .Select(dt => dt.Id)
                .ToListAsync(ct);
            var subEntities = new List<TaskEntity>();
            foreach (var subTaskEntityId in subTaskEntityIds)
            {
                var newEntities = await MoveTaskAsync(subTaskEntityId, taskEntityId, "task", ct);
                subEntities.AddRange(newEntities);
            }

            return subEntities;
        }

        /// <summary>
        /// The trigger relationship cascades on delete. A child in another Scenario or Scenario Template
        /// must not be deleted with its trigger, so it is detached first. It is made manual, so that being
        /// a top-level Task does not start it with its Scenario.
        /// </summary>
        public async STT.Task DetachChildrenInOtherScopesAsync(IReadOnlyCollection<Guid> taskIds, Guid? scenarioId, Guid? scenarioTemplateId, CancellationToken ct)
        {
            var children = await _context.Tasks
                .Where(t => t.TriggerTaskId.HasValue && taskIds.Contains(t.TriggerTaskId.Value)
                    && (t.ScenarioId != scenarioId || t.ScenarioTemplateId != scenarioTemplateId))
                .ToListAsync(ct);
            foreach (var child in children)
            {
                child.TriggerTaskId = null;
                child.TriggerCondition = TaskTrigger.Manual;
            }
            await _context.SaveChangesAsync(ct);
        }

        private async STT.Task<List<Guid>> GetDescendantIdsInScopeAsync(Guid taskId, Guid? scenarioId, Guid? scenarioTemplateId, CancellationToken ct)
        {
            var descendantIds = new List<Guid>();
            var parentIds = new List<Guid> { taskId };
            while (parentIds.Any())
            {
                parentIds = await ChildrenInScope(parentIds, scenarioId, scenarioTemplateId)
                    .Select(t => t.Id)
                    .Where(x => !descendantIds.Contains(x))
                    .ToListAsync(ct);
                descendantIds.AddRange(parentIds);
            }
            return descendantIds;
        }

        // the children of the given tasks that share their Scenario or Scenario Template
        private IQueryable<TaskEntity> ChildrenInScope(IReadOnlyCollection<Guid> parentIds, Guid? scenarioId, Guid? scenarioTemplateId) =>
            _context.Tasks.Where(t => t.TriggerTaskId.HasValue && parentIds.Contains(t.TriggerTaskId.Value)
                && t.ScenarioId == scenarioId && t.ScenarioTemplateId == scenarioTemplateId);

        private async STT.Task<bool> PreventAddingToSelfAsync(Guid existingId, TaskEntity newLocationEntity, CancellationToken ct)
        {
            // make sure the existing task is not the same as the new location task
            var wouldAddToSelf = (newLocationEntity.Id == existingId);
            // walk up the dispatch task family tree to make sure the existing task is not on it
            // a null parentId means we hit the top
            var parentId = newLocationEntity.TriggerTaskId;
            while (!wouldAddToSelf && parentId != null)
            {
                wouldAddToSelf = (parentId == existingId);
                parentId = (await _context.Tasks.SingleAsync(v => v.Id == parentId, ct)).TriggerTaskId;
            }
            return wouldAddToSelf;
        }

        // The task runner fills these markers from the VM it bound the result to. A caller who could
        // substitute them would choose the VM the action runs on, outside the scenario's view.
        private static readonly string[] ReservedSubstitutionKeys = ["moid", "VmName"];

        private static void EnsureNoReservedSubstitutions(Dictionary<string, string> substitutions)
        {
            if (substitutions != null && substitutions.Keys.Any(key => ReservedSubstitutionKeys.Contains(key, StringComparer.OrdinalIgnoreCase)))
                throw new ForbiddenException("The VM a Task runs on cannot be substituted.");
        }

        private TaskEntity MakeSubstitutions(TaskEntity taskToExecute, Dictionary<string, string> substitutions, CancellationToken ct)
        {
            var actionParameters = JsonSerializer.Deserialize<Dictionary<string, string>>(taskToExecute.InputString);
            var modifications = new Dictionary<string, string>();
            foreach (var substitution in substitutions)
            {
                foreach (var actionParameter in actionParameters)
                {
                    var replacementMarker = "{" + substitution.Key + "}";
                    if (actionParameter.Value.Contains(replacementMarker))
                    {
                        modifications[actionParameter.Key] = actionParameter.Value.Replace(replacementMarker, substitution.Value);
                    }
                }
            }
            foreach (var modification in modifications)
            {
                actionParameters[modification.Key] = modification.Value;
            }
            taskToExecute.InputString = JsonSerializer.Serialize(actionParameters);

            return taskToExecute;
        }

    }
    public class GradedExecutionInfo
    {
        public Guid ScenarioId { get; set; }
        public string StartTaskName { get; set; }
        public string GradedTaskName { get; set; }
        public Dictionary<string, string> TaskSubstitutions { get; set; }
    }

    public class NewLocation
    {
        public Guid Id { get; set; }
        public string LocationType { get; set; }
    }

}
