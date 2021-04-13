// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using STT = System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Infrastructure.Exceptions;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Services
{
    public interface IUserPermissionService
    {
        STT.Task<IEnumerable<ViewModels.UserPermission>> GetAsync(CancellationToken ct);
        STT.Task<ViewModels.UserPermission> GetAsync(Guid id, CancellationToken ct);
        STT.Task<ViewModels.UserPermission> CreateAsync(ViewModels.UserPermission userPermission, CancellationToken ct);
        STT.Task<bool> DeleteAsync(Guid id, CancellationToken ct);
        STT.Task<bool> DeleteByIdsAsync(Guid userId, Guid permissionId, CancellationToken ct);
    }

    public class UserPermissionService : IUserPermissionService
    {
        private readonly SteamfitterContext _context;
        private readonly IAuthorizationService _authorizationService;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;

        public UserPermissionService(SteamfitterContext context, IAuthorizationService authorizationService, IPrincipal user, IMapper mapper)
        {
            _context = context;
            _authorizationService = authorizationService;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
        }

        public async STT.Task<IEnumerable<ViewModels.UserPermission>> GetAsync(CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new FullRightsRequirement())).Succeeded)
                throw new ForbiddenException();

            var items = await _context.UserPermissions
                .ToListAsync(ct);         
            
            return _mapper.Map<IEnumerable<SAVM.UserPermission>>(items);
        }

        public async STT.Task<ViewModels.UserPermission> GetAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new FullRightsRequirement())).Succeeded)
                throw new ForbiddenException();

            var item = await _context.UserPermissions
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            return _mapper.Map<SAVM.UserPermission>(item);
        }

        public async STT.Task<ViewModels.UserPermission> CreateAsync(ViewModels.UserPermission userPermission, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new FullRightsRequirement())).Succeeded)
                throw new ForbiddenException();

            userPermission.DateCreated = DateTime.UtcNow;
            var userPermissionEntity = _mapper.Map<UserPermissionEntity>(userPermission);

            _context.UserPermissions.Add(userPermissionEntity);
            await _context.SaveChangesAsync(ct);

            return await GetAsync(userPermissionEntity.Id, ct);
        }

        public async STT.Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new FullRightsRequirement())).Succeeded)
                throw new ForbiddenException();

            var userPermissionToDelete = await _context.UserPermissions.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (userPermissionToDelete == null)
                throw new EntityNotFoundException<SAVM.UserPermission>();

            _context.UserPermissions.Remove(userPermissionToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        public async STT.Task<bool> DeleteByIdsAsync(Guid userId, Guid permissionId, CancellationToken ct)
        {
            if (!(await _authorizationService.AuthorizeAsync(_user, null, new FullRightsRequirement())).Succeeded)
                throw new ForbiddenException();

            var userPermissionToDelete = await _context.UserPermissions.SingleOrDefaultAsync(v => v.UserId == userId && v.PermissionId == permissionId, ct);

            if (userPermissionToDelete == null)
                throw new EntityNotFoundException<SAVM.UserPermission>();

            _context.UserPermissions.Remove(userPermissionToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

    }
}

