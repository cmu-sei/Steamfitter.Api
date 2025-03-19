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

public class ScenarioTemplatePermissionsController : BaseController
{
    private readonly ISteamfitterAuthorizationService _authorizationService;

    public ScenarioTemplatePermissionsController(ISteamfitterAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Get all SystemPermissions for the calling User.
    /// </summary>
    /// <returns></returns>
    [HttpGet("scenarioTemplates/{id}/me/permissions")]
    [ProducesResponseType(typeof(IEnumerable<ScenarioTemplatePermissionClaim>), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "GetMyScenarioTemplatePermissions")]
    public async Task<IActionResult> GetMine(Guid id)
    {
        var result = _authorizationService.GetScenarioTemplatePermissions();
        return Ok(result);
    }
}