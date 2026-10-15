// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// Mirrors UserClaimsService.GetPermissionClaims: User.RoleId -> SystemRole { AllPermissions,
// Permissions } for system permissions; ScenarioMemberships and ScenarioTemplateMemberships (of the user,
// or of a group the user is in through GroupMemberships) -> their role's AllPermissions or Permissions
// for the per-resource claims. A scenario or template grants nothing of its own, so OnNewScenario and
// OnNewScenarioTemplate exist for the "same permission on another resource" near miss.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;

namespace Steamfitter.Api.Tests.Support;

/// <summary>A seeded user, and the ids of the rows seeded with them.</summary>
public sealed class TestActor
{
    public required Guid Id { get; init; }

    /// <summary>Sent as the <c>name</c> claim, which the claims service writes back to the user row.</summary>
    public required string Name { get; init; }

    /// <summary>One per <c>OnScenario</c> or <c>OnNewScenario</c> call, in call order.</summary>
    public required IReadOnlyList<ActorMembership> ScenarioMemberships { get; init; }

    /// <summary>One per <c>OnScenarioTemplate</c> or <c>OnNewScenarioTemplate</c> call, in call order.</summary>
    public required IReadOnlyList<ActorMembership> ScenarioTemplateMemberships { get; init; }
}

/// <summary>A membership row seeded for an actor: the resource (minted by <c>OnNew...</c> or named), the role.</summary>
public sealed record ActorMembership(Guid Id, Guid ResourceId, Guid RoleId);

/// <summary>
/// Seeds a user, the role that grants their system permissions, and their memberships, so that the real
/// claims transformer derives the permissions a test needs.
/// </summary>
public sealed class TestActorBuilder(SteamfitterContext db, CancellationToken ct)
{
    private Guid _id = Guid.NewGuid();
    private string _name = "Test Actor";
    private Guid? _roleId;
    private SystemPermission[] _systemPermissions;
    private readonly List<PendingMembership<ScenarioPermission>> _scenarios = [];
    private readonly List<PendingMembership<ScenarioTemplatePermission>> _templates = [];
    private readonly List<Guid> _groups = [];

    /// <summary>Fixes the actor's id, for a test that needs to know it before seeding.</summary>
    public TestActorBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public TestActorBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>Gives the actor an existing system role, such as <c>TestData.Roles.Administrator</c>.</summary>
    public TestActorBuilder WithRole(Guid roleId)
    {
        if (_systemPermissions is not null)
        {
            throw new InvalidOperationException(
                "WithRole and WithSystemPermissions both decide the actor's system role. Drop one.");
        }

        _roleId = roleId;
        return this;
    }

    /// <summary>Every system permission, by way of the seeded administrator role.</summary>
    public TestActorBuilder WithAllSystemPermissions() => WithRole(TestData.Roles.Administrator);

    /// <summary>Exactly these system permissions, by way of a role minted for this actor.</summary>
    public TestActorBuilder WithSystemPermissions(params SystemPermission[] permissions)
    {
        if (_roleId is not null)
        {
            throw new InvalidOperationException(
                "WithSystemPermissions and WithRole both decide the actor's system role. Drop one.");
        }

        _systemPermissions = permissions;
        return this;
    }

    /// <summary>
    /// A membership on a saved scenario, with a seeded role (<c>TestData.ScenarioRoles</c>) or exactly
    /// <paramref name="permissions"/> through a role minted for it. One of the two is required: the
    /// column default, <c>Member</c>, grants ViewScenario and EditScenario, which a near miss must not
    /// hold by accident.
    /// </summary>
    public TestActorBuilder OnScenario(Guid scenarioId, Guid? roleId = null, params ScenarioPermission[] permissions)
    {
        _scenarios.Add(new(scenarioId, Pick(roleId, permissions, nameof(OnScenario)), permissions));
        return this;
    }

    /// <summary>
    /// Puts the actor on a new scenario, minted in <see cref="SeedAsync"/>, with exactly
    /// <paramref name="permissions"/>: the near miss that holds the right permission on another scenario.
    /// The scenario's id is on <c>ScenarioMemberships[i].ResourceId</c>.
    /// </summary>
    public TestActorBuilder OnNewScenario(params ScenarioPermission[] permissions)
    {
        _scenarios.Add(new(null, null, permissions));
        return this;
    }

    /// <summary>A membership on a saved scenario template; see <see cref="OnScenario"/>.</summary>
    public TestActorBuilder OnScenarioTemplate(Guid scenarioTemplateId, Guid? roleId = null, params ScenarioTemplatePermission[] permissions)
    {
        _templates.Add(new(scenarioTemplateId, Pick(roleId, permissions, nameof(OnScenarioTemplate)), permissions));
        return this;
    }

    /// <summary>A membership on a new scenario template minted in <see cref="SeedAsync"/>; see <see cref="OnNewScenario"/>.</summary>
    public TestActorBuilder OnNewScenarioTemplate(params ScenarioTemplatePermission[] permissions)
    {
        _templates.Add(new(null, null, permissions));
        return this;
    }

    /// <summary>Makes the actor a member of a saved group, whose scenario and template memberships then grant.</summary>
    public TestActorBuilder InGroup(Guid groupId)
    {
        _groups.Add(groupId);
        return this;
    }

    /// <summary>Writes the actor and everything above to the database.</summary>
    public async Task<TestActor> SeedAsync()
    {
        var roleId = _roleId;

        if (_systemPermissions is not null)
        {
            var role = TestData.SystemRole(permissions: _systemPermissions);
            db.SystemRoles.Add(role);
            roleId = role.Id;
        }

        db.Users.Add(TestData.User(_id, _name, roleId));

        foreach (var groupId in _groups)
        {
            db.GroupMemberships.Add(TestData.GroupMembership(groupId, _id));
        }

        List<ActorMembership> scenarios = [];

        foreach (var pending in _scenarios)
        {
            var scenarioId = pending.ResourceId ?? MintScenario();
            var membershipRoleId = pending.RoleId ?? MintScenarioRole(pending.Permissions);
            var membership = TestData.ScenarioMembership(scenarioId, _id, roleId: membershipRoleId);
            db.ScenarioMemberships.Add(membership);
            scenarios.Add(new(membership.Id, scenarioId, membershipRoleId));
        }

        List<ActorMembership> templates = [];

        foreach (var pending in _templates)
        {
            var templateId = pending.ResourceId ?? MintScenarioTemplate();
            var membershipRoleId = pending.RoleId ?? MintScenarioTemplateRole(pending.Permissions);
            var membership = TestData.ScenarioTemplateMembership(templateId, _id, roleId: membershipRoleId);
            db.ScenarioTemplateMemberships.Add(membership);
            templates.Add(new(membership.Id, templateId, membershipRoleId));
        }

        await db.SaveChangesAsync(ct);

        return new TestActor
        {
            Id = _id,
            Name = _name,
            ScenarioMemberships = scenarios,
            ScenarioTemplateMemberships = templates
        };
    }

    private Guid MintScenario()
    {
        var scenario = TestData.Scenario($"{_name}'s other scenario");
        db.Scenarios.Add(scenario);
        return scenario.Id;
    }

    private Guid MintScenarioTemplate()
    {
        var template = TestData.ScenarioTemplate($"{_name}'s other template");
        db.ScenarioTemplates.Add(template);
        return template.Id;
    }

    private Guid MintScenarioRole(ScenarioPermission[] permissions)
    {
        var role = TestData.ScenarioRole(permissions);
        db.ScenarioRoles.Add(role);
        return role.Id;
    }

    private Guid MintScenarioTemplateRole(ScenarioTemplatePermission[] permissions)
    {
        var role = TestData.ScenarioTemplateRole(permissions);
        db.ScenarioTemplateRoles.Add(role);
        return role.Id;
    }

    /// <summary>The role a named-resource membership takes: a seeded one, or null to mint one for the permissions.</summary>
    private static Guid? Pick<T>(Guid? roleId, T[] permissions, string step)
    {
        if (roleId is not null && permissions.Length > 0)
        {
            throw new InvalidOperationException(
                $"{step} was given both a role and permissions; both decide the membership's role. Drop one.");
        }

        if (roleId is null && permissions.Length == 0)
        {
            throw new InvalidOperationException(
                $"{step} needs a role or the permissions to grant; the column default role grants more than nothing.");
        }

        return roleId;
    }

    private sealed record PendingMembership<T>(Guid? ResourceId, Guid? RoleId, T[] Permissions);
}
