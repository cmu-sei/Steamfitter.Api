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
    public class UserController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IAuthorizationService _authorizationService;

        public UserController(IUserService userService, IAuthorizationService authorizationService)
        {
            _userService = userService;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Gets all Users in the system
        /// </summary>
        /// <remarks>
        /// Returns a list of all of the Users in the system.
        /// <para />
        /// Only accessible to a SuperUser
        /// </remarks>       
        /// <returns></returns>
        [HttpGet("users")]
        [ProducesResponseType(typeof(IEnumerable<SAVM.User>), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getUsers")]
        public async STT.Task<IActionResult> Get(CancellationToken ct)
        {
            var list = await _userService.GetAsync(ct);
            return Ok(list);
        }

        /// <summary>
        /// Gets a specific User by id
        /// </summary>
        /// <remarks>
        /// Returns the User with the id specified
        /// <para />
        /// Only accessible to a SuperUser
        /// </remarks>
        /// <param name="id">The id of the User</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("users/{id}")]
        [ProducesResponseType(typeof(SAVM.User), (int)HttpStatusCode.OK)]
        [SwaggerOperation(OperationId = "getUser")]
        public async STT.Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var user = await _userService.GetAsync(id, ct);

            if (user == null)
                throw new EntityNotFoundException<SAVM.User>();

            return Ok(user);
        }

        /// <summary>
        /// Creates a new User
        /// </summary>
        /// <remarks>
        /// Creates a new User with the attributes specified
        /// <para />
        /// Accessible only to a SuperUser
        /// </remarks>    
        /// <param name="user">The data to create the User with</param>
        /// <param name="ct"></param>
        [HttpPost("users")]
        [ProducesResponseType(typeof(SAVM.User), (int)HttpStatusCode.Created)]
        [SwaggerOperation(OperationId = "createUser")]
        public async STT.Task<IActionResult> Create([FromBody] SAVM.User user, CancellationToken ct)
        {
            user.CreatedBy = User.GetId();
            var createdUser = await _userService.CreateAsync(user, ct);
            return CreatedAtAction(nameof(this.Get), new { id = createdUser.Id }, createdUser);
        }

        /// <summary>
        /// Deletes a User
        /// </summary>
        /// <remarks>
        /// Deletes a User with the specified id
        /// <para />
        /// Accessible only to a SuperUser
        /// </remarks>    
        /// <param name="id">The id of the User to delete</param>
        /// <param name="ct"></param>
        [HttpDelete("users/{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [SwaggerOperation(OperationId = "deleteUser")]
        public async STT.Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _userService.DeleteAsync(id, ct);
            return NoContent();
        }


    }
}

