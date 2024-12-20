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
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Text.RegularExpressions;

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
                    claims.AddRange(await GetPermissionClaims(userId, principal));

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
                    // TODO: set first user to ALL permissions/sysadmin
                    // if (!anyUsers)
                    // {
                    //     var systemAdminPermission = await _context.Permissions.Where(p => p.Key == SteamfitterClaimTypes.SystemAdmin.ToString()).FirstOrDefaultAsync();

                    //     if (systemAdminPermission != null)
                    //     {
                    //         user.UserPermissions.Add(new UserPermissionEntity(user.Id, systemAdminPermission.Id));
                    //     }
                    // }

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
            throw new NotImplementedException();

            // var userPermissions = await _context.UserPermissions
            //     .Where(u => u.UserId == userId)
            //     .Include(x => x.Permission)
            //     .ToArrayAsync();

            // if (userPermissions.Where(x => x.Permission.Key == SteamfitterClaimTypes.SystemAdmin.ToString()).Any())
            // {
            //     claims.Add(new Claim(SteamfitterClaimTypes.SystemAdmin.ToString(), "true"));
            // }

            // if (userPermissions.Where(x => x.Permission.Key == SteamfitterClaimTypes.ContentDeveloper.ToString()).Any())
            // {
            //     claims.Add(new Claim(SteamfitterClaimTypes.ContentDeveloper.ToString(), "true"));
            // }

            // if (userPermissions.Where(x => x.Permission.Key == SteamfitterClaimTypes.Operator.ToString()).Any())
            // {
            //     claims.Add(new Claim(SteamfitterClaimTypes.Operator.ToString(), "true"));
            // }

            // if (userPermissions.Where(x => x.Permission.Key == SteamfitterClaimTypes.BaseUser.ToString()).Any())
            // {
            //     claims.Add(new Claim(SteamfitterClaimTypes.BaseUser.ToString(), "true"));
            // }

            return claims;
        }

        private async STT.Task<IEnumerable<Claim>> GetPermissionClaims(Guid userId, ClaimsPrincipal principal)
        {
            List<Claim> claims = new();

            var tokenRoleNames = _options.UseRolesFromIdP ?
                this.GetClaimsFromToken(principal, _options.RolesClaimPath).Select(x => x.ToLower()) :
                [];

            var roles = await _context.SystemRoles
                .Where(x => tokenRoleNames.Contains(x.Name.ToLower()))
                .ToListAsync();

            var userRole = await _context.Users
                .Where(x => x.Id == userId)
                .Select(x => x.Role)
                .FirstOrDefaultAsync();

            if (userRole != null)
            {
                roles.Add(userRole);
            }

            roles = roles.Distinct().ToList();

            foreach (var role in roles)
            {
                List<string> permissions;

                if (role.AllPermissions)
                {
                    permissions = Enum.GetValues<SystemPermission>().Select(x => x.ToString()).ToList();
                }
                else
                {
                    permissions = role.Permissions.Select(x => x.ToString()).ToList();
                }

                foreach (var permission in permissions)
                {
                    if (!claims.Any(x => x.Type == AuthorizationConstants.PermissionClaimType &&
                        x.Value == permission))
                    {
                        claims.Add(new Claim(AuthorizationConstants.PermissionClaimType, permission));
                    };
                }
            }

            var groupNames = _options.UseGroupsFromIdP ?
                this.GetClaimsFromToken(principal, _options.GroupsClaimPath).Select(x => x.ToLower()) :
                [];

            var groupIds = await _context.Groups
                .Where(x => x.Memberships.Any(y => y.UserId == userId) || groupNames.Contains(x.Name.ToLower()))
                .Select(x => x.Id)
                .ToListAsync();

            // Get Scenario Permissions
            var scenarioMemberships = await _context.ScenarioMemberships
                .Where(x => x.UserId == userId || (x.GroupId.HasValue && groupIds.Contains(x.GroupId.Value)))
                .Include(x => x.Role)
                .GroupBy(x => x.ScenarioId)
                .ToListAsync();

            foreach (var group in scenarioMemberships)
            {
                var scenarioPermissions = new List<ScenarioPermission>();

                foreach (var membership in group)
                {
                    if (membership.Role.AllPermissions)
                    {
                        scenarioPermissions.AddRange(Enum.GetValues<ScenarioPermission>());
                    }
                    else
                    {
                        scenarioPermissions.AddRange(membership.Role.Permissions);
                    }
                }

                var permissionsClaim = new ScenarioPermissionClaim
                {
                    ScenarioId = group.Key,
                    Permissions = scenarioPermissions.Distinct().ToArray()
                };

                claims.Add(new Claim(AuthorizationConstants.ScenarioPermissionClaimType, permissionsClaim.ToString()));
            }

            // Get ScenarioTemplate Permissions
            var scenarioTemplateMemberships = await _context.ScenarioTemplateMemberships
                .Where(x => x.UserId == userId || (x.GroupId.HasValue && groupIds.Contains(x.GroupId.Value)))
                .Include(x => x.Role)
                .GroupBy(x => x.ScenarioTemplateId)
                .ToListAsync();

            foreach (var group in scenarioTemplateMemberships)
            {
                var scenarioTemplatePermissions = new List<ScenarioTemplatePermission>();

                foreach (var membership in group)
                {
                    if (membership.Role.AllPermissions)
                    {
                        scenarioTemplatePermissions.AddRange(Enum.GetValues<ScenarioTemplatePermission>());
                    }
                    else
                    {
                        scenarioTemplatePermissions.AddRange(membership.Role.Permissions);
                    }
                }

                var permissionsClaim = new ScenarioTemplatePermissionClaim
                {
                    ScenarioTemplateId = group.Key,
                    Permissions = scenarioTemplatePermissions.Distinct().ToArray()
                };

                claims.Add(new Claim(AuthorizationConstants.ScenarioTemplatePermissionClaimType, permissionsClaim.ToString()));
            }

            return claims;
        }

        private string[] GetClaimsFromToken(ClaimsPrincipal principal, string claimPath)
        {
            if (string.IsNullOrEmpty(claimPath))
            {
                return [];
            }

            // Name of the claim to insert into the token. This can be a fully qualified name like 'address.street'.
            // In this case, a nested json object will be created. To prevent nesting and use dot literally, escape the dot with backslash (\.).
            var pathSegments = Regex.Split(claimPath, @"(?<!\\)\.").Select(s => s.Replace("\\.", ".")).ToArray();

            var tokenClaim = principal.Claims.Where(x => x.Type == pathSegments.First()).FirstOrDefault();

            if (tokenClaim == null)
            {
                return [];
            }

            return tokenClaim.ValueType switch
            {
                ClaimValueTypes.String => [tokenClaim.Value],
                JsonClaimValueTypes.Json => ExtractJsonClaimValues(tokenClaim.Value, pathSegments.Skip(1)),
                _ => []
            };
        }

        private string[] ExtractJsonClaimValues(string json, IEnumerable<string> pathSegments)
        {
            List<string> values = new();
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement currentElement = doc.RootElement;

                foreach (var segment in pathSegments)
                {
                    if (!currentElement.TryGetProperty(segment, out JsonElement propertyElement))
                    {
                        return [];
                    }

                    currentElement = propertyElement;
                }

                if (currentElement.ValueKind == JsonValueKind.Array)
                {
                    values.AddRange(currentElement.EnumerateArray()
                        .Where(item => item.ValueKind == JsonValueKind.String)
                        .Select(item => item.GetString()));
                }
                else if (currentElement.ValueKind == JsonValueKind.String)
                {
                    values.Add(currentElement.GetString());
                }
            }
            catch (JsonException)
            {
                // Handle invalid JSON format
            }

            return values.ToArray();
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
