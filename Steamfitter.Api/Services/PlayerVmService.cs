// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Player.Vm.Api;
using Player.Vm.Api.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using STT = System.Threading.Tasks;

namespace Steamfitter.Api.Services
{
    public interface IPlayerVmService
    {
        STT.Task<IEnumerable<Vm>> GetViewVmsAsync(Guid viewId, CancellationToken ct);
    }

    public class PlayerVmService : IPlayerVmService
    {
        private readonly IPlayerVmApiClient _playerVmApiClient;
        private readonly Guid _userId;

        public PlayerVmService(
            IPlayerVmApiClient playerVmApiClient)
        {
            _playerVmApiClient = playerVmApiClient;
        }

        public async STT.Task<IEnumerable<Vm>> GetViewVmsAsync(Guid viewId, CancellationToken ct)
        {
            if (_playerVmApiClient == null)
            {
                return null;
            }
            var vms = await _playerVmApiClient.GetViewVmsAsync(viewId, null, true, false, ct);
            return (IEnumerable<Vm>)vms;
        }

    }
}
