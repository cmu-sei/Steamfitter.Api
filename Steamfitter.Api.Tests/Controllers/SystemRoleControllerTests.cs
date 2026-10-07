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

/// <summary><c>SystemRolesController</c>: <c>api/system-roles</c>, behind ViewRoles and ManageRoles.</summary>
public class SystemRoleControllerTests(DatabaseFixture fixture, SteamfitterAppFactory factory)
    : ApiTestBase(fixture, factory)
{
    /// <summary>A request with no identity is answered with a 401; the anonymous client is the case under test.</summary>
    [Fact]
    public async Task An_unauthenticated_request_is_unauthorized()
    {
        await AssertStatus(HttpStatusCode.Unauthorized, await Client().GetAsync("api/system-roles", Ct));
    }

    [Fact]
    public async Task GetAll_returns_the_seeded_roles_to_a_caller_holding_ViewRoles()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewRoles).SeedAsync();

        var roles = await ReadAsync<List<SAVM.SystemRole>>(await Client(actor).GetAsync("api/system-roles", Ct));

        Assert.Contains(TestData.Roles.Administrator, roles.Select(x => x.Id));
    }

    [Fact]
    public async Task GetAll_is_forbidden_for_a_caller_holding_only_ManageUsers()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageUsers).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync("api/system-roles", Ct));
    }

    [Fact]
    public async Task Get_returns_the_role_to_a_caller_holding_ViewRoles()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewRoles).SeedAsync();

        var role = await ReadAsync<SAVM.SystemRole>(await Client(actor).GetAsync($"api/system-roles/{TestData.Roles.Administrator}", Ct));

        Assert.Equal(("Administrator", true, true), (role.Name, role.AllPermissions, role.Immutable));
    }

    [Fact]
    public async Task Get_is_forbidden_for_a_caller_holding_only_ManageRoles()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageRoles).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).GetAsync($"api/system-roles/{TestData.Roles.Observer}", Ct));
    }

    /// <summary>An unknown role id is answered with an empty 204.</summary>
    [Fact]
    public async Task Get_answers_an_unknown_role_with_no_content()
    {
        await AssertStatus(HttpStatusCode.NoContent, await RootClient.GetAsync($"api/system-roles/{Guid.NewGuid()}", Ct));
    }

    [Fact]
    public async Task Create_persists_the_role_for_a_caller_holding_ManageRoles()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageRoles).SeedAsync();

        var response = await Client(actor).PostAsJsonAsync("api/system-roles", new { name = "Operators", permissions = new[] { "ExecuteScenarios" } }, Ct);

        await AssertStatus(HttpStatusCode.Created, response);
        await using var db = NewContext();
        Assert.Equal([SystemPermission.ExecuteScenarios], (await db.SystemRoles.SingleAsync(x => x.Name == "Operators", Ct)).Permissions);
    }

    [Fact]
    public async Task Create_is_forbidden_for_a_caller_holding_only_ViewRoles()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewRoles).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PostAsJsonAsync("api/system-roles", new { name = "Refused" }, Ct));
        await using var db = NewContext();
        Assert.False(await db.SystemRoles.AnyAsync(x => x.Name == "Refused", Ct));
    }

    /// <summary>A name another role holds is answered with a 500 on create.</summary>
    [Fact]
    public async Task Create_answers_a_name_that_is_already_taken_with_a_server_error()
    {
        await Seed(TestData.SystemRole("Taken"));

        var error = await AssertJsonError(HttpStatusCode.InternalServerError, await RootClient.PostAsJsonAsync("api/system-roles", new { name = "Taken" }, Ct));

        Assert.Contains("saving the entity changes", error.Detail);
    }

    [Fact]
    public async Task Update_saves_the_role_for_a_caller_holding_ManageRoles()
    {
        var role = TestData.SystemRole("Before");
        await Seed(role);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageRoles).SeedAsync();

        await AssertStatus(HttpStatusCode.OK, await Client(actor).PutAsJsonAsync($"api/system-roles/{role.Id}", new { id = role.Id, name = "After", permissions = Array.Empty<string>() }, Ct));

        await using var db = NewContext();
        Assert.Equal("After", (await db.SystemRoles.SingleAsync(x => x.Id == role.Id, Ct)).Name);
    }

    [Fact]
    public async Task Update_is_forbidden_for_a_caller_holding_only_ViewRoles()
    {
        var role = TestData.SystemRole("Before");
        await Seed(role);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewRoles).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).PutAsJsonAsync($"api/system-roles/{role.Id}", new { id = role.Id, name = "After" }, Ct));
    }

    /// <summary>The seeded Administrator role, marked immutable, is renamed and stripped of AllPermissions on update.</summary>
    [Fact]
    public async Task Update_changes_the_immutable_administrator_role()
    {
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageRoles).SeedAsync();

        var response = await Client(actor).PutAsJsonAsync(
            $"api/system-roles/{TestData.Roles.Administrator}",
            new { id = TestData.Roles.Administrator, name = "Demoted", allPermissions = false, immutable = true, permissions = Array.Empty<string>() },
            Ct);

        await AssertStatus(HttpStatusCode.OK, response);
        await using var db = NewContext();
        var stored = await db.SystemRoles.SingleAsync(x => x.Id == TestData.Roles.Administrator, Ct);
        Assert.Equal(("Demoted", false), (stored.Name, stored.AllPermissions));
    }

    [Fact]
    public async Task Delete_removes_the_role_for_a_caller_holding_ManageRoles()
    {
        var role = TestData.SystemRole();
        await Seed(role);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ManageRoles).SeedAsync();

        await AssertStatus(HttpStatusCode.NoContent, await Client(actor).DeleteAsync($"api/system-roles/{role.Id}", Ct));

        await using var db = NewContext();
        Assert.False(await db.SystemRoles.AnyAsync(x => x.Id == role.Id, Ct));
    }

    [Fact]
    public async Task Delete_is_forbidden_for_a_caller_holding_only_ViewRoles()
    {
        var role = TestData.SystemRole();
        await Seed(role);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewRoles).SeedAsync();

        await AssertStatus(HttpStatusCode.Forbidden, await Client(actor).DeleteAsync($"api/system-roles/{role.Id}", Ct));
        await using var db = NewContext();
        Assert.True(await db.SystemRoles.AnyAsync(x => x.Id == role.Id, Ct));
    }

    /// <summary>A role a user holds is answered with a 500 on delete.</summary>
    [Fact]
    public async Task Delete_answers_a_role_a_user_holds_with_a_server_error()
    {
        var role = TestData.SystemRole();
        await Seed(role, TestData.User(roleId: role.Id));

        var error = await AssertJsonError(HttpStatusCode.InternalServerError, await RootClient.DeleteAsync($"api/system-roles/{role.Id}", Ct));

        Assert.Contains("saving the entity changes", error.Detail);
        await using var db = NewContext();
        Assert.True(await db.SystemRoles.AnyAsync(x => x.Id == role.Id, Ct));
    }
}
