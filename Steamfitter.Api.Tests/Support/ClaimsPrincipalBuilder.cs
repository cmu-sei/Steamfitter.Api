// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// Steamfitter's claim shapes: one Permission claim per system permission, and one serialized
// ScenarioPermissionClaim / ScenarioTemplatePermissionClaim (JSON) per resource the user holds a
// membership on, as UserClaimsService.GetPermissionClaims writes them.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Steamfitter.Api.Data;
using Steamfitter.Api.Infrastructure.Authorization;

namespace Steamfitter.Api.Tests.Support;

/// <summary>
/// Builds the principal the authorization stack sees for a signed-in user, for the tests of the
/// authorization stack itself and for service tests on <see cref="ServiceTestBase"/>. Never an HTTP
/// test's caller: that is <see cref="TestActor"/>.
/// </summary>
public sealed class ClaimsPrincipalBuilder
{
    private readonly List<Claim> _claims = [];
    private Guid _userId = Guid.NewGuid();
    private string _name = "Test User";

    public Guid UserId => _userId;

    public ClaimsPrincipalBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public ClaimsPrincipalBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>Adds system permissions, which grant across every resource.</summary>
    public ClaimsPrincipalBuilder WithSystemPermissions(params SystemPermission[] permissions)
    {
        foreach (var permission in permissions)
        {
            _claims.Add(new Claim(AuthorizationConstants.PermissionClaimType, permission.ToString()));
        }

        return this;
    }

    /// <summary>A raw system-permission value, for values that are not enum names.</summary>
    public ClaimsPrincipalBuilder WithRawSystemPermission(string value)
    {
        _claims.Add(new Claim(AuthorizationConstants.PermissionClaimType, value));
        return this;
    }

    /// <summary>The claim a scenario membership produces.</summary>
    public ClaimsPrincipalBuilder WithScenario(Guid scenarioId, params ScenarioPermission[] permissions)
    {
        var claim = new ScenarioPermissionClaim { ScenarioId = scenarioId, Permissions = permissions };
        _claims.Add(new Claim(AuthorizationConstants.ScenarioPermissionClaimType, claim.ToString()));
        return this;
    }

    /// <summary>The claim a scenario template membership produces.</summary>
    public ClaimsPrincipalBuilder WithScenarioTemplate(Guid scenarioTemplateId, params ScenarioTemplatePermission[] permissions)
    {
        var claim = new ScenarioTemplatePermissionClaim { ScenarioTemplateId = scenarioTemplateId, Permissions = permissions };
        _claims.Add(new Claim(AuthorizationConstants.ScenarioTemplatePermissionClaimType, claim.ToString()));
        return this;
    }

    /// <summary>A raw scenario-permission value, for shapes the transformer cannot produce.</summary>
    public ClaimsPrincipalBuilder WithRawScenarioPermission(string value)
    {
        _claims.Add(new Claim(AuthorizationConstants.ScenarioPermissionClaimType, value));
        return this;
    }

    /// <summary>An arbitrary claim, for asserting that unrelated claim types are ignored.</summary>
    public ClaimsPrincipalBuilder WithClaim(string type, string value)
    {
        _claims.Add(new Claim(type, value));
        return this;
    }

    public ClaimsPrincipal Build()
    {
        var claims = new List<Claim>(_claims) { new("sub", _userId.ToString()), new("name", _name) };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    /// <summary>An authenticated principal with no permissions, the baseline every check must reject.</summary>
    public static ClaimsPrincipal Anonymous() => new ClaimsPrincipalBuilder().Build();
}
