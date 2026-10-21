// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Net.Http;
using Steamfitter.Api.Infrastructure.Extensions;
using Steamfitter.Api.Infrastructure.Options;

namespace Steamfitter.Api.Tests.Infrastructure.Extensions;

/// <summary>
/// <c>ApiClientsExtensions.GetHttpClient</c>'s timeout: HTTP tasks pass
/// <c>VmTaskProcessing:HttpTimeoutSeconds</c>, and every other caller keeps <c>HttpClient</c>'s own default.
/// </summary>
public class ApiClientsExtensionsTests
{
    private static readonly TimeSpan HttpClientDefault = new HttpClient().Timeout;

    [Fact]
    public void A_positive_timeout_is_applied_to_the_client()
    {
        var client = ApiClientsExtensions.GetHttpClient(Factory(), "http://api.test/", null, 7);

        Assert.Equal(TimeSpan.FromSeconds(7), client.Timeout);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-5)]
    public void No_timeout_or_one_that_is_not_positive_keeps_the_httpclient_default(int? timeoutSeconds)
    {
        var client = ApiClientsExtensions.GetHttpClient(Factory(), "http://api.test/", null, timeoutSeconds);

        Assert.Equal(HttpClientDefault, client.Timeout);
    }

    /// <summary>Under <c>TaskProcessMaxWaitSeconds</c> (120), so the executor has room to retry.</summary>
    [Fact]
    public void The_http_task_timeout_defaults_to_90_seconds()
    {
        Assert.Equal(90, new VmTaskProcessingOptions().HttpTimeoutSeconds);
    }

    private static StubHttpClientFactory Factory() => new(new StubHttpMessageHandler());
}
