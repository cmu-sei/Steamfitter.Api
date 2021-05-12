// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using STT = System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Steamfitter.Api.Infrastructure.Extensions;
using Steamfitter.Api.Infrastructure.Exceptions;
using Steamfitter.Api.Services;
using SAVM = Steamfitter.Api.ViewModels;
using Swashbuckle.AspNetCore.Annotations;

namespace Steamfitter.Api.Controllers
{
    public class ScenarioTemplateController : BaseController
    {
        private readonly IScenarioTemplateService _scenarioTemplateService;
        private readonly IAuthorizationService _authorizationService;

        public ScenarioTemplateController(IScenarioTemplateService scenarioTemplateService, IAuthorizationService authorizationService)
        {
            _scenarioTemplateService = scenarioTemplateService;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Gets all ScenarioTemplate in the system
        /// </summary>
        /// <remarks>
        /// Returns a list of all of the ScenarioTemplates in the system.
        /// <para />
        /// Only accessible to a SuperUser
        /// </remarks>
        /// <returns></returns>
        [HttpGet("scenarioTemplates")]
        [ProducesResponseType(typeof(IEnumerable<SAVM.ScenarioTemplate>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getScenarioTemplates")]
        public async STT.Task<IActionResult> Get(CancellationToken ct)
        {
            var list = await _scenarioTemplateService.GetAsync(ct);
            return Ok(list);
        }

        // /// <summary>
        // /// Gets all ScenarioTemplates for a User
        // /// </summary>
        // /// <remarks>
        // /// Returns all ScenarioTemplates where the specified User is a member of at least one of it's Teams
        // /// <para />
        // /// Accessible to a SuperUser or the specified User itself
        // /// </remarks>
        // /// <returns></returns>
        // [HttpGet("users/{id}/scenarioTemplates")]
        // [ProducesResponseType(typeof(IEnumerable<ScenarioTemplate>), (int)HttpStatusCode.OK)]
        // [SwaggerOperation(OperationId = "getUserScenarioTemplates")]
        // public async STT.Task<IActionResult> GetByUserId(Guid id, CancellationToken ct)
        // {
        //     var list = await _scenarioTemplateService.GetByUserIdAsync(id, ct);
        //     return Ok(list);
        // }

        // /// <summary>
        // /// Gets all ScenarioTemplates for the current User
        // /// </summary>
        // /// <remarks>
        // /// Returns all ScenarioTemplates where the current User is a member of at least one of it's Teams
        // /// <para />
        // /// Accessible only to the current User.
        // /// <para/>
        // /// This is a convenience endpoint and simply returns a 302 redirect to the fully qualified users/{id}/scenarioTemplates endpoint
        // /// </remarks>
        // [HttpGet("me/scenarioTemplates")]
        // [ProducesResponseType(typeof(IEnumerable<ScenarioTemplate>), (int)HttpStatusCode.OK)]
        // [SwaggerOperation(OperationId = "getMyScenarioTemplates")]
        // public async STT.Task<IActionResult> GetMy(CancellationToken ct)
        // {
        //     return RedirectToAction(nameof(this.GetByUserId), new { id = User.GetId() });
        // }

        /// <summary>
        /// Gets a specific ScenarioTemplate by id
        /// </summary>
        /// <remarks>
        /// Returns the ScenarioTemplate with the id specified
        /// <para />
        /// Accessible to a SuperUser or a User that is a member of a Team within the specified ScenarioTemplate
        /// </remarks>
        /// <param name="id">The id of the ScenarioTemplate</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("scenarioTemplates/{id}")]
        [ProducesResponseType(typeof(SAVM.ScenarioTemplate), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getScenarioTemplate")]
        public async STT.Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var scenarioTemplate = await _scenarioTemplateService.GetAsync(id, ct);

            if (scenarioTemplate == null)
                throw new EntityNotFoundException<SAVM.ScenarioTemplate>();

            return Ok(scenarioTemplate);
        }

        /// <summary>
        /// Creates a new ScenarioTemplate
        /// </summary>
        /// <remarks>
        /// Creates a new ScenarioTemplate with the attributes specified
        /// <para />
        /// Accessible only to a SuperUser or an Administrator
        /// </remarks>
        /// <param name="scenarioTemplateForm">The data to create the ScenarioTemplate with</param>
        /// <param name="ct"></param>
        [HttpPost("scenarioTemplates")]
        [ProducesResponseType(typeof(SAVM.ScenarioTemplate), (int)HttpStatusCode.Created)]
        [SwaggerOperation(OperationId = "createScenarioTemplate")]
        public async STT.Task<IActionResult> Create([FromBody] SAVM.ScenarioTemplateForm scenarioTemplateForm, CancellationToken ct)
        {
            var createdScenarioTemplate = await _scenarioTemplateService.CreateAsync(scenarioTemplateForm, ct);
            return CreatedAtAction(nameof(this.Get), new { id = createdScenarioTemplate.Id }, createdScenarioTemplate);
        }

        /// <summary>
        /// Copies a new ScenarioTemplate
        /// </summary>
        /// <remarks>
        /// Copies a new ScenarioTemplate with the attributes specified
        /// <para />
        /// Accessible only to a SuperUser or an Administrator
        /// </remarks>
        /// <param name="id">The ID of scenarioTemplate to copy</param>
        /// <param name="ct"></param>
        [HttpPost("scenarioTemplates/{id}/copy")]
        [ProducesResponseType(typeof(SAVM.ScenarioTemplate), (int)HttpStatusCode.Created)]
        [SwaggerOperation(OperationId = "copyScenarioTemplate")]
        public async STT.Task<IActionResult> Copy(Guid id, CancellationToken ct)
        {
            var newScenarioTemplate = await _scenarioTemplateService.CopyAsync(id, ct);
            return CreatedAtAction(nameof(this.Get), new { id = newScenarioTemplate.Id }, newScenarioTemplate);
        }

        /// <summary>
        /// Updates an ScenarioTemplate
        /// </summary>
        /// <remarks>
        /// Updates an ScenarioTemplate with the attributes specified
        /// <para />
        /// Accessible only to a SuperUser or a User on an Admin Team within the specified ScenarioTemplate
        /// </remarks>
        /// <param name="id">The Id of the Exericse to update</param>
        /// <param name="scenarioTemplateForm">The updated ScenarioTemplate values</param>
        /// <param name="ct"></param>
        [HttpPut("scenarioTemplates/{id}")]
        [ProducesResponseType(typeof(SAVM.ScenarioTemplate), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "updateScenarioTemplate")]
        public async STT.Task<IActionResult> Update([FromRoute] Guid id, [FromBody] SAVM.ScenarioTemplateForm scenarioTemplateForm, CancellationToken ct)
        {
            var updatedScenarioTemplate = await _scenarioTemplateService.UpdateAsync(id, scenarioTemplateForm, ct);
            return Ok(updatedScenarioTemplate);
        }

        /// <summary>
        /// Deletes an ScenarioTemplate
        /// </summary>
        /// <remarks>
        /// Deletes an ScenarioTemplate with the specified id
        /// <para />
        /// Accessible only to a SuperUser or a User on an Admin Team within the specified ScenarioTemplate
        /// </remarks>
        /// <param name="id">The id of the ScenarioTemplate to delete</param>
        /// <param name="ct"></param>
        [HttpDelete("scenarioTemplates/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteScenarioTemplate")]
        public async STT.Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _scenarioTemplateService.DeleteAsync(id, ct);
            return NoContent();
        }

    }
}
