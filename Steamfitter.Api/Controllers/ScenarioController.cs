// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using STT = System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Steamfitter.Api.Data;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Infrastructure.Exceptions;
using Steamfitter.Api.Services;
using SAVM = Steamfitter.Api.ViewModels;
using Swashbuckle.AspNetCore.Annotations;

namespace Steamfitter.Api.Controllers
{
    public class ScenarioController : BaseController
    {
        private readonly IScenarioService _ScenarioService;
        private readonly ISteamfitterAuthorizationService _authorizationService;

        public ScenarioController(IScenarioService ScenarioService, ISteamfitterAuthorizationService authorizationService)
        {
            _ScenarioService = ScenarioService;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Gets all Scenarios in the system
        /// </summary>
        /// <remarks>
        /// Returns a list of all of the Scenarios in the system.
        /// <para />
        /// Only accessible to a SuperUser
        /// </remarks>
        /// <returns></returns>
        [HttpGet("Scenarios")]
        [ProducesResponseType(typeof(IEnumerable<SAVM.Scenario>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getScenarios")]
        public async STT.Task<IActionResult> Get(CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync([SystemPermission.ViewScenarios], ct))
                throw new ForbiddenException();

            var list = await _ScenarioService.GetAsync(ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets all Scenarios with the specified ViewId
        /// </summary>
        /// <remarks>
        /// Returns a list of all of the Scenarios in the specified View.
        /// <para />
        /// Only accessible to a SuperUser
        /// </remarks>
        /// <returns></returns>
        [HttpGet("Scenarios/view/{viewId}")]
        [ProducesResponseType(typeof(IEnumerable<SAVM.Scenario>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getScenariosByViewId")]
        public async STT.Task<IActionResult> GetByViewId(Guid viewId, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync([SystemPermission.ViewScenarios], ct))
                throw new ForbiddenException();

            var list = await _ScenarioService.GetByViewIdAsync(viewId, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets a specific Scenario by id
        /// </summary>
        /// <remarks>
        /// Returns the Scenario with the id specified
        /// <para />
        /// Accessible to a SuperUser or a User that is a member of a Team within the specified Scenario
        /// </remarks>
        /// <param name="id">The id of the Scenario</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("Scenarios/{id}")]
        [ProducesResponseType(typeof(SAVM.Scenario), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getScenario")]
        public async STT.Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync<SAVM.Scenario>(id, [SystemPermission.ViewScenarios], [ScenarioPermission.ViewScenario], ct))
                throw new ForbiddenException();

            var Scenario = await _ScenarioService.GetAsync(id, ct);

            return Ok(Scenario);
        }

        /// <summary>
        /// Gets the personal Scenario for the current user
        /// </summary>
        /// <remarks>
        /// Returns the current user's personal Scenario
        /// <para />
        /// Accessible to an authenticated User
        /// </remarks>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("Scenarios/me")]
        [ProducesResponseType(typeof(SAVM.Scenario), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getMyScenario")]
        public async STT.Task<IActionResult> GetMine(CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync([SystemPermission.ManageTasks], ct))
                throw new ForbiddenException();

            var Scenario = await _ScenarioService.GetMineAsync(ct);

            if (Scenario == null)
                throw new EntityNotFoundException<SAVM.Scenario>();

            return Ok(Scenario);
        }

        /// <summary>
        /// Creates a new Scenario
        /// </summary>
        /// <remarks>
        /// Creates a new Scenario with the attributes specified
        /// <para />
        /// Accessible only to a SuperUser or an Administrator
        /// </remarks>
        /// <param name="scenarioForm">The data to create the Scenario with</param>
        /// <param name="ct"></param>
        [HttpPost("Scenarios")]
        [ProducesResponseType(typeof(SAVM.Scenario), (int)HttpStatusCode.Created)]
        [SwaggerOperation(OperationId = "createScenario")]
        public async STT.Task<IActionResult> Create([FromBody] SAVM.ScenarioForm scenarioForm, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync([SystemPermission.CreateScenarios], ct))
                throw new ForbiddenException();

            var createdScenario = await _ScenarioService.CreateAsync(scenarioForm, ct);
            return CreatedAtAction(nameof(this.Get), new { id = createdScenario.Id }, createdScenario);
        }

        /// <summary>
        /// Creates a new Scenario from a ScenarioTemplate
        /// </summary>
        /// <remarks>
        /// Creates a new Scenario from the specified ScenarioTemplate
        /// <para />
        /// Accessible only to a SuperUser or an Administrator
        /// </remarks>
        /// <param name="id">The ScenarioTemplate ID to create the Scenario with</param>
        /// <param name="options"></param>
        /// <param name="ct"></param>
        [HttpPost("ScenarioTemplates/{id}/Scenarios")]
        [ProducesResponseType(typeof(SAVM.Scenario), (int)HttpStatusCode.Created)]
        [SwaggerOperation(OperationId = "createScenarioFromScenarioTemplate")]
        public async STT.Task<IActionResult> CreateFromScenarioTemplate(Guid id, [FromBody] SAVM.ScenarioCloneOptions options, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync([SystemPermission.CreateScenarios], ct)
                || !await _authorizationService.AuthorizeAsync<SAVM.ScenarioTemplate>(id, [SystemPermission.ViewScenarioTemplates], [ScenarioTemplatePermission.ViewScenarioTemplate], ct))
                throw new ForbiddenException();

            var createdScenario = await _ScenarioService.CreateFromScenarioTemplateAsync(id, options, ct);
            return CreatedAtAction(nameof(this.Get), new { id = createdScenario.Id }, createdScenario);
        }

        /// <summary>
        /// Creates a new Scenario from a Scenario
        /// </summary>
        /// <remarks>
        /// Creates a new Scenario from the specified Scenario
        /// <para />
        /// Accessible only to a SuperUser or an Administrator
        /// </remarks>
        /// <param name="id">The Scenario ID to copy into a new Scenario</param>
        /// <param name="ct"></param>
        [HttpPost("Scenarios/{id}/Copy")]
        [ProducesResponseType(typeof(SAVM.Scenario), (int)HttpStatusCode.Created)]
        [SwaggerOperation(OperationId = "copyScenario")]
        public async STT.Task<IActionResult> CopyScenario(Guid id, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync([SystemPermission.CreateScenarios], ct)
                || !await _authorizationService.AuthorizeAsync<SAVM.Scenario>(id, [SystemPermission.ViewScenarioTemplates], [ScenarioPermission.ViewScenario], ct))
                throw new ForbiddenException();

            var createdScenario = await _ScenarioService.CreateFromScenarioAsync(id, ct);
            return CreatedAtAction(nameof(this.Get), new { id = createdScenario.Id }, createdScenario);
        }

        /// <summary>
        /// Updates a Scenario
        /// </summary>
        /// <remarks>
        /// Updates a Scenario with the attributes specified
        /// <para />
        /// Accessible only to a SuperUser or a User on an Admin Team within the specified Scenario
        /// </remarks>
        /// <param name="id">The Id of the Exericse to update</param>
        /// <param name="scenarioForm">The updated Scenario values</param>
        /// <param name="ct"></param>
        [HttpPut("Scenarios/{id}")]
        [ProducesResponseType(typeof(SAVM.Scenario), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "updateScenario")]
        public async STT.Task<IActionResult> Update([FromRoute] Guid id, [FromBody] SAVM.ScenarioForm scenarioForm, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync<SAVM.Scenario>(id, [SystemPermission.EditScenarios], [ScenarioPermission.EditScenario], ct))
                throw new ForbiddenException();

            var updatedScenario = await _ScenarioService.UpdateAsync(id, scenarioForm, ct);
            return Ok(updatedScenario);
        }

        /// <summary>
        /// Start a Scenario
        /// </summary>
        /// <remarks>
        /// Updates a Scenario to active and executes initial Tasks
        /// <para />
        /// Accessible only to a SuperUser or a User on an Admin Team within the specified Scenario
        /// </remarks>
        /// <param name="id">The Id of the Scenario to update</param>
        /// <param name="ct"></param>
        [HttpPut("Scenarios/{id}/start")]
        [ProducesResponseType(typeof(SAVM.Scenario), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "startScenario")]
        public async STT.Task<IActionResult> Start([FromRoute] Guid id, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync<SAVM.Scenario>(id, [SystemPermission.ExecuteScenarios], [ScenarioPermission.ExecuteScenario], ct))
                throw new ForbiddenException();

            var updatedScenario = await _ScenarioService.StartAsync(id, ct);
            return Ok(updatedScenario);
        }

        /// <summary>
        /// Pause a Scenario
        /// </summary>
        /// <remarks>
        /// Updates a Scenario to paused
        /// <para />
        /// Accessible only to a SuperUser or a User on an Admin Team within the specified Scenario
        /// </remarks>
        /// <param name="id">The Id of the Scenario to update</param>
        /// <param name="ct"></param>
        [HttpPut("Scenarios/{id}/pause")]
        [ProducesResponseType(typeof(SAVM.Scenario), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "pauseScenario")]
        public async STT.Task<IActionResult> Pause([FromRoute] Guid id, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync<SAVM.Scenario>(id, [SystemPermission.ExecuteScenarios], [ScenarioPermission.ExecuteScenario], ct))
                throw new ForbiddenException();

            var updatedScenario = await _ScenarioService.PauseAsync(id, ct);
            return Ok(updatedScenario);
        }

        /// <summary>
        /// Continue a Scenario
        /// </summary>
        /// <remarks>
        /// Updates a Scenario to active
        /// <para />
        /// Accessible only to a SuperUser or a User on an Admin Team within the specified Scenario
        /// </remarks>
        /// <param name="id">The Id of the Scenario to update</param>
        /// <param name="ct"></param>
        [HttpPut("Scenarios/{id}/continue")]
        [ProducesResponseType(typeof(SAVM.Scenario), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "continueScenario")]
        public async STT.Task<IActionResult> Continue([FromRoute] Guid id, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync<SAVM.Scenario>(id, [SystemPermission.ExecuteScenarios], [ScenarioPermission.ExecuteScenario], ct))
                throw new ForbiddenException();

            var updatedScenario = await _ScenarioService.ContinueAsync(id, ct);
            return Ok(updatedScenario);
        }

        /// <summary>
        /// End a Scenario
        /// </summary>
        /// <remarks>
        /// Updates a Scenario to ended
        /// <para />
        /// Accessible only to a SuperUser or a User on an Admin Team within the specified Scenario
        /// </remarks>
        /// <param name="id">The Id of the Scenario to update</param>
        /// <param name="ct"></param>
        [HttpPut("Scenarios/{id}/end")]
        [ProducesResponseType(typeof(SAVM.Scenario), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "endScenario")]
        public async STT.Task<IActionResult> End([FromRoute] Guid id, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync<SAVM.Scenario>(id, [SystemPermission.ExecuteScenarios], [ScenarioPermission.ExecuteScenario], ct))
                throw new ForbiddenException();

            var updatedScenario = await _ScenarioService.EndAsync(id, ct);
            return Ok(updatedScenario);
        }

        /// <summary>
        /// Deletes a Scenario
        /// </summary>
        /// <remarks>
        /// Deletes a Scenario with the specified id
        /// <para />
        /// Accessible only to a SuperUser or a User on an Admin Team within the specified Scenario
        /// </remarks>
        /// <param name="id">The id of the Scenario to delete</param>
        /// <param name="ct"></param>
        [HttpDelete("Scenarios/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteScenario")]
        public async STT.Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync<SAVM.Scenario>(id, [SystemPermission.ManageScenarios], [ScenarioPermission.ManageScenario], ct))
                throw new ForbiddenException();

            await _ScenarioService.DeleteAsync(id, ct);
            return NoContent();
        }

    }
}
