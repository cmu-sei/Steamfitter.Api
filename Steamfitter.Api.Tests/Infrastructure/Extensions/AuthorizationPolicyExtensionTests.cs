// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Steamfitter.Api.Data;
using Steamfitter.Api.Tests.Support;

namespace Steamfitter.Api.Tests.Infrastructure.Extensions;

/// <summary>
/// The scopes every controller requires: an authenticated user whose token carries each scope of
/// <c>Authorization:AuthorizationScope</c> ("steamfitter player player-vm" as shipped). Every controller
/// takes them twice, through <c>BaseController</c>'s <c>[Authorize]</c> (the default policy
/// <c>AuthorizationPolicyExtensions.AddAuthorizationPolicy</c> builds) and through the global
/// <c>AuthorizeFilter</c> <c>Startup.ConfigureServices</c> adds to MVC; <c>EngineHub</c> takes the default
/// policy only (<c>EngineHubConnectionTests</c>).
/// </summary>
/// <remarks>
/// The policy's refusal is a 403 with no body, where the controllers' own permission checks answer a 403
/// through <c>JsonExceptionFilter</c> with a JSON body; the empty body tells the two apart.
/// </remarks>
public class AuthorizationPolicyExtensionTests(DatabaseFixture fixture, SteamfitterAppFactory factory) : ApiTestBase(fixture, factory)
{
    /// <summary>The shipped scopes without "steamfitter".</summary>
    private const string WithoutSteamfitter = "player player-vm";

    /// <summary>The shipped scopes without "player".</summary>
    private const string WithoutPlayer = "steamfitter player-vm";

    /// <summary>The shipped scopes without "player-vm".</summary>
    private const string WithoutPlayerVm = "steamfitter player";

    [Fact]
    public async Task A_request_whose_token_lacks_the_steamfitter_scope_is_forbidden_but_allowed_with_it()
    {
        var user = TestData.User(name: "Scoped");
        await Seed(user);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewUsers).SeedAsync();

        using var refused = await Client(actor).SendAsync(GetUser(user.Id, WithoutSteamfitter), Ct);

        await AssertStatus(HttpStatusCode.Forbidden, refused);
        Assert.Empty(await refused.Content.ReadAsStringAsync(Ct));
        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync($"api/users/{user.Id}", Ct));
    }

    [Fact]
    public async Task A_request_whose_token_lacks_the_player_scope_is_forbidden_but_allowed_with_it()
    {
        var user = TestData.User(name: "Scoped");
        await Seed(user);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewUsers).SeedAsync();

        using var refused = await Client(actor).SendAsync(GetUser(user.Id, WithoutPlayer), Ct);

        await AssertStatus(HttpStatusCode.Forbidden, refused);
        Assert.Empty(await refused.Content.ReadAsStringAsync(Ct));
        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync($"api/users/{user.Id}", Ct));
    }

    [Fact]
    public async Task A_request_whose_token_lacks_the_player_vm_scope_is_forbidden_but_allowed_with_it()
    {
        var user = TestData.User(name: "Scoped");
        await Seed(user);
        var actor = await Actor().WithSystemPermissions(SystemPermission.ViewUsers).SeedAsync();

        using var refused = await Client(actor).SendAsync(GetUser(user.Id, WithoutPlayerVm), Ct);

        await AssertStatus(HttpStatusCode.Forbidden, refused);
        Assert.Empty(await refused.Content.ReadAsStringAsync(Ct));
        await AssertStatus(HttpStatusCode.OK, await Client(actor).GetAsync($"api/users/{user.Id}", Ct));
    }

    /// <summary><c>GET api/users/{id}</c> with a token carrying only <paramref name="scopes"/>.</summary>
    private static HttpRequestMessage GetUser(System.Guid id, string scopes)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"api/users/{id}");
        request.Headers.Add(TestAuthHandler.ScopeHeader, scopes);

        return request;
    }
}
