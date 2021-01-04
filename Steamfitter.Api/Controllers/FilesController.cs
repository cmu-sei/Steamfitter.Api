// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using STT = System.Threading.Tasks;
using Steamfitter.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Steamfitter.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : BaseController
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IFilesService _filesService;

        public FilesController(IAuthorizationService authorizationService, IFilesService filesService)
        {
            _authorizationService = authorizationService;
            _filesService = filesService;
        }

        /// <summary>
        /// Gets all files that a user can dispatch to guest vms
        /// </summary>
        /// <param name="ct">CancellationToken</param>
        /// <returns>List of files this user can dispatch</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<FileInfo>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getAllFiles")]
        public async STT.Task<IActionResult> Get(CancellationToken ct)
        {
            return Ok(await _filesService.GetAsync(ct));
        }

        /// <summary>
        /// Gets a file that a user can dispatch to guest vms
        /// </summary>
        /// <param name="id">Id of the file</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>List of files this user can dispatch</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(FileInfo), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getFileById")]
        public async STT.Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            return Ok(await _filesService.GetAsync(id, ct));
        }

        /// <summary>
        /// Saves files and enables them to be dispatchable. If the file is not a zip file, it automatically gets zipped on upload
        /// </summary>
        /// <param name="files">IEnumerable<IFormFile/></param>
        /// <param name="ct">CancellationToken</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(IEnumerable<FileInfo>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "saveFile")]
        // TODO: fix api client generation and stop ignoring this endpoint
        [ApiExplorerSettings(IgnoreApi = true)]
        public async STT.Task<IActionResult> Post(IEnumerable<IFormFile> files, CancellationToken ct)
        {
            return Ok(await _filesService.SaveAsync(files, ct));
        }

        /// <summary>
        /// Deletes a file record and the actual file
        /// </summary>
        /// <param name="id">File to delete</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(IEnumerable<FileInfo>), (int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteFile")]
        public async STT.Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _filesService.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}
