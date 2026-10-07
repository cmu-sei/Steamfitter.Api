// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data;
using Steamfitter.Api.Hubs;
using Steamfitter.Api.Tests.Support;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Tests.Controllers;

/// <summary><c>GroupController</c>: groups and their memberships, behind ViewGroups and ManageGroups.</summary>
public class GroupControllerTests(DatabaseFixture fixture, SteamfitterAppFactory factory)
    : ApiTestBase(fixture, factory)
{
    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task An_unauthenticated_request_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync($"api/groups/{Guid.NewGuid()}", Ct));
    }

    [Fact]
    public async Task Get_returns_the_group_to_a_caller_holding_ViewGroups()
    {
        var group = TestData.Group("Readers");
        await Seed(group);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewGroups).SeedAsync();

        var read = await ReadAsync<SAVM.Group>(await Client(actor).GetAsync($"api/groups/{group.Id}", Ct));

        Assert.Equal("Readers", read.Name);
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_only_ManageUsers()
    {
        var group = TestData.Group();
        await Seed(group);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageUsers).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/groups/{group.Id}", Ct));
    }

    /// <summary>An unknown group id is answered with an empty 204.</summary>
    [Fact]
    public async Task Get_answers_an_unknown_group_with_no_content()
    {
        var response = await RootClient.GetAsync($"api/groups/{Guid.NewGuid()}", Ct);

        await AssertStatus(HttpStatusCode.NoContent, response);
    }

    [Fact]
    public async Task GetAll_returns_the_groups_to_a_caller_holding_ViewGroups()
    {
        var group = TestData.Group("Listed");
        await Seed(group);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewGroups).SeedAsync();

        var groups = await ReadAsync<List<SAVM.Group>>(await Client(actor).GetAsync("api/groups", Ct));

        Assert.Equal([group.Id], groups.Select(x => x.Id));
    }

    [Fact]
    public async Task GetAll_is_forbidden_for_a_caller_holding_only_ViewUsers()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewUsers).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync("api/groups", Ct));
    }

    [Fact]
    public async Task Create_persists_the_group_for_a_caller_holding_ManageGroups()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageGroups).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync("api/groups", new { name = "Created" }, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        await using var db = NewContext();
        Assert.True(await db.Groups.AnyAsync(x => x.Name == "Created", Ct));
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_only_ViewGroups()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewGroups).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/groups", new { name = "Refused" }, Ct));
        await using var db = NewContext();
        Assert.False(await db.Groups.AnyAsync(x => x.Name == "Refused", Ct));
    }

    [Fact]
    public async Task Update_saves_the_group_for_a_caller_holding_ManageGroups()
    {
        var group = TestData.Group("Before");
        await Seed(group);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageGroups).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/groups/{group.Id}", new { id = group.Id, name = "After" }, Ct));

        await using var db = NewContext();
        Assert.Equal("After", (await db.Groups.SingleAsync(x => x.Id == group.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_only_ViewGroups()
    {
        var group = TestData.Group("Before");
        await Seed(group);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewGroups).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/groups/{group.Id}", new { id = group.Id, name = "After" }, Ct));
    }

    [Fact]
    public async Task Delete_removes_the_group_for_a_caller_holding_ManageGroups()
    {
        var group = TestData.Group();
        await Seed(group);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageGroups).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/groups/{group.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.Groups.AnyAsync(x => x.Id == group.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_only_ManageUsers()
    {
        var group = TestData.Group();
        await Seed(group);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageUsers).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/groups/{group.Id}", Ct));
    }

    [Fact]
    public async Task GetMemberships_returns_the_groups_memberships_to_a_caller_holding_ViewGroups()
    {
        var group = TestData.Group();
        var user = TestData.User();
        var membership = TestData.GroupMembership(group.Id, user.Id);
        await Seed(group, user, membership);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewGroups).SeedAsync();

        var memberships = await ReadAsync<List<SAVM.GroupMembership>>(await Client(actor).GetAsync($"api/groups/{group.Id}/memberships", Ct));

        Assert.Equal([(membership.Id, user.Id)], memberships.Select(x => (x.Id, x.UserId)));
    }

    [Fact]
    public async Task GetMemberships_is_forbidden_for_a_caller_holding_only_ManageGroups()
    {
        var group = TestData.Group();
        await Seed(group);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageGroups).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/groups/{group.Id}/memberships", Ct));
    }

    [Fact]
    public async Task GetMembership_returns_the_membership_to_a_caller_holding_ViewGroups()
    {
        var group = TestData.Group();
        var user = TestData.User();
        var membership = TestData.GroupMembership(group.Id, user.Id);
        await Seed(group, user, membership);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewGroups).SeedAsync();

        var read = await ReadAsync<SAVM.GroupMembership>(await Client(actor).GetAsync($"api/groups/memberships/{membership.Id}", Ct));

        Assert.Equal(group.Id, read.GroupId);
    }

    [Fact]
    public async Task GetMembership_is_forbidden_for_a_caller_holding_only_ViewUsers()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewUsers).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/groups/memberships/{Guid.NewGuid()}", Ct));
    }

    [Fact]
    public async Task CreateMembership_adds_the_user_to_the_group_for_a_caller_holding_ManageGroups()
    {
        var group = TestData.Group();
        var user = TestData.User();
        await Seed(group, user);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageGroups).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync($"api/groups/{group.Id}/memberships", new { groupId = group.Id, userId = user.Id }, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        await using var db = NewContext();
        Assert.True(await db.GroupMemberships.AnyAsync(x => x.GroupId == group.Id && x.UserId == user.Id, Ct));
    }

    [Fact]
    public async Task CreateMembership_broadcasts_the_membership_to_the_group_administrators()
    {
        var group = TestData.Group();
        var user = TestData.User();
        await Seed(group, user);

        var created = await ReadAsync<SAVM.GroupMembership>(await RootClient.PostAsJsonAsync($"api/groups/{group.Id}/memberships", new { groupId = group.Id, userId = user.Id }, Ct));

        Assert.Contains(Factory.Hub<EngineHub>().ToGroup(EngineHub.GROUP_GROUP),
            x => x.Method == EngineMethods.GroupMembershipCreated && ((SAVM.GroupMembership)x.Argument).Id == created.Id);
    }

    [Fact]
    public async Task CreateMembership_is_forbidden_for_a_caller_holding_only_ViewGroups()
    {
        var group = TestData.Group();
        var user = TestData.User();
        await Seed(group, user);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewGroups).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync($"api/groups/{group.Id}/memberships", new { groupId = group.Id, userId = user.Id }, Ct));
        await using var db = NewContext();
        Assert.False(await db.GroupMemberships.AnyAsync(x => x.GroupId == group.Id, Ct));
    }

    /// <summary>A user already in the group is answered with a 500 when added again.</summary>
    [Fact]
    public async Task CreateMembership_answers_a_user_already_in_the_group_with_a_server_error()
    {
        var group = TestData.Group();
        var user = TestData.User();
        await Seed(group, user, TestData.GroupMembership(group.Id, user.Id));

        var response = await RootClient.PostAsJsonAsync($"api/groups/{group.Id}/memberships", new { groupId = group.Id, userId = user.Id }, Ct);

        var error = await AssertJsonError(HttpStatusCode.InternalServerError, response);
        Assert.Contains("saving the entity changes", error.Detail);
    }

    [Fact]
    public async Task DeleteMembership_removes_the_membership_for_a_caller_holding_ManageGroups()
    {
        var group = TestData.Group();
        var user = TestData.User();
        var membership = TestData.GroupMembership(group.Id, user.Id);
        await Seed(group, user, membership);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageGroups).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/groups/memberships/{membership.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.GroupMemberships.AnyAsync(x => x.Id == membership.Id, Ct));
    }

    [Fact]
    public async Task DeleteMembership_is_forbidden_for_a_caller_holding_only_ViewGroups()
    {
        var group = TestData.Group();
        var user = TestData.User();
        var membership = TestData.GroupMembership(group.Id, user.Id);
        await Seed(group, user, membership);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewGroups).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/groups/memberships/{membership.Id}", Ct));
    }
}
