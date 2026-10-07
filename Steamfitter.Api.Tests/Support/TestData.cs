// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// Steamfitter's seeded rows come from HasData in Steamfitter.Api.Data/Models (SystemRole.cs,
// ScenarioRole.cs, ScenarioTemplateRole.cs), so the migrations put them in the template database.

using System;
using System.Collections.Generic;
using System.Linq;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;

namespace Steamfitter.Api.Tests.Support;

/// <summary>Object mothers, and the ids of the rows the migrations seed.</summary>
public static class TestData
{
    /// <summary>Ids of the system roles the migrations seed (<c>SystemRoleEntityDefaults</c>).</summary>
    public static class Roles
    {
        /// <summary>AllPermissions, immutable.</summary>
        public static readonly Guid Administrator = SystemRoleEntityDefaults.AdministratorRoleId;

        /// <summary>CreateScenarioTemplates, CreateScenarios, ExecuteScenarios.</summary>
        public static readonly Guid ContentDeveloper = SystemRoleEntityDefaults.ContentDeveloperRoleId;

        /// <summary>Every View* system permission.</summary>
        public static readonly Guid Observer = SystemRoleEntityDefaults.ObserverRoleId;
    }

    /// <summary>Ids of the scenario roles the migrations seed (<c>ScenarioRoleDefaults</c>).</summary>
    public static class ScenarioRoles
    {
        /// <summary>AllPermissions; given to a scenario's creator.</summary>
        public static readonly Guid Manager = ScenarioRoleDefaults.ScenarioCreatorRoleId;

        /// <summary>ViewScenario.</summary>
        public static readonly Guid Observer = ScenarioRoleDefaults.ScenarioReadOnlyRoleId;

        /// <summary>ViewScenario, EditScenario; the default of a new membership.</summary>
        public static readonly Guid Member = ScenarioRoleDefaults.ScenarioMemberRoleId;

        /// <summary>ViewScenario, ExecuteScenario.</summary>
        public static readonly Guid Facilitator = ScenarioRoleDefaults.ScenarioFacilitatorRoleId;
    }

    /// <summary>Ids of the scenario template roles the migrations seed.</summary>
    public static class ScenarioTemplateRoles
    {
        /// <summary>AllPermissions; given to a template's creator.</summary>
        public static readonly Guid Manager = ScenarioTemplateRoleEntityDefaults.ScenarioTemplateCreatorRoleId;

        /// <summary>ViewScenarioTemplate.</summary>
        public static readonly Guid Observer = ScenarioTemplateRoleEntityDefaults.ScenarioTemplateReadOnlyRoleId;

        /// <summary>ViewScenarioTemplate, EditScenarioTemplate; the default of a new membership.</summary>
        public static readonly Guid Member = ScenarioTemplateRoleEntityDefaults.ScenarioTemplateMemberRoleId;
    }

    public static UserEntity User(Guid? id = null, string name = "Test User", Guid? roleId = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            RoleId = roleId
        };

    /// <summary>A system role of its own, named uniquely because role names are uniquely indexed.</summary>
    public static SystemRoleEntity SystemRole(bool allPermissions = false, SystemPermission[] permissions = null)
    {
        var id = Guid.NewGuid();

        return SystemRole($"role-{id:N}", allPermissions, permissions, id);
    }

    /// <summary>A system role with the name given: <c>system_roles.name</c> is uniquely indexed.</summary>
    public static SystemRoleEntity SystemRole(string name, bool allPermissions = false, SystemPermission[] permissions = null, Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            AllPermissions = allPermissions,
            Permissions = [.. permissions ?? []]
        };

    /// <summary>A scenario role granting exactly <paramref name="permissions"/>.</summary>
    public static ScenarioRoleEntity ScenarioRole(params ScenarioPermission[] permissions)
    {
        var id = Guid.NewGuid();

        return new ScenarioRoleEntity
        {
            Id = id,
            Name = $"scenario-role-{id:N}",
            AllPermissions = false,
            Permissions = [.. permissions]
        };
    }

    /// <summary>A scenario template role granting exactly <paramref name="permissions"/>.</summary>
    public static ScenarioTemplateRoleEntity ScenarioTemplateRole(params ScenarioTemplatePermission[] permissions)
    {
        var id = Guid.NewGuid();

        return new ScenarioTemplateRoleEntity
        {
            Id = id,
            Name = $"scenario-template-role-{id:N}",
            AllPermissions = false,
            Permissions = [.. permissions]
        };
    }

    public static ScenarioTemplateEntity ScenarioTemplate(string name = "Template", Guid? createdBy = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "A scenario template",
            CreatedBy = createdBy ?? Guid.Empty
        };

    public static ScenarioEntity Scenario(
        string name = "Scenario",
        Guid? viewId = null,
        Guid? scenarioTemplateId = null,
        ScenarioStatus status = ScenarioStatus.ready,
        Guid? createdBy = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "A scenario",
            ViewId = viewId,
            ScenarioTemplateId = scenarioTemplateId,
            Status = status,
            CreatedBy = createdBy ?? Guid.Empty
        };

    /// <summary>
    /// A manual email task in a scenario, with no VMs: the shape <c>TaskExecutionService</c> runs without
    /// reaching the Player VM API.
    /// </summary>
    public static TaskEntity ScenarioTask(Guid scenarioId, string name = "Task", Guid? triggerTaskId = null) =>
        Task(name, scenarioId: scenarioId, triggerTaskId: triggerTaskId);

    /// <summary>The same task shape in a scenario template.</summary>
    public static TaskEntity TemplateTask(Guid scenarioTemplateId, string name = "Template Task", Guid? triggerTaskId = null) =>
        Task(name, scenarioTemplateId: scenarioTemplateId, triggerTaskId: triggerTaskId);

    private static TaskEntity Task(string name, Guid? scenarioId = null, Guid? scenarioTemplateId = null, Guid? triggerTaskId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "A task",
            ScenarioId = scenarioId,
            ScenarioTemplateId = scenarioTemplateId,
            TriggerTaskId = triggerTaskId,
            TriggerCondition = triggerTaskId is null ? TaskTrigger.Manual : TaskTrigger.Success,
            Action = TaskAction.send_email,
            ApiUrl = "email",
            VmMask = "",
            InputString = "{}",
            ExpectedOutput = "",
            Iterations = 1,
            Status = TaskStatus.none,
            TotalStatus = TaskStatus.none
        };

    public static ResultEntity Result(Guid? taskId, Guid? vmId = null, TaskStatus status = TaskStatus.succeeded, Guid? createdBy = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            VmId = vmId,
            VmName = "vm",
            ApiUrl = "email",
            Action = TaskAction.send_email,
            InputString = "{}",
            Iterations = 1,
            CurrentIteration = 1,
            Status = status,
            ExpectedOutput = "",
            ActualOutput = "",
            SentDate = DefaultDateCreated,
            StatusDate = DefaultDateCreated,
            CreatedBy = createdBy ?? Guid.Empty
        };

    public static VmCredentialEntity VmCredential(Guid? scenarioId = null, Guid? scenarioTemplateId = null, string username = "user") =>
        new()
        {
            Id = Guid.NewGuid(),
            ScenarioId = scenarioId,
            ScenarioTemplateId = scenarioTemplateId,
            Username = username,
            Password = "secret",
            Description = "A credential"
        };

    public static GroupEntity Group(string name = "Group") =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "A group"
        };

    public static GroupMembershipEntity GroupMembership(Guid groupId, Guid userId) =>
        new()
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            UserId = userId
        };

    public static ScenarioMembershipEntity ScenarioMembership(Guid scenarioId, Guid? userId = null, Guid? groupId = null, Guid? roleId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            ScenarioId = scenarioId,
            UserId = userId,
            GroupId = groupId,
            RoleId = roleId ?? ScenarioRoles.Member
        };

    public static ScenarioTemplateMembershipEntity ScenarioTemplateMembership(Guid scenarioTemplateId, Guid? userId = null, Guid? groupId = null, Guid? roleId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            ScenarioTemplateId = scenarioTemplateId,
            UserId = userId,
            GroupId = groupId,
            RoleId = roleId ?? ScenarioTemplateRoles.Member
        };

    /// <summary>A fixed creation timestamp. Tests that care about ordering pass their own.</summary>
    public static readonly DateTime DefaultDateCreated = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Every system permission except <paramref name="excluded"/>, for the near miss that holds everything else.</summary>
    public static SystemPermission[] SystemPermissionsExcept(params SystemPermission[] excluded) =>
        [.. Enum.GetValues<SystemPermission>().Where(x => !excluded.Contains(x))];

    /// <summary>The action parameters a task form carries, as a dictionary the API accepts.</summary>
    public static Dictionary<string, string> NoActionParameters() => [];
}
