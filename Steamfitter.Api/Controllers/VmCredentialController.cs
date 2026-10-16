// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using STT = System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Steamfitter.Api.Services;
using SAVM = Steamfitter.Api.ViewModels;
using Steamfitter.Api.Data;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Infrastructure.Exceptions;
using Swashbuckle.AspNetCore.Annotations;
using Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Controllers
{
    public class VmCredentialController : BaseController
    {
        private readonly IVmCredentialService _vmCredentialService;
        private readonly ISteamfitterAuthorizationService _authorizationService;

        public VmCredentialController(IVmCredentialService vmCredentialService, ISteamfitterAuthorizationService authorizationService)
        {
            _vmCredentialService = vmCredentialService;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Gets all VmCredentials for a ScenarioTemplate
        /// </summary>
        /// <remarks>
        /// Returns all VmCredentials for the specified ScenarioTemplate
        /// </remarks>
        /// <returns></returns>
        [HttpGet("scenarioTemplates/{id}/VmCredentials")]
        [ProducesResponseType(typeof(IEnumerable<SAVM.VmCredential>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getScenarioTemplateVmCredentials")]
        public async STT.Task<IActionResult> GetByScenarioTemplateId(Guid id, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync<SAVM.ScenarioTemplate>(id, [SystemPermission.ViewScenarioTemplates], [ScenarioTemplatePermission.ViewScenarioTemplate], ct))
                throw new ForbiddenException();

            var list = await _vmCredentialService.GetByScenarioTemplateIdAsync(id, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets all VmCredentials for a Scenario
        /// </summary>
        /// <remarks>
        /// Returns all VmCredentials for the specified Scenario
        /// </remarks>
        /// <returns></returns>
        [HttpGet("scenarios/{id}/VmCredentials")]
        [ProducesResponseType(typeof(IEnumerable<SAVM.VmCredential>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getScenarioVmCredentials")]
        public async STT.Task<IActionResult> GetByScenarioId(Guid id, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync<SAVM.Scenario>(id, [SystemPermission.ViewScenarios], [ScenarioPermission.ViewScenario], ct))
                throw new ForbiddenException();

            var list = await _vmCredentialService.GetByScenarioIdAsync(id, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets a specific VmCredential by id
        /// </summary>
        /// <remarks>
        /// Returns the VmCredential with the id specified
        /// <para />
        /// Accessible to a SuperUser or a User that is a member of a Team within the specified VmCredential
        /// </remarks>
        /// <param name="id">The id of the STT.Task</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("VmCredentials/{id}")]
        [ProducesResponseType(typeof(SAVM.VmCredential), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getVmCredential")]
        public async STT.Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var vmCredential = await _vmCredentialService.GetAsync(id, ct);
            if (vmCredential == null)
                throw new EntityNotFoundException<VmCredential>();

            if (!(vmCredential.ScenarioTemplateId != null && await _authorizationService.AuthorizeAsync<SAVM.ScenarioTemplate>(vmCredential.ScenarioTemplateId, [SystemPermission.ViewScenarioTemplates], [ScenarioTemplatePermission.ViewScenarioTemplate], ct))
               && !(vmCredential.ScenarioId != null && await _authorizationService.AuthorizeAsync<SAVM.Scenario>(vmCredential.ScenarioId, [SystemPermission.ViewScenarios], [ScenarioPermission.ViewScenario], ct)))
                throw new ForbiddenException();

            return Ok(vmCredential);
        }

        /// <summary>
        /// Creates a new VmCredential
        /// </summary>
        /// <remarks>
        /// Creates a new VmCredential with the attributes specified
        /// <para />
        /// The VmCredential must belong to exactly one Scenario or Scenario Template.
        /// </remarks>
        /// <param name="vmCredential">The data to create the VmCredential with</param>
        /// <param name="ct"></param>
        [HttpPost("VmCredentials")]
        [ProducesResponseType(typeof(SAVM.VmCredential), (int)HttpStatusCode.Created)]
        [SwaggerOperation(OperationId = "createVmCredential")]
        public async STT.Task<IActionResult> Create([FromBody] SAVM.VmCredentialCreate vmCredential, CancellationToken ct)
        {
            await CheckParentEditAuthorization(vmCredential.ScenarioId, vmCredential.ScenarioTemplateId, ct);

            var createdVmCredential = await _vmCredentialService.CreateAsync(vmCredential, ct);
            return CreatedAtAction(nameof(this.Get), new { id = createdVmCredential.Id }, createdVmCredential);
        }

        /// <summary>
        /// Updates a VmCredential
        /// </summary>
        /// <remarks>
        /// Updates a VmCredential with the attributes specified. The VmCredential stays in its Scenario or Scenario Template.
        /// <para />
        /// Accessible only to a SuperUser or a User on an Admin Team within the specified VmCredential
        /// </remarks>
        /// <param name="id">The Id of the Exericse to update</param>
        /// <param name="vmCredential">The updated VmCredential values</param>
        /// <param name="ct"></param>
        [HttpPut("VmCredentials/{id}")]
        [ProducesResponseType(typeof(SAVM.VmCredential), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "updateVmCredential")]
        public async STT.Task<IActionResult> Update([FromRoute] Guid id, [FromBody] SAVM.VmCredentialEdit vmCredential, CancellationToken ct)
        {
            var storedVmCredential = await _vmCredentialService.GetAsync(id, ct);
            if (storedVmCredential == null)
                throw new EntityNotFoundException<VmCredential>();

            await CheckParentEditAuthorization(storedVmCredential.ScenarioId, storedVmCredential.ScenarioTemplateId, ct);

            var updatedVmCredential = await _vmCredentialService.UpdateAsync(id, vmCredential, ct);
            return Ok(updatedVmCredential);
        }

        /// <summary>
        /// Deletes a VmCredential
        /// </summary>
        /// <remarks>
        /// Deletes a VmCredential with the specified id
        /// <para />
        /// Accessible only to a SuperUser or a User on an Admin Team within the specified VmCredential
        /// </remarks>
        /// <param name="id">The id of the VmCredential to delete</param>
        /// <param name="ct"></param>
        [HttpDelete("VmCredentials/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteVmCredential")]
        public async STT.Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var vmCredential = await _vmCredentialService.GetAsync(id, ct);
            if (vmCredential == null)
                throw new EntityNotFoundException<VmCredential>();

            await CheckParentEditAuthorization(vmCredential.ScenarioId, vmCredential.ScenarioTemplateId, ct);

            await _vmCredentialService.DeleteAsync(id, ct);
            return NoContent();
        }

        // a credential belongs to exactly one Scenario or Scenario Template, and editing it needs edit permission there
        private async STT.Task CheckParentEditAuthorization(Guid? scenarioId, Guid? scenarioTemplateId, CancellationToken ct)
        {
            if ((scenarioId == null) == (scenarioTemplateId == null))
                throw new ForbiddenException("A VmCredential must belong to exactly one Scenario or Scenario Template.");

            if (scenarioId != null
                ? !await _authorizationService.AuthorizeAsync<SAVM.Scenario>(scenarioId, [SystemPermission.EditScenarios], [ScenarioPermission.EditScenario], ct)
                : !await _authorizationService.AuthorizeAsync<SAVM.ScenarioTemplate>(scenarioTemplateId, [SystemPermission.EditScenarioTemplates], [ScenarioTemplatePermission.EditScenarioTemplate], ct))
                throw new ForbiddenException();
        }

    }
}
