// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Steamfitter.Api.Data;
using System;
using System.Collections.Generic;

namespace Steamfitter.Api.ViewModels
{
    public class Task : Base, IAuthorizationType
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid? ScenarioTemplateId { get; set; }
        public Guid? ScenarioId { get; set; }
        public Guid? UserId { get; set; }
        public TaskAction Action { get; set; }
        public string VmMask { get; set; }
        public List<Guid> VmList { get; set; }
        public string ApiUrl { get; set; }
        public Dictionary<string, string> ActionParameters { get; set; }
        public string ExpectedOutput { get; set; }
        public int ExpirationSeconds { get; set; }
        public int DelaySeconds { get; set; }
        public int IntervalSeconds { get; set; }
        public int Iterations { get; set; }
        public TaskIterationTermination IterationTermination { get; set; }
        public int CurrentIteration { get; set; }
        public Guid? TriggerTaskId { get; set; }
        public TaskTrigger TriggerCondition { get; set; }
        public int Score { get; set; }
        public TaskStatus Status { get; set; }
        public int ScoreEarned { get; set; }
        public bool UserExecutable { get; set; }
        public int TotalScore { get; set; }
        public int TotalScoreEarned { get; set; }
        public TaskStatus TotalStatus { get; set; }
        public bool Repeatable { get; set; }
        public bool Executable { get; set; }
    }

    public class TaskSummary : Base
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Score { get; set; }
        public int ScoreEarned { get; set; }
        public TaskStatus Status { get; set; }
        public string Description { get; set; }
        public Guid? ScenarioId { get; set; }
        public bool UserExecutable { get; set; }
        public int TotalScore { get; set; }
        public int TotalScoreEarned { get; set; }
        public TaskStatus TotalStatus { get; set; }
        public bool Repeatable { get; set; }
        public bool Executable { get; set; }
    }

    public class TaskForm
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid? ScenarioTemplateId { get; set; }
        public Guid? ScenarioId { get; set; }
        public Guid? UserId { get; set; }
        public TaskAction Action { get; set; }
        public string VmMask { get; set; }
        public List<Guid> VmList { get; set; }
        public string ApiUrl { get; set; }
        public Dictionary<string, string> ActionParameters { get; set; }
        public string ExpectedOutput { get; set; }
        public int ExpirationSeconds { get; set; }
        public int DelaySeconds { get; set; }
        public int IntervalSeconds { get; set; }
        public int Iterations { get; set; }
        public TaskIterationTermination IterationTermination { get; set; }
        public int CurrentIteration { get; set; }
        public Guid? TriggerTaskId { get; set; }
        public TaskTrigger TriggerCondition { get; set; }
        public int Score { get; set; }
        public bool UserExecutable { get; set; }
        public bool Repeatable { get; set; }
        public bool Executable { get; set; }
    }
}
