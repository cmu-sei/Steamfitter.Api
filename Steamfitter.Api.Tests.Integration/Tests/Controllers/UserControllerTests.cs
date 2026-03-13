// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Tests.Integration.Fixtures;
using Xunit;
using UserViewModel = Steamfitter.Api.ViewModels.User;

namespace Steamfitter.Api.Tests.Integration.Tests.Controllers;

public class UserControllerTests : IClassFixture<SteamfitterTestContext>
{
    private readonly HttpClient _client;
    private readonly SteamfitterTestContext _context;

    public UserControllerTests(SteamfitterTestContext context)
    {
        _context = context;
        _client = context.CreateClient();
    }

    [Fact]
    public async Task GetUsers_ReturnsOkAndList()
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<UserViewModel>>();
        users.ShouldNotBeNull();
        users.ShouldContain(u => u.Id == userEntity.Id);
    }

    [Fact]
    public async Task GetUser_ById_ReturnsOk()
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserViewModel>();
        user.ShouldNotBeNull();
        user.Id.ShouldBe(userEntity.Id);
        user.Name.ShouldBe(userEntity.Name);
    }

    [Fact]
    public async Task CreateUser_ReturnsCreated()
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
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<UserViewModel>();
        created.ShouldNotBeNull();
        created.Id.ShouldBe(newUser.Id);
        created.Name.ShouldBe(newUser.Name);
    }

    [Fact]
    public async Task DeleteUser_ReturnsNoContent()
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
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
