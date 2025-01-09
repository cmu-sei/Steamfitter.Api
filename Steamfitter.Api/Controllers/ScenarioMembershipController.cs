// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using Steamfitter.Api.Infrastructure.Exceptions;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Steamfitter.Api.Data;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Services;
using Steamfitter.Api.ViewModels;
using System.Threading;

namespace Steamfitter.Api.Controllers;

public class ScenarioMembershipsController : BaseController
{
    private readonly ISteamfitterAuthorizationService _authorizationService;
    private readonly IScenarioMembershipService _scenarioMembershipService;

    public ScenarioMembershipsController(ISteamfitterAuthorizationService authorizationService, IScenarioMembershipService scenarioMembershipService)
    {
        _authorizationService = authorizationService;
        _scenarioMembershipService = scenarioMembershipService;
    }

    /// <summary>
    /// Get a single ScenarioMembership.
    /// </summary>
    /// <param name="id">ID of a ScenarioMembership.</param>
    /// <returns></returns>
    [HttpGet("scenarios/memberships/{id}")]
    [ProducesResponseType(typeof(ScenarioMembership), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "GetScenarioMembership")]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken ct)
    {
        if (!await _authorizationService.AuthorizeAsync([SystemPermission.ViewScenarios], ct))
            throw new ForbiddenException();

        var result = await _scenarioMembershipService.GetAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get all ScenarioMemberships.
    /// </summary>
    /// <returns></returns>
    [HttpGet("scenarios/{id}/memberships")]
    [ProducesResponseType(typeof(IEnumerable<ScenarioMembership>), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "GetAllScenarioMemberships")]
    public async Task<IActionResult> GetAll(Guid id, CancellationToken ct)
    {
        var result = await _scenarioMembershipService.GetByScenarioAsync(id, ct);
        return Ok(result);
    }
}