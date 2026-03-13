// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Tests.Integration.Fixtures;
using TUnit.Core;
using UserViewModel = Steamfitter.Api.ViewModels.User;

namespace Steamfitter.Api.Tests.Integration.Tests.Controllers;

[Category("Integration")]
[ClassDataSource<SteamfitterTestContext>(Shared = SharedType.PerTestSession)]
public class UserControllerTests(SteamfitterTestContext context)
{
    private readonly HttpClient _client = context.CreateClient();
    private readonly SteamfitterTestContext _context = context;

    [Test]
    public async System.Threading.Tasks.Task GetUsers_WhenUsersExist_ReturnsOkAndList()
    {
        // Arrange - seed a user
        using var dbContext = _context.GetDbContext();
        var userEntity = new UserEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test User for GetUsers"
        };
        dbContext.Users.Add(userEntity);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<UserViewModel>>();
        await Assert.That(users).IsNotNull();
        await Assert.That(users!.Any(u => u.Id == userEntity.Id)).IsTrue();
    }

    [Test]
    public async System.Threading.Tasks.Task GetUser_ByIdWhenUserExists_ReturnsOk()
    {
        // Arrange
        using var dbContext = _context.GetDbContext();
        var userEntity = new UserEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test User for GetById"
        };
        dbContext.Users.Add(userEntity);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/users/{userEntity.Id}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserViewModel>();
        await Assert.That(user).IsNotNull();
        await Assert.That(user!.Id).IsEqualTo(userEntity.Id);
        await Assert.That(user.Name).IsEqualTo(userEntity.Name);
    }

    [Test]
    public async System.Threading.Tasks.Task CreateUser_WithValidData_ReturnsCreated()
    {
        // Arrange
        var newUser = new UserViewModel
        {
            Id = Guid.NewGuid(),
            Name = "Newly Created User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", newUser);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<UserViewModel>();
        await Assert.That(created).IsNotNull();
        await Assert.That(created!.Id).IsEqualTo(newUser.Id);
        await Assert.That(created.Name).IsEqualTo(newUser.Name);
    }

    [Test]
    public async System.Threading.Tasks.Task DeleteUser_WhenUserExists_ReturnsNoContent()
    {
        // Arrange
        using var dbContext = _context.GetDbContext();
        var userEntity = new UserEntity
        {
            Id = Guid.NewGuid(),
            Name = "User To Delete"
        };
        dbContext.Users.Add(userEntity);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/users/{userEntity.Id}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }
}
