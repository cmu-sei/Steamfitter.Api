// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using STT = System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Steamfitter.Api.Data;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Infrastructure.Exceptions;
using Steamfitter.Api.Services;
using SAVM = Steamfitter.Api.ViewModels;
using Swashbuckle.AspNetCore.Annotations;
using Steamfitter.Api.ViewModels;
using System.Linq;

namespace Steamfitter.Api.Controllers
{
    public class ScenarioTemplateController : BaseController
    {
        private readonly IScenarioTemplateService _scenarioTemplateService;
        private readonly IScenarioTemplateArchiveService _scenarioTemplateArchiveService;
        private readonly ISteamfitterAuthorizationService _authorizationService;

        public ScenarioTemplateController(
            IScenarioTemplateService scenarioTemplateService,
            IScenarioTemplateArchiveService scenarioTemplateArchiveService,
            ISteamfitterAuthorizationService authorizationService)
        {
            _scenarioTemplateService = scenarioTemplateService;
            _scenarioTemplateArchiveService = scenarioTemplateArchiveService;
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
            IEnumerable<SAVM.ScenarioTemplate> list;
            // get ALL scenario templates
            if (await _authorizationService.AuthorizeAsync([SystemPermission.ViewScenarioTemplates], ct))
            {
                list = await _scenarioTemplateService.GetAsync(ct);
            }
            // get scenario templates the user can access
            else
            {
                list = await _scenarioTemplateService.GetMineAsync(ct);
            }

            AddPermissions(list);

            return Ok(list);

        }

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
            if (!await _authorizationService.AuthorizeAsync<SAVM.ScenarioTemplate>(id, [SystemPermission.ViewScenarioTemplates], [ScenarioTemplatePermission.ViewScenarioTemplate], ct))
                throw new ForbiddenException();

            var scenarioTemplate = await _scenarioTemplateService.GetAsync(id, ct);

            AddPermissions(scenarioTemplate);

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
            if (!await _authorizationService.AuthorizeAsync([SystemPermission.CreateScenarioTemplates], ct))
                throw new ForbiddenException();

            var createdScenarioTemplate = await _scenarioTemplateService.CreateAsync(scenarioTemplateForm, ct);
            AddPermissions(createdScenarioTemplate);

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
            if (!await _authorizationService.AuthorizeAsync([SystemPermission.CreateScenarioTemplates], ct)
                || !await _authorizationService.AuthorizeAsync<SAVM.ScenarioTemplate>(id, [SystemPermission.ViewScenarioTemplates], [ScenarioTemplatePermission.ViewScenarioTemplate], ct))
                throw new ForbiddenException();

            var newScenarioTemplate = await _scenarioTemplateService.CopyAsync(id, ct);
            AddPermissions(newScenarioTemplate);

            return CreatedAtAction(nameof(this.Get), new { id = newScenarioTemplate.Id }, newScenarioTemplate);
        }

        /// <summary>
        /// Exports ScenarioTemplates
        /// </summary>
        /// <remarks>
        /// Returns a json file containing the specified ScenarioTemplates, with their VM Credentials and Task trees.
        /// All ScenarioTemplates are exported if no ids are specified. The ids travel in the body rather than the
        /// query string, so an export of many templates is not limited by the length of a URL.
        /// <para />
        /// Accessible to a User with the ViewScenarioTemplates system permission, or ViewScenarioTemplate permission on each specified ScenarioTemplate
        /// </remarks>
        /// <param name="request">The ids of the ScenarioTemplates to export</param>
        /// <param name="ct"></param>
        [HttpPost("scenarioTemplates/actions/export")]
        [ProducesResponseType(typeof(FileResult), (int)HttpStatusCode.OK, "application/json")]
        [SwaggerOperation(OperationId = "exportScenarioTemplates")]
        public async STT.Task<IActionResult> Export([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SAVM.ScenarioTemplateExportRequest request, CancellationToken ct)
        {
            var ids = request?.Ids?.ToArray() ?? [];
            if (!await _authorizationService.AuthorizeAsync([SystemPermission.ViewScenarioTemplates], ct))
            {
                if (ids.Length == 0)
                    throw new ForbiddenException();

                foreach (var id in ids.Distinct())
                {
                    if (!await _authorizationService.AuthorizeAsync<SAVM.ScenarioTemplate>(id, [SystemPermission.ViewScenarioTemplates], [ScenarioTemplatePermission.ViewScenarioTemplate], ct))
                        throw new ForbiddenException();
                }
            }

            var (stream, fileName) = await _scenarioTemplateArchiveService.ExportAsync(ids, ct);

            // If this is wrapped in an Ok, it throws an exception
            return File(stream, "application/json", fileName);
        }

        /// <summary>
        /// Imports ScenarioTemplates
        /// </summary>
        /// <remarks>
        /// Creates ScenarioTemplates, with their VM Credentials and Task trees, from a json file produced by the export
        /// (or a zip containing one). Each is a new copy with new ids. A ScenarioTemplate whose name is already in use
        /// is renamed "name - user", noted in Warnings. ScenarioTemplates that cannot be imported are listed in Failures; the rest are created.
        /// <para />
        /// Accessible to a User with the CreateScenarioTemplates system permission
        /// </remarks>
        /// <param name="form">The file to import</param>
        /// <param name="ct"></param>
        [HttpPost("scenarioTemplates/actions/import")]
        [ProducesResponseType(typeof(SAVM.ImportScenarioTemplatesResult), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "importScenarioTemplates")]
        public async STT.Task<IActionResult> Import([FromForm] SAVM.ScenarioTemplateImportForm form, CancellationToken ct)
        {
            if (!await _authorizationService.AuthorizeAsync([SystemPermission.CreateScenarioTemplates], ct))
                throw new ForbiddenException();

            var result = await _scenarioTemplateArchiveService.ImportAsync(form.File, ct);
            foreach (var item in result.ScenarioTemplates)
            {
                AddPermissions(item);
            }

            return Ok(result);
        }

        /// <summary>
        /// Updates a ScenarioTemplate
        /// </summary>
        /// <remarks>
        /// Updates a ScenarioTemplate with the attributes specified
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
            if (!await _authorizationService.AuthorizeAsync<SAVM.ScenarioTemplate>(id, [SystemPermission.EditScenarioTemplates], [ScenarioTemplatePermission.EditScenarioTemplate], ct))
                throw new ForbiddenException();

            var updatedScenarioTemplate = await _scenarioTemplateService.UpdateAsync(id, scenarioTemplateForm, ct);
            AddPermissions(updatedScenarioTemplate);

            return Ok(updatedScenarioTemplate);
        }

        /// <summary>
        /// Deletes a ScenarioTemplate
        /// </summary>
        /// <remarks>
        /// Deletes a ScenarioTemplate with the specified id
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
            if (!await _authorizationService.AuthorizeAsync<SAVM.ScenarioTemplate>(id, [SystemPermission.ManageScenarioTemplates], [ScenarioTemplatePermission.ManageScenarioTemplate], ct))
                throw new ForbiddenException();

            await _scenarioTemplateService.DeleteAsync(id, ct);
            return NoContent();
        }

        private void AddPermissions(IEnumerable<SAVM.ScenarioTemplate> list)
        {
            foreach (var item in list)
            {
                AddPermissions(item);
            }
        }

        private void AddPermissions(SAVM.ScenarioTemplate item)
        {
            item.ScenarioTemplatePermissions = _authorizationService.GetScenarioTemplatePermissions(item.Id).SelectMany(m => m.Permissions).Select(m => m.ToString()).ToList();
        }

    }
}
