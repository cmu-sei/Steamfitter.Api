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
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Infrastructure.Exceptions;
using Steamfitter.Api.Infrastructure.Mappings;
using Steamfitter.Api.Services;
using Steamfitter.Api.Tests.Shared.Fixtures;
using TUnit.Core;

namespace Steamfitter.Api.Tests.Unit.Services;

[Category("Unit")]
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

    [Test]
    public async System.Threading.Tasks.Task GetAsync_WhenMultipleUsersExist_ReturnsAllUsers()
    {
        // Arrange
        var entities = _fixture.CreateMany<UserEntity>(3).ToList();
        var (service, _) = CreateService(users: entities);

        // Act
        var result = await service.GetAsync(CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count()).IsEqualTo(3);
    }

    [Test]
    public async System.Threading.Tasks.Task GetAsync_ByIdWhenUserExists_ReturnsMappedUser()
    {
        // Arrange
        var entity = _fixture.Create<UserEntity>();
        var entities = new List<UserEntity> { entity };
        var (service, _) = CreateService(users: entities);

        // Act
        var result = await service.GetAsync(entity.Id, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Id).IsEqualTo(entity.Id);
        await Assert.That(result.Name).IsEqualTo(entity.Name);
    }

    [Test]
    public async System.Threading.Tasks.Task GetAsync_ById_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var (service, _) = CreateService(users: new List<UserEntity>());

        // Act
        var result = await service.GetAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async System.Threading.Tasks.Task DeleteAsync_WhenDeletingSelf_ThrowsForbidden()
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
        await Assert.That(async () => await service.DeleteAsync(userId, CancellationToken.None))
            .ThrowsExactly<ForbiddenException>();
    }

    [Test]
    public async System.Threading.Tasks.Task DeleteAsync_WhenUserExistsAndNotSelf_RemovesAndReturnsTrue()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetEntity = _fixture.Create<UserEntity>();
        var entities = new List<UserEntity> { targetEntity };
        var (service, context) = CreateService(userId: currentUserId, users: entities);

        // Act
        var result = await service.DeleteAsync(targetEntity.Id, CancellationToken.None);

        // Assert
        await Assert.That(result).IsTrue();
        var deletedUser = await context.Users.FindAsync(targetEntity.Id);
        await Assert.That(deletedUser).IsNull();
    }

    [Test]
    public async System.Threading.Tasks.Task UpdateAsync_WhenChangingOwnId_ThrowsForbidden()
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
        await Assert.That(async () => await service.UpdateAsync(userId, updatedUser, CancellationToken.None))
            .ThrowsExactly<ForbiddenException>();
    }
}
