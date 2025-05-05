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
        var result = await _scenarioMembershipService.GetAsync(id, ct);
        if (!await _authorizationService.AuthorizeAsync<Scenario>(result.ScenarioId, [SystemPermission.ViewScenarios], [ScenarioPermission.ViewScenario], ct))
            throw new ForbiddenException();

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
        if (!await _authorizationService.AuthorizeAsync<Scenario>(id, [SystemPermission.ViewScenarios], [ScenarioPermission.ViewScenario], ct))
            throw new ForbiddenException();

        var result = await _scenarioMembershipService.GetByScenarioAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Create a new Scenario Membership.
    /// </summary>
    /// <param name="scenarioId"></param>
    /// <param name="scenarioMembership"></param>
    /// <returns></returns>
    [HttpPost("scenarios/{scenarioId}/memberships")]
    [ProducesResponseType(typeof(ScenarioMembership), (int)HttpStatusCode.Created)]
    [SwaggerOperation(OperationId = "CreateScenarioMembership")]
    public async Task<IActionResult> CreateMembership([FromRoute] Guid scenarioId, ScenarioMembership scenarioMembership, CancellationToken ct)
    {
        if (!await _authorizationService.AuthorizeAsync<Scenario>(scenarioMembership.ScenarioId, [SystemPermission.ManageScenarios], [ScenarioPermission.ManageScenario], ct))
            throw new ForbiddenException();

        var result = await _scenarioMembershipService.CreateAsync(scenarioMembership, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    /// <summary>
    /// Updates a ScenarioMembership
    /// </summary>
    /// <remarks>
    /// Updates a ScenarioMembership with the attributes specified
    /// </remarks>
    /// <param name="id">The Id of the Exericse to update</param>
    /// <param name="scenarioMembership">The updated ScenarioMembership values</param>
    /// <param name="ct"></param>
    [HttpPut("Scenarios/Memberships/{id}")]
    [ProducesResponseType(typeof(ScenarioMembership), (int)HttpStatusCode.OK)]
    [SwaggerOperation(OperationId = "updateScenarioMembership")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] ScenarioMembership scenarioMembership, CancellationToken ct)
    {
        if (!await _authorizationService.AuthorizeAsync<ScenarioMembership>(scenarioMembership.ScenarioId, [SystemPermission.ManageScenarios], [ScenarioPermission.ManageScenario], ct))
            throw new ForbiddenException();

        var updatedScenarioMembership = await _scenarioMembershipService.UpdateAsync(id, scenarioMembership, ct);
        return Ok(updatedScenarioMembership);
    }

    /// <summary>
    /// Delete a Scenario Membership.
    /// </summary>
    /// <returns></returns>
    [HttpDelete("scenarios/memberships/{id}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [SwaggerOperation(OperationId = "DeleteScenarioMembership")]
    public async Task<IActionResult> DeleteMembership([FromRoute] Guid id, CancellationToken ct)
    {
        var scenarioMembership = await _scenarioMembershipService.GetAsync(id, ct);
        if (!await _authorizationService.AuthorizeAsync<Scenario>(scenarioMembership.ScenarioId, [SystemPermission.ManageScenarios], [ScenarioPermission.ManageScenario], ct))
            throw new ForbiddenException();

        await _scenarioMembershipService.DeleteAsync(id, ct);
        return NoContent();
    }


}
