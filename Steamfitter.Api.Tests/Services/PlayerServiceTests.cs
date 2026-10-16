// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Player.Api.Client;
using Steamfitter.Api.Services;
using Crucible.Api.Testing;

namespace Steamfitter.Api.Tests.Services;

/// <summary><c>PlayerService.CanManageViewAsync</c> over a Player client whose HTTP this test owns.</summary>
public class PlayerServiceTests
{
    private readonly Guid _viewId = Guid.NewGuid();
    private readonly StubHttpMessageHandler _player = new StubHttpMessageHandler().RefusesUnmatched();

    [Fact]
    public async Task ManageView_on_the_view_grants()
    {
        _player.AnswersJson("GET api/team-permissions/mine", $$"""[{"viewId":"{{_viewId}}","teamId":"{{Guid.NewGuid()}}","permissionValues":["ViewView","ManageView"]}]""");

        Assert.True(await Service().CanManageViewAsync(_viewId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ManageView_on_another_view_does_not_grant()
    {
        _player.AnswersJson("GET api/team-permissions/mine", $$"""[{"viewId":"{{Guid.NewGuid()}}","teamId":"{{Guid.NewGuid()}}","permissionValues":["ManageView"]}]""");
        _player.AnswersJson("GET api/permissions/mine", "[]");

        Assert.False(await Service().CanManageViewAsync(_viewId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task The_ManageViews_system_permission_grants_without_a_view_permission()
    {
        _player.AnswersJson("GET api/team-permissions/mine", "[]");
        _player.AnswersJson("GET api/permissions/mine", """["ViewViews","ManageViews"]""");

        Assert.True(await Service().CanManageViewAsync(_viewId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Player_refusing_both_calls_grants_nothing()
    {
        _player.Answers("GET api/team-permissions/mine", HttpStatusCode.Forbidden);
        _player.Answers("GET api/permissions/mine", HttpStatusCode.InternalServerError);

        Assert.False(await Service().CanManageViewAsync(_viewId, TestContext.Current.CancellationToken));
    }

    private PlayerService Service() =>
        new(new PlayerApiClient(new HttpClient(_player) { BaseAddress = new Uri("http://player.test/") }));
}
