// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using STT = System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Steamfitter.Api.Services;
using Swashbuckle.AspNetCore.Annotations;
using Player.Vm.Api.Models;
using Player.Api.Client;

namespace Steamfitter.Api.Controllers
{
    public class PlayerController : BaseController
    {
        private readonly IPlayerService _playerService;
        private readonly IPlayerVmService _playerVmService;
        private readonly IAuthorizationService _authorizationService;

        public PlayerController(IPlayerService playerService, IPlayerVmService playerVmService, IAuthorizationService authorizationService)
        {
            _playerService = playerService;
            _playerVmService = playerVmService;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Gets all Views this user can dispatch
        /// </summary>
        /// <remarks>
        /// Returns a list of all of the Views this user can dispatch.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("views")]
        [ProducesResponseType(typeof(IEnumerable<View>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getViews")]
        public async STT.Task<IActionResult> GetViews(CancellationToken ct)
        {
            var list = await _playerService.GetViewsAsync(ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets all VM's this user can dispatch to
        /// </summary>
        /// <remarks>
        /// Returns a list of all of the VM's this user can dispatch to.
        /// </remarks>
        /// <returns></returns>
        [HttpGet("vms")]
        [ProducesResponseType(typeof(IEnumerable<Vm>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getVms")]
        public async STT.Task<IActionResult> GetViewVms(Guid viewId, CancellationToken ct)
        {
            var list = await _playerVmService.GetViewVmsAsync(viewId, ct);
            return Ok(list);
        }
    }

}
