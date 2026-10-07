// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Steamfitter.Api.Data;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Infrastructure.Options;
using Steamfitter.Api.Services;
using Steamfitter.Api.Tests.Support;

namespace Steamfitter.Api.Tests.Services;

/// <summary>
/// <c>UserClaimsService</c>, behind <c>AuthorizationClaimsTransformer</c>: the paths the HTTP tests turn
/// off (claims caching, roles and groups read from the token) and the user row it writes.
/// </summary>
public class UserClaimsServiceTests(DatabaseFixture fixture) : DatabaseTestBase(fixture)
{
    [Fact]
    public async Task A_first_request_creates_the_user_with_the_tokens_name()
    {
        var userId = Guid.NewGuid();

        await AddClaims(Principal(userId, ("name", "Newcomer")), new ClaimsTransformationOptions(), update: true);

        await using var db = NewContext();
        Assert.Equal("Newcomer", (await db.Users.SingleAsync(x => x.Id == userId, Ct)).Name);
    }

    [Fact]
    public async Task A_later_request_renames_the_user_to_the_tokens_name()
    {
        var user = TestData.User(name: "Old");
        await Seed(user);

        await AddClaims(Principal(user.Id, ("name", "New")), new ClaimsTransformationOptions(), update: true);

        await using var db = NewContext();
        Assert.Equal("New", (await db.Users.SingleAsync(x => x.Id == user.Id, Ct)).Name);
    }

    [Fact]
    public async Task A_role_named_in_the_token_grants_its_permissions_when_roles_come_from_the_identity_provider()
    {
        var role = TestData.SystemRole("Token Role", permissions: [SystemPermission.ViewRoles]);
        var user = TestData.User();
        await Seed(role, user);
        var options = new ClaimsTransformationOptions { UseRolesFromIdP = true, RolesClaimPath = "roles" };

        var principal = await AddClaims(Principal(user.Id, ("roles", "token role")), options);

        Assert.Equal(["ViewRoles"], Permissions(principal));
    }

    [Fact]
    public async Task A_role_named_in_the_token_grants_nothing_when_roles_do_not_come_from_the_identity_provider()
    {
        var role = TestData.SystemRole("Token Role", permissions: [SystemPermission.ViewRoles]);
        var user = TestData.User();
        await Seed(role, user);

        var principal = await AddClaims(Principal(user.Id, ("roles", "token role")), new ClaimsTransformationOptions { RolesClaimPath = "roles" });

        Assert.Empty(Permissions(principal));
    }

    [Fact]
    public async Task A_group_named_in_the_token_grants_its_scenario_memberships_when_groups_come_from_the_identity_provider()
    {
        var group = TestData.Group("Token Group");
        var scenario = TestData.Scenario();
        var user = TestData.User();
        await Seed(group, scenario, user, TestData.ScenarioMembership(scenario.Id, groupId: group.Id, roleId: TestData.ScenarioRoles.Observer));
        var options = new ClaimsTransformationOptions { UseGroupsFromIdP = true, GroupsClaimPath = "groups" };

        var principal = await AddClaims(Principal(user.Id, ("groups", "token group")), options);

        var claim = ScenarioPermissionClaim.FromString(Assert.Single(principal.Claims, x => x.Type == AuthorizationConstants.ScenarioPermissionClaimType).Value);
        Assert.Equal(scenario.Id, claim.ScenarioId);
    }

    [Fact]
    public async Task Cached_claims_answer_a_later_request_for_the_same_user_when_caching_is_on()
    {
        var role = TestData.SystemRole(permissions: [SystemPermission.ViewRoles]);
        var user = TestData.User(roleId: role.Id);
        await Seed(role, user);
        var options = new ClaimsTransformationOptions { EnableCaching = true, CacheExpirationSeconds = 60 };
        using var cache = new MemoryCache(new MemoryCacheOptions());
        await AddClaims(Principal(user.Id), options, cache: cache);
        await using (var db = NewContext())
        {
            var stored = await db.Users.SingleAsync(x => x.Id == user.Id, Ct);
            stored.RoleId = null;
            await db.SaveChangesAsync(Ct);
        }

        var principal = await AddClaims(Principal(user.Id), options, cache: cache);

        Assert.Equal(["ViewRoles"], Permissions(principal));
    }

    private async Task<ClaimsPrincipal> AddClaims(ClaimsPrincipal principal, ClaimsTransformationOptions options, bool update = false, IMemoryCache cache = null)
    {
        await using var context = NewContext();
        using var ownCache = new MemoryCache(new MemoryCacheOptions());

        return await new UserClaimsService(context, cache ?? ownCache, options).AddUserClaims(principal, update);
    }

    private static ClaimsPrincipal Principal(Guid userId, params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity([new Claim("sub", userId.ToString()), .. claims.Select(x => new Claim(x.Type, x.Value))], "Test"));

    private static string[] Permissions(ClaimsPrincipal principal) =>
        [.. principal.Claims.Where(x => x.Type == AuthorizationConstants.PermissionClaimType).Select(x => x.Value).Order()];
}
