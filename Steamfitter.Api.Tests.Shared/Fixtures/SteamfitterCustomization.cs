// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoFixture;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using SteamfitterTaskStatus = Steamfitter.Api.Data.TaskStatus;

namespace Steamfitter.Api.Tests.Shared.Fixtures;

/// <summary>
/// AutoFixture customization that registers factories for all Steamfitter entity types,
/// avoiding circular reference issues from EF navigation properties.
/// </summary>
public class SteamfitterCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        // Prevent infinite recursion from navigation properties
        fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => fixture.Behaviors.Remove(b));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        fixture.Customize<ScenarioEntity>(c => c
            .Without(x => x.Tasks)
            .Without(x => x.ScenarioTemplate)
            .Without(x => x.VmCredentials)
            .Without(x => x.Memberships)
            .With(x => x.Id, () => Guid.NewGuid())
            .With(x => x.Status, ScenarioStatus.ready)
            .With(x => x.StartDate, () => DateTime.UtcNow.AddDays(1))
            .With(x => x.EndDate, () => DateTime.UtcNow.AddDays(31))
            .With(x => x.CreatedBy, () => Guid.NewGuid()));

        fixture.Customize<ScenarioTemplateEntity>(c => c
            .Without(x => x.Tasks)
            .Without(x => x.Scenarios)
            .Without(x => x.VmCredentials)
            .Without(x => x.Memberships)
            .With(x => x.Id, () => Guid.NewGuid())
            .With(x => x.CreatedBy, () => Guid.NewGuid()));

        fixture.Customize<TaskEntity>(c => c
            .Without(x => x.ScenarioTemplate)
            .Without(x => x.Scenario)
            .Without(x => x.TriggerTask)
            .Without(x => x.Children)
            .Without(x => x.Results)
            .With(x => x.Id, () => Guid.NewGuid())
            .With(x => x.Action, TaskAction.guest_process_run)
            .With(x => x.Status, SteamfitterTaskStatus.pending)
            .With(x => x.TotalStatus, SteamfitterTaskStatus.pending)
            .With(x => x.TriggerCondition, TaskTrigger.Time)
            .With(x => x.IterationTermination, TaskIterationTermination.IterationCount)
            .With(x => x.Iterations, 1)
            .With(x => x.CreatedBy, () => Guid.NewGuid()));

        fixture.Customize<ResultEntity>(c => c
            .Without(x => x.Task)
            .With(x => x.Id, () => Guid.NewGuid())
            .With(x => x.Action, TaskAction.guest_process_run)
            .With(x => x.Status, SteamfitterTaskStatus.pending)
            .With(x => x.SentDate, () => DateTime.UtcNow)
            .With(x => x.StatusDate, () => DateTime.UtcNow)
            .With(x => x.CreatedBy, () => Guid.NewGuid()));

        fixture.Customize<UserEntity>(c => c
            .Without(x => x.Role)
            .Without(x => x.ScenarioMemberships)
            .Without(x => x.ScenarioTemplateMemberships)
            .Without(x => x.GroupMemberships)
            .With(x => x.Id, () => Guid.NewGuid()));

        fixture.Customize<PermissionEntity>(c => c
            .Without(x => x.UserPermissions)
            .With(x => x.Id, () => Guid.NewGuid())
            .With(x => x.CreatedBy, () => Guid.NewGuid()));

        fixture.Customize<UserPermissionEntity>(c => c
            .Without(x => x.User)
            .Without(x => x.Permission)
            .With(x => x.Id, () => Guid.NewGuid())
            .With(x => x.UserId, () => Guid.NewGuid())
            .With(x => x.PermissionId, () => Guid.NewGuid()));

        fixture.Customize<VmCredentialEntity>(c => c
            .Without(x => x.ScenarioTemplate)
            .Without(x => x.Scenario)
            .With(x => x.Id, () => Guid.NewGuid())
            .With(x => x.CreatedBy, () => Guid.NewGuid()));

        fixture.Customize<FileEntity>(c => c
            .With(x => x.Id, () => Guid.NewGuid())
            .With(x => x.CreatedBy, () => Guid.NewGuid()));

        fixture.Customize<SystemRoleEntity>(c => c
            .With(x => x.Id, () => Guid.NewGuid())
            .With(x => x.Permissions, () => new List<SystemPermission>()));

        fixture.Customize<ScenarioRoleEntity>(c => c
            .With(x => x.Id, () => Guid.NewGuid())
            .With(x => x.Permissions, () => new List<ScenarioPermission>()));

        fixture.Customize<ScenarioTemplateRoleEntity>(c => c
            .With(x => x.Id, () => Guid.NewGuid())
            .With(x => x.Permissions, () => new List<ScenarioTemplatePermission>()));

        fixture.Customize<ScenarioMembershipEntity>(c => c
            .Without(x => x.Scenario)
            .Without(x => x.User)
            .Without(x => x.Group)
            .Without(x => x.Role)
            .With(x => x.Id, () => Guid.NewGuid())
            .With(x => x.ScenarioId, () => Guid.NewGuid())
            .With(x => x.RoleId, () => ScenarioRoleDefaults.ScenarioMemberRoleId));

        fixture.Customize<ScenarioTemplateMembershipEntity>(c => c
            .Without(x => x.ScenarioTemplate)
            .Without(x => x.User)
            .Without(x => x.Group)
            .Without(x => x.Role)
            .With(x => x.Id, () => Guid.NewGuid())
            .With(x => x.ScenarioTemplateId, () => Guid.NewGuid())
            .With(x => x.RoleId, () => ScenarioTemplateRoleEntityDefaults.ScenarioTemplateMemberRoleId));

        fixture.Customize<GroupEntity>(c => c
            .Without(x => x.Memberships)
            .Without(x => x.ScenarioTemplateMemberships)
            .Without(x => x.ScenarioMemberships)
            .With(x => x.Id, () => Guid.NewGuid()));

        fixture.Customize<GroupMembershipEntity>(c => c
            .Without(x => x.Group)
            .Without(x => x.User)
            .With(x => x.Id, () => Guid.NewGuid())
            .With(x => x.GroupId, () => Guid.NewGuid())
            .With(x => x.UserId, () => Guid.NewGuid()));

        fixture.Customize<UserScenarioEntity>(c => c
            .Without(x => x.User)
            .Without(x => x.Scenario)
            .With(x => x.Id, () => Guid.NewGuid())
            .With(x => x.UserId, () => Guid.NewGuid())
            .With(x => x.ScenarioId, () => Guid.NewGuid()));

        fixture.Customize<XApiQueuedStatementEntity>(c => c
            .With(x => x.Id, () => Guid.NewGuid())
            .With(x => x.Status, XApiQueueStatus.Pending)
            .With(x => x.QueuedAt, () => DateTime.UtcNow)
            .With(x => x.CreatedBy, () => Guid.NewGuid()));
    }
}
