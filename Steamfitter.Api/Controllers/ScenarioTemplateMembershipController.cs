// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
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
using System.Data;

namespace Steamfitter.Api.Controllers;

public class ScenarioTemplateMembershipsController : BaseController
{
    private readonly ISteamfitterAuthorizationService _authorizationService;
    private readonly IScenarioTemplateMembershipService _scenarioTemplateMembershipService;

    public ScenarioTemplateMembershipsController(ISteamfitterAuthorizationService authorizationService, IScenarioTemplateMembershipService scenarioTemplateMembershipService)
    {
        _authorizationService = authorizationService;
        _scenarioTemplateMembershipService = scenarioTemplateMembershipService;
    }

    /// <summary>
    /// Get a single ScenarioTemplateMembership.
    /// </summary>
    /// <param name="id">ID of a ScenarioTemplateMembership.</param>
    /// <returns></returns>
    [HttpGet("scenarioTemplates/memberships/{id}")]
    [ProducesResponseType(typeof(ScenarioTemplateMembership), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "GetScenarioTemplateMembership")]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _scenarioTemplateMembershipService.GetAsync(id, ct);
        if (!await _authorizationService.AuthorizeAsync<ScenarioTemplate>(result.ScenarioTemplateId, [SystemPermission.ViewScenarioTemplates], [ScenarioTemplatePermission.ViewScenarioTemplate], ct))
            throw new ForbiddenException();

        return Ok(result);
    }

    /// <summary>
    /// Get all ScenarioTemplateMemberships.
    /// </summary>
    /// <returns></returns>
    [HttpGet("scenarioTemplates/{id}/memberships")]
    [ProducesResponseType(typeof(IEnumerable<ScenarioTemplateMembership>), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "GetAllScenarioTemplateMemberships")]
    public async Task<IActionResult> GetAll(Guid id, CancellationToken ct)
    {
        if (!await _authorizationService.AuthorizeAsync<ScenarioTemplate>(id, [SystemPermission.ViewScenarioTemplates], [ScenarioTemplatePermission.ViewScenarioTemplate], ct))
            throw new ForbiddenException();

        var result = await _scenarioTemplateMembershipService.GetByScenarioTemplateAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Create a new ScenarioTemplate Membership.
    /// </summary>
    /// <param name="scenarioTemplateId"></param>
    /// <param name="scenarioTemplateMembership"></param>
    /// <returns></returns>
    [HttpPost("scenarioTemplates/{scenarioTemplateId}/memberships")]
    [ProducesResponseType(typeof(ScenarioTemplateMembership), (int)HttpStatusCode.Created)]
    [SwaggerOperation(OperationId = "CreateScenarioTemplateMembership")]
    public async Task<IActionResult> CreateMembership([FromRoute] Guid scenarioTemplateId, ScenarioTemplateMembership scenarioTemplateMembership, CancellationToken ct)
    {
        if (!await _authorizationService.AuthorizeAsync<ScenarioTemplate>(scenarioTemplateId, [SystemPermission.ManageScenarioTemplates], [ScenarioTemplatePermission.ManageScenarioTemplate], ct))
            throw new ForbiddenException();

        if (scenarioTemplateMembership.ScenarioTemplateId != scenarioTemplateId)
            throw new DataException("The ScenarioTemplateId of the membership must match the ScenarioTemplateId of the URL.");

        var result = await _scenarioTemplateMembershipService.CreateAsync(scenarioTemplateMembership, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    /// <summary>
    /// Updates a ScenarioTemplateMembership
    /// </summary>
    /// <remarks>
    /// Updates a ScenarioTemplateMembership with the attributes specified
    /// </remarks>
    /// <param name="id">The Id of the Exericse to update</param>
    /// <param name="scenarioTemplateMembership">The updated ScenarioTemplateMembership values</param>
    /// <param name="ct"></param>
    [HttpPut("ScenarioTemplates/Memberships/{id}")]
    [ProducesResponseType(typeof(ScenarioTemplateMembership), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "updateScenarioTemplateMembership")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] ScenarioTemplateMembership scenarioTemplateMembership, CancellationToken ct)
    {
        var membership = await _scenarioTemplateMembershipService.GetAsync(id, ct);
        if (!await _authorizationService.AuthorizeAsync<ScenarioTemplate>(membership.ScenarioTemplateId, [SystemPermission.ManageScenarioTemplates], [ScenarioTemplatePermission.ManageScenarioTemplate], ct))
            throw new ForbiddenException();

        var updatedScenarioTemplateMembership = await _scenarioTemplateMembershipService.UpdateAsync(id, scenarioTemplateMembership, ct);
        return Ok(updatedScenarioTemplateMembership);
    }

    /// <summary>
    /// Delete a ScenarioTemplate Membership.
    /// </summary>
    /// <returns></returns>
    [HttpDelete("scenarioTemplates/memberships/{id}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [SwaggerOperation(OperationId = "DeleteScenarioTemplateMembership")]
    public async Task<IActionResult> DeleteMembership([FromRoute] Guid id, CancellationToken ct)
    {
        var membership = await _scenarioTemplateMembershipService.GetAsync(id, ct);
        if (!await _authorizationService.AuthorizeAsync<ScenarioTemplate>(membership.ScenarioTemplateId, [SystemPermission.ManageScenarioTemplates], [ScenarioTemplatePermission.ManageScenarioTemplate], ct))
            throw new ForbiddenException();

        await _scenarioTemplateMembershipService.DeleteAsync(id, ct);
        return NoContent();
    }


}
