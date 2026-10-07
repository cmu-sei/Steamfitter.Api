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
using Steamfitter.Api.Tests.Support;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Tests.Controllers;

/// <summary><c>UserController</c>: the user list (any of three View permissions), and user CRUD behind ViewUsers and ManageUsers.</summary>
public class UserControllerTests(DatabaseFixture fixture, SteamfitterAppFactory factory)
    : ApiTestBase(fixture, factory)
{
    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task An_unauthenticated_request_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync("api/users", Ct));
    }

    [Fact]
    public async Task GetAll_returns_the_users_to_a_caller_holding_ViewUsers()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewUsers).SeedAsync();

        var users = await ReadAsync<List<SAVM.User>>(await Client(actor).GetAsync("api/users", Ct));

        Assert.Contains(actor.Id, users.Select(x => x.Id));
    }

    [Fact]
    public async Task GetAll_returns_the_users_to_a_caller_holding_only_ViewScenarios()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync("api/users", Ct));
    }

    [Fact]
    public async Task GetAll_returns_the_users_to_a_caller_holding_only_ViewScenarioTemplates()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarioTemplates).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync("api/users", Ct));
    }

    [Fact]
    public async Task GetAll_is_forbidden_for_a_caller_holding_only_ViewGroups()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewGroups).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync("api/users", Ct));
    }

    [Fact]
    public async Task Get_returns_the_user_to_a_caller_holding_ViewUsers()
    {
        var user = TestData.User(name: "Someone");
        await Seed(user);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewUsers).SeedAsync();

        var read = await ReadAsync<SAVM.User>(await Client(actor).GetAsync($"api/users/{user.Id}", Ct));

        Assert.Equal("Someone", read.Name);
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_only_ViewScenarios()
    {
        var user = TestData.User();
        await Seed(user);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewScenarios).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/users/{user.Id}", Ct));
    }

    [Fact]
    public async Task Get_of_an_unknown_user_is_not_found()
    {
        await AssertJsonError(HttpStatusCode.NotFound, await RootClient.GetAsync($"api/users/{Guid.NewGuid()}", Ct));
    }

    [Fact]
    public async Task Create_persists_the_user_with_the_role_given_for_a_caller_holding_ManageUsers()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageUsers).SeedAsync();
        var id = Guid.NewGuid();

        var response = await Client(actor).PostAsJsonAsync("api/users", new { id, name = "Created", roleId = TestData.Roles.Observer.ToString() }, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        await using var db = NewContext();
        var stored = await db.Users.SingleAsync(x => x.Id == id, Ct);
        Assert.Equal(("Created", TestData.Roles.Observer, actor.Id), (stored.Name, stored.RoleId.Value, stored.CreatedBy));
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_only_ViewUsers()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewUsers).SeedAsync();
        var id = Guid.NewGuid();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/users", new { id, name = "Refused" }, Ct));
        await using var db = NewContext();
        Assert.False(await db.Users.AnyAsync(x => x.Id == id, Ct));
    }

    [Fact]
    public async Task Update_saves_the_user_for_a_caller_holding_ManageUsers()
    {
        var user = TestData.User(name: "Before");
        await Seed(user);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageUsers).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/users/{user.Id}", new { id = user.Id, name = "After" }, Ct));

        await using var db = NewContext();
        Assert.Equal("After", (await db.Users.SingleAsync(x => x.Id == user.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_only_ViewUsers()
    {
        var user = TestData.User(name: "Before");
        await Seed(user);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewUsers).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/users/{user.Id}", new { id = user.Id, name = "After" }, Ct));
    }

    [Fact]
    public async Task Update_of_the_callers_own_id_is_forbidden()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageUsers).SeedAsync();

        var error = await AssertJsonError(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/users/{actor.Id}", new { id = Guid.NewGuid(), name = "Renamed" }, Ct));

        Assert.Equal("You cannot change your own Id", error.Title);
    }

    [Fact]
    public async Task Delete_removes_the_user_for_a_caller_holding_ManageUsers()
    {
        var user = TestData.User();
        await Seed(user);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageUsers).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/users/{user.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.Users.AnyAsync(x => x.Id == user.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_only_ViewUsers()
    {
        var user = TestData.User();
        await Seed(user);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewUsers).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/users/{user.Id}", Ct));
        await using var db = NewContext();
        Assert.True(await db.Users.AnyAsync(x => x.Id == user.Id, Ct));
    }

    [Fact]
    public async Task Delete_of_the_callers_own_account_is_forbidden()
    {
        var error = await AssertJsonError(HttpStatusCode.Forbidden, await RootClient.DeleteAsync($"api/users/{Root.Id}", Ct));

        Assert.Equal("You cannot delete your own account", error.Title);
    }

    /// <summary>A caller holding only ManageUsers gives itself the Administrator role.</summary>
    [Fact]
    public async Task Update_lets_a_caller_holding_only_ManageUsers_give_itself_the_administrator_role()
    {
        var actor = await Actor().WithName("Climber").WithSystemPermissions(SystemPermission.ManageUsers).SeedAsync();

        var response = await Client(actor).PutAsJsonAsync($"api/users/{actor.Id}", new { id = actor.Id, name = "Climber", roleId = TestData.Roles.Administrator.ToString() }, Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        await using var db = NewContext();
        Assert.Equal(TestData.Roles.Administrator, (await db.Users.SingleAsync(x => x.Id == actor.Id, Ct)).RoleId);
    }
}
