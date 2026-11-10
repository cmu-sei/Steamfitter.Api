// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
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
using Microsoft.Extensions.Logging;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Infrastructure.Extensions;
using Steamfitter.Api.Infrastructure.Exceptions;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Services
{
    public interface IVmCredentialService
    {
        STT.Task<IEnumerable<SAVM.VmCredential>> GetAsync(CancellationToken ct);
        STT.Task<SAVM.VmCredential> GetAsync(Guid Id, CancellationToken ct);
        STT.Task<IEnumerable<SAVM.VmCredential>> GetByScenarioTemplateIdAsync(Guid scenarioTemplateId, CancellationToken ct);
        STT.Task<IEnumerable<SAVM.VmCredential>> GetByScenarioIdAsync(Guid scenarioId, CancellationToken ct);
        STT.Task<SAVM.VmCredential> CreateAsync(SAVM.VmCredentialCreate vmCredential, CancellationToken ct);
        STT.Task<SAVM.VmCredential> UpdateAsync(Guid Id, SAVM.VmCredentialEdit vmCredential, CancellationToken ct);
        STT.Task<bool> DeleteAsync(Guid Id, CancellationToken ct);
    }

    public class VmCredentialService : IVmCredentialService
    {
        private readonly SteamfitterContext _context;
        private readonly ClaimsPrincipal _user;
        private readonly ILogger<TaskService> _logger;

        public VmCredentialService(
            SteamfitterContext context,
            IPrincipal user,
            ILogger<TaskService> logger)
        {
            _context = context;
            _user = user as ClaimsPrincipal;
            _logger = logger;
        }

        public async STT.Task<IEnumerable<SAVM.VmCredential>> GetAsync(CancellationToken ct)
        {
            var items = await _context.VmCredentials.ToListAsync(ct);

            return items.Select(x => x.ToVmCredential()).ToList();
        }

        public async STT.Task<SAVM.VmCredential> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.VmCredentials
                .SingleOrDefaultAsync(o => o.Id == id, ct);
            if (item == null)
                throw new EntityNotFoundException<SAVM.VmCredential>();

            return item.ToVmCredential();
        }

        public async STT.Task<IEnumerable<SAVM.VmCredential>> GetByScenarioTemplateIdAsync(Guid scenarioTemplateId, CancellationToken ct)
        {
            var vmCredentials = _context.VmCredentials.Where(x => x.ScenarioTemplateId == scenarioTemplateId);

            return vmCredentials.Select(x => x.ToVmCredential()).ToList();
        }

        public async STT.Task<IEnumerable<SAVM.VmCredential>> GetByScenarioIdAsync(Guid scenarioId, CancellationToken ct)
        {
            var vmCredentials = _context.VmCredentials.Where(x => x.ScenarioId == scenarioId);

            return vmCredentials.Select(x => x.ToVmCredential()).ToList();
        }

        public async STT.Task<SAVM.VmCredential> CreateAsync(SAVM.VmCredentialCreate vmCredential, CancellationToken ct)
        {
            var vmCredentialEntity = vmCredential.ToEntity();
            vmCredentialEntity.DateCreated = DateTime.UtcNow;
            vmCredentialEntity.CreatedBy = _user.GetId();

            _context.VmCredentials.Add(vmCredentialEntity);
            await _context.SaveChangesAsync(ct);

            return await GetAsync(vmCredentialEntity.Id, ct);
        }

        public async STT.Task<SAVM.VmCredential> UpdateAsync(Guid id, SAVM.VmCredentialEdit vmCredential, CancellationToken ct)
        {
            var vmCredentialToUpdate = await _context.VmCredentials.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (vmCredentialToUpdate == null)
                throw new EntityNotFoundException<SAVM.VmCredential>();

            vmCredential.ApplyTo(vmCredentialToUpdate);
            vmCredentialToUpdate.DateModified = DateTime.UtcNow;
            vmCredentialToUpdate.ModifiedBy = _user.GetId();
            await _context.SaveChangesAsync(ct);

            return vmCredentialToUpdate.ToVmCredential();
        }

        public async STT.Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            var vmCredentialToDelete = await _context.VmCredentials.SingleOrDefaultAsync(v => v.Id == id, ct);
            if (vmCredentialToDelete == null)
                throw new EntityNotFoundException<SAVM.VmCredential>();

            _context.VmCredentials.Remove(vmCredentialToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

    }

}
