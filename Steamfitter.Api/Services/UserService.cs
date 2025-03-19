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
using Microsoft.Extensions.Logging;
using Steamfitter.Api.Data;
using Steamfitter.Api.Data.Models;
using Steamfitter.Api.Infrastructure.Extensions;
using Steamfitter.Api.Infrastructure.Exceptions;
using SAVM = Steamfitter.Api.ViewModels;

namespace Steamfitter.Api.Services
{
    public interface IUserService
    {
        STT.Task<IEnumerable<ViewModels.User>> GetAsync(CancellationToken ct);
        STT.Task<ViewModels.User> GetAsync(Guid id, CancellationToken ct);
        STT.Task<ViewModels.User> CreateAsync(ViewModels.User user, CancellationToken ct);
        STT.Task<ViewModels.User> UpdateAsync(Guid id, ViewModels.User user, CancellationToken ct);
        STT.Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }

    public class UserService : IUserService
    {
        private readonly SteamfitterContext _context;
        private readonly ClaimsPrincipal _user;
        private readonly IAuthorizationService _authorizationService;
        private readonly IUserClaimsService _userClaimsService;
        private readonly IMapper _mapper;
        private readonly ILogger<IUserService> _logger;

        public UserService(SteamfitterContext context, IPrincipal user, IAuthorizationService authorizationService, IUserClaimsService userClaimsService, ILogger<IUserService> logger, IMapper mapper)
        {
            _context = context;
            _user = user as ClaimsPrincipal;
            _authorizationService = authorizationService;
            _userClaimsService = userClaimsService;
            _mapper = mapper;
            _logger = logger;
        }

        public async STT.Task<IEnumerable<ViewModels.User>> GetAsync(CancellationToken ct)
        {
            var items = await _context.Users
                .ToArrayAsync(ct);
            return _mapper.Map<IEnumerable<SAVM.User>>(items);
        }

        public async STT.Task<ViewModels.User> GetAsync(Guid id, CancellationToken ct)
        {
            var item = await _context.Users
                .SingleOrDefaultAsync(o => o.Id == id, ct);
            return _mapper.Map<SAVM.User>(item);
        }

        public async STT.Task<ViewModels.User> CreateAsync(ViewModels.User user, CancellationToken ct)
        {
            var userEntity = _mapper.Map<UserEntity>(user);

            _context.Users.Add(userEntity);
            await _context.SaveChangesAsync(ct);
            _logger.LogWarning($"User {user.Name} ({userEntity.Id}) created by {_user.GetId()}");
            return await GetAsync(user.Id, ct);
        }

        public async STT.Task<ViewModels.User> UpdateAsync(Guid id, ViewModels.User user, CancellationToken ct)
        {
            // Don't allow changing your own Id
            if (id == _user.GetId() && id != user.Id)
            {
                throw new ForbiddenException("You cannot change your own Id");
            }

            var userToUpdate = await _context.Users.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (userToUpdate == null)
                throw new EntityNotFoundException<SAVM.User>();

            _mapper.Map(user, userToUpdate);

            _context.Users.Update(userToUpdate);
            await _context.SaveChangesAsync(ct);
            _logger.LogWarning($"User {userToUpdate.Name} ({userToUpdate.Id}) updated by {_user.GetId()}");
            return await GetAsync(id, ct);
        }

        public async STT.Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        {
            if (id == _user.GetId())
            {
                throw new ForbiddenException("You cannot delete your own account");
            }

            var userToDelete = await _context.Users.SingleOrDefaultAsync(v => v.Id == id, ct);

            if (userToDelete == null)
                throw new EntityNotFoundException<SAVM.User>();

            _context.Users.Remove(userToDelete);
            await _context.SaveChangesAsync(ct);
            _logger.LogWarning($"User {userToDelete.Name} ({userToDelete.Id}) deleted by {_user.GetId()}");
            return true;
        }

    }
}
