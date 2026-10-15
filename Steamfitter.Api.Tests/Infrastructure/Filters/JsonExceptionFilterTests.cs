// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Steamfitter.Api.Infrastructure.Exceptions;
using Steamfitter.Api.Infrastructure.Filters;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Tests.Infrastructure.Filters;

/// <summary><c>JsonExceptionFilter</c>: the status and body each exception an action throws is answered with.</summary>
public class JsonExceptionFilterTests
{
    [Fact]
    public void An_api_exception_is_answered_with_its_status_and_its_message_as_the_title()
    {
        var result = Filter("Production", new EntityNotFoundException<SAVM.Scenario>());

        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal((404, 404, "Scenario not found", null), (result.StatusCode, problem.Status, problem.Title, problem.Detail));
    }

    [Fact]
    public void Any_other_exception_in_production_is_a_500_with_the_message_as_the_detail()
    {
        var result = Filter("Production", new InvalidOperationException("boom"));

        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal((500, "A server error occurred.", "boom"), (result.StatusCode, problem.Title, problem.Detail));
    }

    [Fact]
    public void Any_other_exception_in_development_is_a_500_with_the_message_as_the_title()
    {
        var result = Filter("Development", new InvalidOperationException("boom"));

        Assert.Equal("boom", Assert.IsType<ProblemDetails>(result.Value).Title);
    }

    private static JsonResult Filter(string environment, Exception exception)
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(environment);
        var context = new ExceptionContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>())
        {
            Exception = exception
        };

        new JsonExceptionFilter(env).OnException(context);

        return Assert.IsType<JsonResult>(context.Result);
    }
}
