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

public class ScenarioTemplateRolesController : BaseController
{
    private readonly ISteamfitterAuthorizationService _authorizationService;
    private readonly IScenarioTemplateRoleService _scenarioRoleService;

    public ScenarioTemplateRolesController(ISteamfitterAuthorizationService authorizationService, IScenarioTemplateRoleService scenarioRoleService)
    {
        _authorizationService = authorizationService;
        _scenarioRoleService = scenarioRoleService;
    }

    /// <summary>
    /// Get a single ScenarioTemplateRole.
    /// </summary>
    /// <param name="id">ID of a ScenarioTemplateRole.</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpGet("scenarioTemplate-roles/{id}")]
    [ProducesResponseType(typeof(ScenarioTemplateRole), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "GetScenarioTemplateRole")]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken ct)
    {
        if (!await _authorizationService.AuthorizeAsync([SystemPermission.ViewRoles], ct))
            throw new ForbiddenException();

        var result = await _scenarioRoleService.GetAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get all ScenarioTemplateRoles.
    /// </summary>
    /// <returns></returns>
    [HttpGet("scenarioTemplate-roles")]
    [ProducesResponseType(typeof(IEnumerable<ScenarioTemplateRole>), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "GetAllScenarioTemplateRoles")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!await _authorizationService.AuthorizeAsync([SystemPermission.ViewRoles], ct))
            throw new ForbiddenException();

        var result = await _scenarioRoleService.GetAsync(ct);
        return Ok(result);
    }
}
