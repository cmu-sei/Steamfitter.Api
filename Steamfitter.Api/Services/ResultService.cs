// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using STT = System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Infrastructure.Extensions;
using Steamfitter.Api.Infrastructure.Exceptions;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Services
{
    public interface IResultService
    {
        STT.Task<ViewModels.Result> GetAsync(Guid id, CancellationToken ct);
        STT.Task<IEnumerable<ViewModels.Result>> GetByScenarioIdAsync(Guid scenarioId, CancellationToken ct);
        STT.Task<IEnumerable<ViewModels.Result>> GetByViewIdAsync(Guid viewId, CancellationToken ct);
        STT.Task<IEnumerable<ViewModels.Result>> GetByUserIdAsync(Guid userId, CancellationToken ct);
        STT.Task<IEnumerable<ViewModels.Result>> GetByVmIdAsync(Guid vmId, CancellationToken ct);
        STT.Task<IEnumerable<ViewModels.Result>> GetByTaskIdAsync(Guid taskId, CancellationToken ct);
        STT.Task<ViewModels.Result> CreateAsync(ViewModels.Result result, CancellationToken ct);
        STT.Task<ViewModels.Result> UpdateAsync(Guid id, ViewModels.Result result, CancellationToken ct);
        STT.Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }

    public class ResultService : IResultService
    {
        private readonly SteamfitterContext _context;
        private readonly ClaimsPrincipal _user;
        private readonly IMapper _mapper;

        public ResultService(SteamfitterContext context,
                                            IPrincipal user,
                                            IMapper mapper)
        {
            _context = context;
            _user = user as ClaimsPrincipal;
            _mapper = mapper;
        }

        public async STT.Task<ViewModels.Result> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.Results
                .SingleOrDefaultAsync(o => o.Id == id, ct);

            return _mapper.Map<SAVM.Result>(item);
        }

        public async STT.Task<IEnumerable<ViewModels.Result>> GetByScenarioIdAsync(Guid scenarioId, CancellationToken ct)
        {
            var taskIdList = _context.Tasks.Where(dt => dt.ScenarioId == scenarioId).Select(dt => dt.Id.ToString()).ToList();
            var results = _context.Results.Where(dt => taskIdList.Contains(dt.TaskId.ToString()));

            return _mapper.Map<IEnumerable<ViewModels.Result>>(results);
        }

        public async STT.Task<IEnumerable<ViewModels.Result>> GetByViewIdAsync(Guid viewId, CancellationToken ct)
        {
            var scenarioIdList = _context.Scenarios.Where(s => s.ViewId == viewId).Select(s => s.Id.ToString()).ToList();
            var taskIdList = _context.Tasks.Where(dt => scenarioIdList.Contains(dt.ScenarioId.ToString())).Select(dt => dt.Id.ToString()).ToList();
            var results = _context.Results.Where(r => taskIdList.Contains(r.TaskId.ToString()));

            return _mapper.Map<IEnumerable<ViewModels.Result>>(results);
        }

        public async STT.Task<IEnumerable<ViewModels.Result>> GetByUserIdAsync(Guid userId, CancellationToken ct)
        {
            var results = _context.Results.Where(r => r.CreatedBy == userId);

            return _mapper.Map<IEnumerable<ViewModels.Result>>(results.OrderByDescending(r => r.StatusDate));
        }

        public async STT.Task<IEnumerable<ViewModels.Result>> GetByVmIdAsync(Guid vmId, CancellationToken ct)
        {
            var results = _context.Results.Where(dt => dt.VmId == vmId);

            return _mapper.Map<IEnumerable<ViewModels.Result>>(results);
        }

        public async STT.Task<IEnumerable<SAVM.Result>> GetByTaskIdAsync(Guid taskId, CancellationToken ct)
        {
            var results = await _context.Results.Where(dt => dt.TaskId == taskId).ToListAsync(ct);
            return _mapper.Map<IEnumerable<ViewModels.Result>>(results);
        }

        public async STT.Task<ViewModels.Result> CreateAsync(ViewModels.Result result, CancellationToken ct)
        {
            result.DateCreated = DateTime.UtcNow;
            result.CreatedBy = _user.GetId();
            var resultEntity = _mapper.Map<ResultEntity>(result);

            _context.Results.Add(resultEntity);
            await _context.SaveChangesAsync(ct);

            return await GetAsync(resultEntity.Id, ct);
        }

        public async STT.Task<ViewModels.Result> UpdateAsync(Guid id, ViewModels.Result result, CancellationToken ct)
        {
            var resultToUpdate = await _context.Results.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (resultToUpdate == null)
                throw new EntityNotFoundException<SAVM.Result>();

            result.DateCreated = resultToUpdate.DateCreated;
            result.CreatedBy = resultToUpdate.CreatedBy;
            result.DateModified = DateTime.UtcNow;
            result.ModifiedBy = _user.GetId();
            _mapper.Map(result, resultToUpdate);

            _context.Results.Update(resultToUpdate);
            await _context.SaveChangesAsync(ct);

            return _mapper.Map(resultToUpdate, result);
        }

        public async STT.Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            var ResultToDelete = await _context.Results.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (ResultToDelete == null)
                throw new EntityNotFoundException<SAVM.Result>();

            _context.Results.Remove(ResultToDelete);
            await _context.SaveChangesAsync(ct);

            return true;
        }

    }
}
