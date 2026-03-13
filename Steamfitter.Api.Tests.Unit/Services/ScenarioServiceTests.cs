// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Security.Claims;
using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using AutoMapper;
using Crucible.Common.Testing.Fixtures;
using FakeItEasy;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Infrastructure.Mappings;
using Steamfitter.Api.Services;
using Steamfitter.Api.Tests.Shared.Fixtures;
using TUnit.Core;

namespace Steamfitter.Api.Tests.Unit.Services;

[Category("Unit")]
public class ScenarioServiceTests
{
    private readonly IFixture _fixture;
    private readonly IMapper _mapper;

    public ScenarioServiceTests()
    {
        _fixture = new Fixture().Customize(new SteamfitterCustomization())
                                .Customize(new AutoFakeItEasyCustomization());

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ScenarioProfile>();
        });
        _mapper = config.CreateMapper();
    }

    private (ScenarioService service, SteamfitterContext context) CreateService(
        Guid? userId = null,
        List<ScenarioEntity>? scenarios = null)
    {
        var uid = userId ?? Guid.NewGuid();
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", uid.ToString()),
            new Claim("name", "Test User")
        }, "TestAuth"));

        var context = TestDbContextFactory.Create<SteamfitterContext>();
        var taskService = A.Fake<ITaskService>();
        var stackStormService = A.Fake<IStackStormService>();
        var xApiService = A.Fake<IXApiService>();

        if (scenarios != null)
        {
            context.Scenarios.AddRange(scenarios);
            context.SaveChanges();
        }

        var service = new ScenarioService(
            context,
            claims,
            _mapper,
            taskService,
            stackStormService,
            xApiService);

        return (service, context);
    }

    [Test]
    public async System.Threading.Tasks.Task GetAsync_WhenMultipleScenariosExist_ReturnsAllScenarios()
    {
        // Arrange
        var entities = _fixture.CreateMany<ScenarioEntity>(3).ToList();
        var (service, _) = CreateService(scenarios: entities);

        // Act
        var result = await service.GetAsync(CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count()).IsEqualTo(3);
    }

    [Test]
    public async System.Threading.Tasks.Task GetAsync_ByIdWhenScenarioExists_ReturnsMappedScenario()
    {
        // Arrange
        var entity = _fixture.Create<ScenarioEntity>();
        var entities = new List<ScenarioEntity> { entity };
        var (service, _) = CreateService(scenarios: entities);

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
        var (service, _) = CreateService(scenarios: new List<ScenarioEntity>());

        // Act
        var result = await service.GetAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async System.Threading.Tasks.Task DeleteAsync_WhenScenarioExists_RemovesScenarioAndReturnsTrue()
    {
        // Arrange
        var entity = _fixture.Create<ScenarioEntity>();
        var entities = new List<ScenarioEntity> { entity };
        var (service, context) = CreateService(scenarios: entities);

        // Act
        var result = await service.DeleteAsync(entity.Id, CancellationToken.None);

        // Assert
        await Assert.That(result).IsTrue();
        var deletedScenario = await context.Scenarios.FindAsync(entity.Id);
        await Assert.That(deletedScenario).IsNull();
    }

    [Test]
    public async System.Threading.Tasks.Task GetByViewIdAsync_WhenViewIdMatches_FiltersCorrectly()
    {
        // Arrange
        var viewId = Guid.NewGuid();
        var matching = _fixture.Build<ScenarioEntity>()
            .Without(x => x.Tasks)
            .Without(x => x.ScenarioTemplate)
            .Without(x => x.VmCredentials)
            .Without(x => x.Memberships)
            .With(x => x.ViewId, viewId)
            .CreateMany(2).ToList();
        var nonMatching = _fixture.Create<ScenarioEntity>();
        var all = matching.Concat(new[] { nonMatching }).ToList();
        var (service, _) = CreateService(scenarios: all);

        // Act
        var result = await service.GetByViewIdAsync(viewId, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count()).IsEqualTo(2);
        foreach (var scenario in result)
        {
            await Assert.That(scenario.ViewId).IsEqualTo(viewId);
        }
    }
}
