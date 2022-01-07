// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using STT = System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Infrastructure.Extensions;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Infrastructure.Options;

namespace Steamfitter.Api.Services
{
    public interface IUserClaimsService
    {
        STT.Task<ClaimsPrincipal> AddUserClaims(ClaimsPrincipal principal, bool update);
        STT.Task<ClaimsPrincipal> GetClaimsPrincipal(Guid userId, bool setAsCurrent);
        STT.Task<ClaimsPrincipal> RefreshClaims(Guid userId);
        ClaimsPrincipal GetCurrentClaimsPrincipal();
        void SetCurrentClaimsPrincipal(ClaimsPrincipal principal);
    }

    public class UserClaimsService : IUserClaimsService
    {
        private readonly SteamfitterContext _context;
        private readonly ClaimsTransformationOptions _options;
        private IMemoryCache _cache;
        private ClaimsPrincipal _currentClaimsPrincipal;

        public UserClaimsService(SteamfitterContext context, IMemoryCache cache, ClaimsTransformationOptions options)
        {
            _context = context;
            _options = options;
            _cache = cache;
        }

        public async STT.Task<ClaimsPrincipal> AddUserClaims(ClaimsPrincipal principal, bool update)
        {
            List<Claim> claims;
            var identity = ((ClaimsIdentity)principal.Identity);
            var userId = principal.GetId();

            if (!_cache.TryGetValue(userId, out claims))
            {
                claims = new List<Claim>();
                var user = await ValidateUser(userId, principal.FindFirst("name")?.Value, update);

                if (user != null)
                {
                    claims.AddRange(await GetUserClaims(userId));

                    if (_options.EnableCaching)
                    {
                        _cache.Set(userId, claims, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(_options.CacheExpirationSeconds)));
                    }
                }
            }
            addNewClaims(identity, claims);
            return principal;
        }

        public async STT.Task<ClaimsPrincipal> GetClaimsPrincipal(Guid userId, bool setAsCurrent)
        {
            ClaimsIdentity identity = new ClaimsIdentity();
            identity.AddClaim(new Claim("sub", userId.ToString()));
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);

            principal = await AddUserClaims(principal, false);

            if (setAsCurrent || _currentClaimsPrincipal.GetId() == userId)
            {
                _currentClaimsPrincipal = principal;
            }

            return principal;
        }

        public async STT.Task<ClaimsPrincipal> RefreshClaims(Guid userId)
        {
            _cache.Remove(userId);
            return await GetClaimsPrincipal(userId, false);
        }

        public ClaimsPrincipal GetCurrentClaimsPrincipal()
        {
            return _currentClaimsPrincipal;
        }

        public void SetCurrentClaimsPrincipal(ClaimsPrincipal principal)
        {
            _currentClaimsPrincipal = principal;
        }

        private async STT.Task<UserEntity> ValidateUser(Guid subClaim, string nameClaim, bool update)
        {
            var user = await _context.Users
                .Where(u => u.Id == subClaim)
                .SingleOrDefaultAsync();

            var anyUsers = await _context.Users.AnyAsync();

            if (update)
            {
                if (user == null)
                {
                    user = new UserEntity
                    {
                        Id = subClaim,
                        Name = nameClaim ?? "Anonymous"
                    };

                    // First user is default SystemAdmin
                    if (!anyUsers)
                    {
                        var systemAdminPermission = await _context.Permissions.Where(p => p.Key == SteamfitterClaimTypes.SystemAdmin.ToString()).FirstOrDefaultAsync();

                        if (systemAdminPermission != null)
                        {
                            user.UserPermissions.Add(new UserPermissionEntity(user.Id, systemAdminPermission.Id));
                        }
                    }

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    if (nameClaim != null && user.Name != nameClaim)
                    {
                        user.Name = nameClaim;
                        _context.Update(user);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return user;
        }

        private async STT.Task<IEnumerable<Claim>> GetUserClaims(Guid userId)
        {
            List<Claim> claims = new List<Claim>();

            var userPermissions = await _context.UserPermissions
                .Where(u => u.UserId == userId)
                .Include(x => x.Permission)
                .ToArrayAsync();

            if (userPermissions.Where(x => x.Permission.Key == SteamfitterClaimTypes.SystemAdmin.ToString()).Any())
            {
                claims.Add(new Claim(SteamfitterClaimTypes.SystemAdmin.ToString(), "true"));
            }

            if (userPermissions.Where(x => x.Permission.Key == SteamfitterClaimTypes.ContentDeveloper.ToString()).Any())
            {
                claims.Add(new Claim(SteamfitterClaimTypes.ContentDeveloper.ToString(), "true"));
            }

            if (userPermissions.Where(x => x.Permission.Key == SteamfitterClaimTypes.Operator.ToString()).Any())
            {
                claims.Add(new Claim(SteamfitterClaimTypes.Operator.ToString(), "true"));
            }

            if (userPermissions.Where(x => x.Permission.Key == SteamfitterClaimTypes.BaseUser.ToString()).Any())
            {
                claims.Add(new Claim(SteamfitterClaimTypes.BaseUser.ToString(), "true"));
            }

            return claims;
        }

        private void addNewClaims(ClaimsIdentity identity, List<Claim> claims)
        {
            var newClaims = new List<Claim>();
            claims.ForEach(delegate (Claim claim)
            {
                if (!identity.Claims.Any(identityClaim => identityClaim.Type == claim.Type))
                {
                    newClaims.Add(claim);
                }
            });
            identity.AddClaims(newClaims);
        }
    }
}

