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
    public class ResultController : BaseController
    {
        private readonly IResultService _ResultService;
        private readonly IAuthorizationService _authorizationService;

        public ResultController(IResultService ResultService, IAuthorizationService authorizationService)
        {
            _ResultService = ResultService;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Gets all Result in the system
        /// </summary>
        /// <remarks>
        /// Returns a list of all of the Results in the system.
        /// <para />
        /// Only accessible to a SuperUser
        /// </remarks>       
        /// <returns></returns>
        [HttpGet("Results")]
        [ProducesResponseType(typeof(IEnumerable<SAVM.Result>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getResults")]
        public async STT.Task<IActionResult> Get(CancellationToken ct)
        {
            var list = await _ResultService.GetAsync(ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets all Results for a Scenario
        /// </summary>
        /// <remarks>
        /// Returns all Results for the specified Scenario
        /// </remarks>
        /// <returns></returns>
        [HttpGet("scenarios/{id}/Results")]
        [ProducesResponseType(typeof(IEnumerable<SAVM.Task>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getScenarioResults")]
        public async STT.Task<IActionResult> GetByScenarioId(Guid id, CancellationToken ct)
        {
            var list = await _ResultService.GetByScenarioIdAsync(id, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets all Results for a Task
        /// </summary>
        /// <remarks>
        /// Returns all Results for the specified Task
        /// </remarks>
        /// <returns></returns>
        [HttpGet("tasks/{id}/Results")]
        [ProducesResponseType(typeof(IEnumerable<SAVM.Task>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getTaskResults")]
        public async STT.Task<IActionResult> GetByTaskId(Guid id, CancellationToken ct)
        {
            var list = await _ResultService.GetByTaskIdAsync(id, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets all Results for an View
        /// </summary>
        /// <remarks>
        /// Returns all Results for the specified View
        /// </remarks>
        /// <returns></returns>
        [HttpGet("views/{id}/Results")]
        [ProducesResponseType(typeof(IEnumerable<SAVM.Task>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getViewResults")]
        public async STT.Task<IActionResult> GetByViewId(Guid id, CancellationToken ct)
        {
            var list = await _ResultService.GetByViewIdAsync(id, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets all manual Results for a User
        /// </summary>
        /// <remarks>
        /// Returns all manual Results for the specified User
        /// </remarks>
        /// <returns></returns>
        [HttpGet("users/{id}/Results")]
        [ProducesResponseType(typeof(IEnumerable<SAVM.Task>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getUserResults")]
        public async STT.Task<IActionResult> GetByUserId(Guid id, CancellationToken ct)
        {
            var list = await _ResultService.GetByUserIdAsync(id, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets all manual Results for the current User
        /// </summary>
        /// <remarks>
        /// Returns all manual Results for the current User
        /// </remarks>
        /// <returns></returns>
        [HttpGet("me/Results")]
        [ProducesResponseType(typeof(IEnumerable<SAVM.Task>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getMyResults")]
        public async STT.Task<IActionResult> GetMine(CancellationToken ct)
        {
            var list = await _ResultService.GetByUserIdAsync(User.GetId(), ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets all Results for a VM
        /// </summary>
        /// <remarks>
        /// Returns all Results for the specified VM
        /// </remarks>
        /// <returns></returns>
        [HttpGet("vms/{id}/Results")]
        [ProducesResponseType(typeof(IEnumerable<SAVM.Task>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getVmResults")]
        public async STT.Task<IActionResult> GetByVmId(Guid id, CancellationToken ct)
        {
            var list = await _ResultService.GetByVmIdAsync(id, ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets a specific Result by id
        /// </summary>
        /// <remarks>
        /// Returns the Result with the id specified
        /// <para />
        /// Accessible to a SuperUser or a User that is a member of a Team within the specified Result
        /// </remarks>
        /// <param name="id">The id of the Result</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("Results/{id}")]
        [ProducesResponseType(typeof(SAVM.Result), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getResult")]
        public async STT.Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var Result = await _ResultService.GetAsync(id, ct);

            if (Result == null)
                throw new EntityNotFoundException<SAVM.Result>();

            return Ok(Result);
        }

        /// <summary>
        /// Creates a new Result
        /// </summary>
        /// <remarks>
        /// Creates a new Result with the attributes specified
        /// <para />
        /// Accessible only to a SuperUser or an Administrator
        /// </remarks>    
        /// <param name="result">The data to create the Result with</param>
        /// <param name="ct"></param>
        [HttpPost("Results")]
        [ProducesResponseType(typeof(SAVM.Result), (int)HttpStatusCode.Created)]
        [SwaggerOperation(OperationId = "createResult")]
        public async STT.Task<IActionResult> Create([FromBody] SAVM.Result result, CancellationToken ct)
        {
            var createdResult = await _ResultService.CreateAsync(result, ct);
            return CreatedAtAction(nameof(this.Get), new { id = createdResult.Id }, createdResult);
        }

        /// <summary>
        /// Updates an Result
        /// </summary>
        /// <remarks>
        /// Updates an Result with the attributes specified
        /// <para />
        /// Accessible only to a SuperUser or a User on an Admin Team within the specified Result
        /// </remarks>  
        /// <param name="id">The Id of the Exericse to update</param>
        /// <param name="result">The updated Result values</param>
        /// <param name="ct"></param>
        [HttpPut("Results/{id}")]
        [ProducesResponseType(typeof(SAVM.Result), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "updateResult")]
        public async STT.Task<IActionResult> Update([FromRoute] Guid id, [FromBody] SAVM.Result result, CancellationToken ct)
        {
            var updatedResult = await _ResultService.UpdateAsync(id, result, ct);
            return Ok(updatedResult);
        }

        /// <summary>
        /// Deletes an Result
        /// </summary>
        /// <remarks>
        /// Deletes an Result with the specified id
        /// <para />
        /// Accessible only to a SuperUser or a User on an Admin Team within the specified Result
        /// </remarks>    
        /// <param name="id">The id of the Result to delete</param>
        /// <param name="ct"></param>
        [HttpDelete("Results/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteResult")]
        public async STT.Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _ResultService.DeleteAsync(id, ct);
            return NoContent();
        }

    }
}

