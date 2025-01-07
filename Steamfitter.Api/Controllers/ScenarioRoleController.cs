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

public class ScenarioRolesController : BaseController
{
    private readonly ISteamfitterAuthorizationService _authorizationService;
    private readonly IScenarioRoleService _scenarioRoleService;

    public ScenarioRolesController(ISteamfitterAuthorizationService authorizationService, IScenarioRoleService scenarioRoleService)
    {
        _authorizationService = authorizationService;
        _scenarioRoleService = scenarioRoleService;
    }

    /// <summary>
    /// Get a single ScenarioRole.
    /// </summary>
    /// <param name="id">ID of a ScenarioRole.</param>
    /// <returns></returns>
    [HttpGet("scenario-roles/{id}")]
    [ProducesResponseType(typeof(ScenarioRole), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "GetScenarioRole")]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken ct)
    {
        if (!await _authorizationService.AuthorizeAsync([SystemPermission.ViewRoles], ct))
            throw new ForbiddenException();

        var result = await _scenarioRoleService.GetAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get all ScenarioRoles.
    /// </summary>
    /// <returns></returns>
    [HttpGet("scenario-roles")]
    [ProducesResponseType(typeof(IEnumerable<ScenarioRole>), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "GetAllScenarioRoles")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _scenarioRoleService.GetAsync(ct);
        return Ok(result);
    }
}