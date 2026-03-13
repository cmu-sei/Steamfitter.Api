// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net.Http;
using System.Security.Claims;
using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using AutoMapper;
using Crucible.Common.Testing.Fixtures;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Infrastructure.Mappings;
using Steamfitter.Api.Infrastructure.Options;
using Steamfitter.Api.Services;
using Steamfitter.Api.Tests.Shared.Fixtures;
using Xunit;
using SystemTask = System.Threading.Tasks.Task;

namespace Steamfitter.Api.Tests.Unit.Services;

[Trait("Category", "Unit")]
public class TaskServiceTests
{
    private readonly IFixture _fixture;
    private readonly IMapper _mapper;

    public TaskServiceTests()
    {
        _fixture = new Fixture().Customize(new SteamfitterCustomization())
                                .Customize(new AutoFakeItEasyCustomization());

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TaskProfile>();
        });
        _mapper = config.CreateMapper();
    }

    private (TaskService service, SteamfitterContext context) CreateService(
        Guid? userId = null,
        List<TaskEntity>? tasks = null)
    {
        var uid = userId ?? Guid.NewGuid();
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", uid.ToString()),
            new Claim("name", "Test User")
        }, "TestAuth"));

        var context = TestDbContextFactory.Create<SteamfitterContext>();
        var stackStormService = A.Fake<IStackStormService>();
        var options = Options.Create(new VmTaskProcessingOptions());
        var resultService = A.Fake<IResultService>();
        var logger = A.Fake<ILogger<TaskService>>();
        var playerService = A.Fake<IPlayerService>();
        var playerVmService = A.Fake<IPlayerVmService>();
        var httpClientFactory = A.Fake<IHttpClientFactory>();
        var clientOptions = new ClientOptions
        {
            urls = new ApiUrlSettings { playerApi = "https://localhost", vmApi = "https://localhost" }
        };
        var taskExecutionQueue = A.Fake<ITaskExecutionQueue>();
        var scopeFactory = A.Fake<IServiceScopeFactory>();

        if (tasks != null)
        {
            context.Tasks.AddRange(tasks);
            context.SaveChanges();
        }

        var service = new TaskService(
            context,
            claims,
            _mapper,
            stackStormService,
            options,
            resultService,
            logger,
            playerService,
            playerVmService,
            httpClientFactory,
            clientOptions,
            taskExecutionQueue,
            scopeFactory);

        return (service, context);
    }

    [Fact]
    public async SystemTask GetAsync_WhenMultipleTasksExist_ReturnsAllTasks()
    {
        // Arrange
        var entities = _fixture.CreateMany<TaskEntity>(3).ToList();
        var (service, _) = CreateService(tasks: entities);

        // Act
        var result = await service.GetAsync(CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(3);
    }

    [Fact]
    public async SystemTask GetAsync_ByIdWhenTaskExists_ReturnsMappedTask()
    {
        // Arrange
        var entity = _fixture.Create<TaskEntity>();
        var entities = new List<TaskEntity> { entity };
        var (service, _) = CreateService(tasks: entities);

        // Act
        var result = await service.GetAsync(entity.Id, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(entity.Id);
        result.Name.ShouldBe(entity.Name);
    }

    [Fact]
    public async SystemTask GetByScenarioIdAsync_WhenScenarioIdMatches_FiltersCorrectly()
    {
        // Arrange
        var scenarioId = Guid.NewGuid();
        var matching = _fixture.Build<TaskEntity>()
            .Without(x => x.ScenarioTemplate)
            .Without(x => x.Scenario)
            .Without(x => x.TriggerTask)
            .Without(x => x.Children)
            .Without(x => x.Results)
            .With(x => x.ScenarioId, scenarioId)
            .CreateMany(2).ToList();
        var nonMatching = _fixture.Create<TaskEntity>();
        var all = matching.Concat(new[] { nonMatching }).ToList();
        var (service, _) = CreateService(tasks: all);

        // Act
        var result = await service.GetByScenarioIdAsync(scenarioId, CancellationToken.None);

        // Assert
        result.Count().ShouldBe(2);
        result.ShouldAllBe(t => t.ScenarioId == scenarioId);
    }

    [Fact]
    public async SystemTask DeleteAsync_WhenTaskExists_RemovesTaskAndReturnsTrue()
    {
        // Arrange
        var entity = _fixture.Create<TaskEntity>();
        var entities = new List<TaskEntity> { entity };
        var (service, context) = CreateService(tasks: entities);

        // Act
        var result = await service.DeleteAsync(entity.Id, CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
        var deletedTask = await context.Tasks.FindAsync(entity.Id);
        deletedTask.ShouldBeNull();
    }

    [Fact]
    public async SystemTask GetSubtasksAsync_WhenChildTasksExist_ReturnsChildTasks()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var children = _fixture.Build<TaskEntity>()
            .Without(x => x.ScenarioTemplate)
            .Without(x => x.Scenario)
            .Without(x => x.TriggerTask)
            .Without(x => x.Children)
            .Without(x => x.Results)
            .With(x => x.TriggerTaskId, parentId)
            .CreateMany(2).ToList();
        var unrelated = _fixture.Create<TaskEntity>();
        var all = children.Concat(new[] { unrelated }).ToList();
        var (service, _) = CreateService(tasks: all);

        // Act
        var result = await service.GetSubtasksAsync(parentId, CancellationToken.None);

        // Assert
        result.Count().ShouldBe(2);
    }
}
