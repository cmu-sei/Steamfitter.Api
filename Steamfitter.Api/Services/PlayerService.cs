// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Player.Api.Client;
using STT = System.Threading.Tasks;


namespace Steamfitter.Api.Services
{
    public interface IPlayerService
    {
        STT.Task<IEnumerable<View>> GetViewsAsync(CancellationToken ct);
    }

    public class PlayerService : IPlayerService
    {
        private readonly IPlayerApiClient _playerApiClient;

        public PlayerService(
            IPlayerApiClient playerApiClient)
        {
            _playerApiClient = playerApiClient;
        }

        public async STT.Task<IEnumerable<View>> GetViewsAsync(CancellationToken ct)
        {
            var views = await _playerApiClient.GetMyViewsAsync(ct);
            return (IEnumerable<View>)views;
        }

    }
}
