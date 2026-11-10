// Copyright 2024 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Steamfitter.Api.Infrastructure.Mappings;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using STT = System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Infrastructure.Exceptions;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Services
{
    public interface ISystemRoleService
    {
        STT.Task<IEnumerable<ViewModels.SystemRole>> GetAsync(CancellationToken ct);
        STT.Task<ViewModels.SystemRole> GetAsync(Guid id, CancellationToken ct);
        STT.Task<ViewModels.SystemRole> CreateAsync(ViewModels.SystemRole systemRole, CancellationToken ct);
        STT.Task<ViewModels.SystemRole> UpdateAsync(Guid id, ViewModels.SystemRole systemRole, CancellationToken ct);
        STT.Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }

    public class SystemRoleService : ISystemRoleService
    {
        private readonly SteamfitterContext _context;
        private readonly ClaimsPrincipal _user;

        public SystemRoleService(SteamfitterContext context, IPrincipal user)
        {
            _context = context;
            _user = user as ClaimsPrincipal;
        }

        public async STT.Task<IEnumerable<ViewModels.SystemRole>> GetAsync(CancellationToken ct)
        {
            var items = await _context.SystemRoles
                .ToListAsync(ct);

            return items.Select(x => x.ToSystemRole()).ToList();
        }

        public async STT.Task<ViewModels.SystemRole> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.SystemRoles
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            return item.ToSystemRole();
        }

        public async STT.Task<ViewModels.SystemRole> CreateAsync(ViewModels.SystemRole systemRole, CancellationToken ct)
        {
            var systemRoleEntity = systemRole.ToEntity();

            _context.SystemRoles.Add(systemRoleEntity);
            await _context.SaveChangesAsync(ct);

            return await GetAsync(systemRoleEntity.Id, ct);
        }

        public async STT.Task<ViewModels.SystemRole> UpdateAsync(Guid id, ViewModels.SystemRole systemRole, CancellationToken ct)
        {
            var systemRoleToUpdate = await _context.SystemRoles.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (systemRoleToUpdate == null)
                throw new EntityNotFoundException<SAVM.SystemRole>();

            systemRole.ApplyTo(systemRoleToUpdate);

            _context.SystemRoles.Update(systemRoleToUpdate);
            await _context.SaveChangesAsync(ct);

            return systemRoleToUpdate.ToSystemRole();
        }

        public async STT.Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            var systemRoleToDelete = await _context.SystemRoles.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (systemRoleToDelete == null)
                throw new EntityNotFoundException<SAVM.SystemRole>();

            _context.SystemRoles.Remove(systemRoleToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

    }
}
