// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

// Steamfitter: hosts are built over the test's Session (see ApiTestHost) rather than over Db.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Steamfitter.Api.Data;

namespace Steamfitter.Api.Tests.Support;

/// <summary>Base class for tests that resolve a service out of a container built for one principal.</summary>
/// <remarks>
/// A host is built per principal and reused. Its contexts are scoped and come from the test's own
/// session, so re-read through <c>NewContext()</c> as in any other test. Substitutes and recorders in an
/// <see cref="ApiTestHost"/> belong to the test that built it.
/// </remarks>
public abstract class ServiceTestBase(DatabaseFixture fixture) : DatabaseTestBase(fixture)
{
    private readonly Dictionary<ClaimsPrincipal, ApiTestHost> _hosts = [];
    private ClaimsPrincipal _root;

    /// <summary>A principal holding every system permission.</summary>
    protected ClaimsPrincipal Root => _root ??= new ClaimsPrincipalBuilder()
        .WithSystemPermissions(Enum.GetValues<SystemPermission>())
        .Build();

    protected ApiTestHost RootHost => HostFor(Root);

    /// <summary>
    /// The host for <paramref name="user"/>. Asking for options against a principal already hosted
    /// throws, because they could not take effect; build a principal per configuration.
    /// </summary>
    protected ApiTestHost HostFor(ClaimsPrincipal user, Action<ApiTestHostOptions> configure = null)
    {
        if (_hosts.TryGetValue(user, out var host))
        {
            if (configure != null)
            {
                throw new InvalidOperationException(
                    "A host for this principal has already been built, so these options would be " +
                    "ignored. Build a distinct principal for each configuration.");
            }

            return host;
        }

        host = ApiTestHost.Create(Session, user, configure);
        _hosts.Add(user, host);

        return host;
    }

    public override async ValueTask DisposeAsync()
    {
        foreach (var host in _hosts.Values)
        {
            host.Dispose();
        }

        await base.DisposeAsync();
    }
}
