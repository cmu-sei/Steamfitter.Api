// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using AutoMapper;
using Shouldly;
using Steamfitter.Api.Infrastructure.Mapping;
using Steamfitter.Api.Infrastructure.Mappings;
using Xunit;

namespace Steamfitter.Api.Tests.Unit;

[Trait("Category", "Unit")]
public class MappingConfigurationTests
{
    [Fact]
    public void CreateMapper_WithAllProfiles_ShouldSucceed()
    {
        // Arrange
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ScenarioProfile>();
            cfg.AddProfile<ScenarioTemplateProfile>();
            cfg.AddProfile<TaskProfile>();
            cfg.AddProfile<ResultProfile>();
            cfg.AddProfile<UserProfile>();
            cfg.AddProfile<VmCredentialProfile>();
            cfg.AddProfile<GroupProfile>();
            cfg.AddProfile<GroupMembershipProfile>();
            cfg.AddProfile<ScenarioMembershipProfile>();
            cfg.AddProfile<ScenarioTemplateMembershipProfile>();
            cfg.AddProfile<ScenarioRoleProfile>();
            cfg.AddProfile<ScenarioTemplateRoleProfile>();
            cfg.AddProfile<SystemRoleProfile>();
        });

        // Act - verify mapper can be created (weaker than AssertConfigurationIsValid
        // because the app has unmapped navigation properties populated elsewhere)
        var mapper = config.CreateMapper();
        mapper.ShouldNotBeNull();
    }

    [Fact]
    public void Map_ScenarioEntityToViewModel_MapsProperties()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ScenarioProfile>());
        var mapper = config.CreateMapper();

        var entity = new Steamfitter.Api.Data.Models.ScenarioEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Scenario",
            Description = "Test Description",
            Status = Steamfitter.Api.Data.ScenarioStatus.active
        };

        // Act
        var result = mapper.Map<Steamfitter.Api.ViewModels.Scenario>(entity);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(entity.Id);
        result.Name.ShouldBe(entity.Name);
        result.Description.ShouldBe(entity.Description);
        result.Status.ShouldBe(entity.Status);
    }

    [Fact]
    public void Map_UserEntityToViewModel_MapsProperties()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<UserProfile>());
        var mapper = config.CreateMapper();

        var entity = new Steamfitter.Api.Data.Models.UserEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test User"
        };

        // Act
        var result = mapper.Map<Steamfitter.Api.ViewModels.User>(entity);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(entity.Id);
        result.Name.ShouldBe(entity.Name);
    }

    [Fact]
    public void Map_TaskEntityToViewModel_MapsProperties()
    {
        // Arrange
        var config = new MapperConfiguration(cfg => cfg.AddProfile<TaskProfile>());
        var mapper = config.CreateMapper();

        var entity = new Steamfitter.Api.Data.Models.TaskEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Task",
            Description = "Test Description",
            Action = Steamfitter.Api.Data.TaskAction.guest_process_run,
            Status = Steamfitter.Api.Data.TaskStatus.pending,
            Score = 10
        };

        // Act
        var result = mapper.Map<Steamfitter.Api.ViewModels.Task>(entity);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(entity.Id);
        result.Name.ShouldBe(entity.Name);
        result.Action.ShouldBe(entity.Action);
        result.Score.ShouldBe(entity.Score);
    }
}
