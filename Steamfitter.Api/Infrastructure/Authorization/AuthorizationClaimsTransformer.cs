// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Security.Claims;
using STT = System.Threading.Tasks;
using Steamfitter.Api.Services;
using Microsoft.AspNetCore.Authentication;

namespace Steamfitter.Api.Infrastructure.Authorization
{
    class AuthorizationClaimsTransformer : IClaimsTransformation
    {        
        private IUserClaimsService _claimsService;

        public AuthorizationClaimsTransformer(IUserClaimsService claimsService)
        {
            _claimsService = claimsService;
        }

        public async STT.Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var user = await _claimsService.AddUserClaims(principal, true);
            _claimsService.SetCurrentClaimsPrincipal(user);
            return user;
        }       
    }
}

