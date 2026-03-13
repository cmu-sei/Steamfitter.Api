// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Security.Claims;
using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using AutoMapper;
using Crucible.Common.Testing.Fixtures;
using FakeItEasy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Shouldly;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Infrastructure.Exceptions;
using Steamfitter.Api.Infrastructure.Mappings;
using Steamfitter.Api.Services;
using Steamfitter.Api.Tests.Shared.Fixtures;
using Xunit;

namespace Steamfitter.Api.Tests.Unit.Services;

public class UserServiceTests
{
    private readonly IFixture _fixture;
    private readonly IMapper _mapper;

    public UserServiceTests()
    {
        _fixture = new Fixture().Customize(new SteamfitterCustomization())
                                .Customize(new AutoFakeItEasyCustomization());

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<UserProfile>();
        });
        _mapper = config.CreateMapper();
    }

    private (UserService service, SteamfitterContext context) CreateService(
        Guid? userId = null,
        List<UserEntity>? users = null)
    {
        var uid = userId ?? Guid.NewGuid();
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", uid.ToString()),
            new Claim("name", "Test User")
        }, "TestAuth"));

        var context = TestDbContextFactory.Create<SteamfitterContext>();
        var authorizationService = A.Fake<IAuthorizationService>();
        var userClaimsService = A.Fake<IUserClaimsService>();
        var logger = A.Fake<ILogger<IUserService>>();

        if (users != null)
        {
            context.Users.AddRange(users);
            context.SaveChanges();
        }

        var service = new UserService(
            context,
            claims,
            authorizationService,
            userClaimsService,
            logger,
            _mapper);

        return (service, context);
    }

    [Fact]
    public async Task GetAsync_ReturnsAllUsers()
    {
        // Arrange
        var entities = _fixture.CreateMany<UserEntity>(3).ToList();
        var (service, _) = CreateService(users: entities);

        // Act
        var result = await service.GetAsync(CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(3);
    }

    [Fact]
    public async Task GetAsync_ById_ReturnsMappedUser()
    {
        // Arrange
        var entity = _fixture.Create<UserEntity>();
        var entities = new List<UserEntity> { entity };
        var (service, _) = CreateService(users: entities);

        // Act
        var result = await service.GetAsync(entity.Id, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(entity.Id);
        result.Name.ShouldBe(entity.Name);
    }

    [Fact]
    public async Task GetAsync_ById_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var (service, _) = CreateService(users: new List<UserEntity>());

        // Act
        var result = await service.GetAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenDeletingSelf_ThrowsForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var entity = _fixture.Build<UserEntity>()
            .Without(x => x.Role)
            .Without(x => x.ScenarioMemberships)
            .Without(x => x.ScenarioTemplateMemberships)
            .Without(x => x.GroupMemberships)
            .With(x => x.Id, userId)
            .Create();
        var entities = new List<UserEntity> { entity };
        var (service, _) = CreateService(userId: userId, users: entities);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenException>(
            () => service.DeleteAsync(userId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_WhenUserExists_RemovesAndReturnsTrue()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetEntity = _fixture.Create<UserEntity>();
        var entities = new List<UserEntity> { targetEntity };
        var (service, context) = CreateService(userId: currentUserId, users: entities);

        // Act
        var result = await service.DeleteAsync(targetEntity.Id, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
        var deletedUser = await context.Users.FindAsync(targetEntity.Id);
        deletedUser.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateAsync_WhenChangingOwnId_ThrowsForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var entity = _fixture.Build<UserEntity>()
            .Without(x => x.Role)
            .Without(x => x.ScenarioMemberships)
            .Without(x => x.ScenarioTemplateMemberships)
            .Without(x => x.GroupMemberships)
            .With(x => x.Id, userId)
            .Create();
        var entities = new List<UserEntity> { entity };
        var (service, _) = CreateService(userId: userId, users: entities);

        var updatedUser = new ViewModels.User { Id = Guid.NewGuid(), Name = "Changed" };

        // Act & Assert
        await Should.ThrowAsync<ForbiddenException>(
            () => service.UpdateAsync(userId, updatedUser, CancellationToken.None));
    }
}
