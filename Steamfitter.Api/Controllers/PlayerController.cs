/*
Crucible
Copyright 2020 Carnegie Mellon University.
NO WARRANTY. THIS CARNEGIE MELLON UNIVERSITY AND SOFTWARE ENGINEERING INSTITUTE MATERIAL IS FURNISHED ON AN "AS-IS" BASIS. CARNEGIE MELLON UNIVERSITY MAKES NO WARRANTIES OF ANY KIND, EITHER EXPRESSED OR IMPLIED, AS TO ANY MATTER INCLUDING, BUT NOT LIMITED TO, WARRANTY OF FITNESS FOR PURPOSE OR MERCHANTABILITY, EXCLUSIVITY, OR RESULTS OBTAINED FROM USE OF THE MATERIAL. CARNEGIE MELLON UNIVERSITY DOES NOT MAKE ANY WARRANTY OF ANY KIND WITH RESPECT TO FREEDOM FROM PATENT, TRADEMARK, OR COPYRIGHT INFRINGEMENT.
Released under a MIT (SEI)-style license, please see license.txt or contact permission@sei.cmu.edu for full terms.
[DISTRIBUTION STATEMENT A] This material has been approved for public release and unlimited distribution.  Please see Copyright notice for non-US Government use and distribution.
Carnegie Mellon(R) and CERT(R) are registered in the U.S. Patent and Trademark Office by Carnegie Mellon University.
DM20-0181
*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using STT = System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Steamfitter.Api.Services;
using Swashbuckle.AspNetCore.Annotations;
using S3.Player.Api.Models;
using S3.VM.Api.Models;

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

