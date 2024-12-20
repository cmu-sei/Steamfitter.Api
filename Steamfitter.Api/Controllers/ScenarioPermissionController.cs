// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Steamfitter.Api.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;

namespace Steamfitter.Api.Controllers;

public class ScenarioPermissionsController : ControllerBase
{
    private readonly ISteamfitterAuthorizationService _authorizationService;

    public ScenarioPermissionsController(ISteamfitterAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Get all SystemPermissions for the calling User.
    /// </summary>
    /// <returns></returns>
    [HttpGet("scenarios/{id}/me/permissions")]
    [ProducesResponseType(typeof(IEnumerable<ScenarioPermissionClaim>), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "GetMyScenarioPermissions")]
    public async Task<IActionResult> GetMine(Guid id)
    {
        var result = _authorizationService.GetScenarioPermissions();
        return Ok(result);
    }
}