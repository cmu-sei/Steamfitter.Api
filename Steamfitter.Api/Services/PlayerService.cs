// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Logging;
using Player.Api.Client;
using STT = System.Threading.Tasks;


namespace Steamfitter.Api.Services
{
    public interface IPlayerService
    {
        STT.Task<IEnumerable<View>> GetViewsAsync(CancellationToken ct);
        STT.Task<bool> CanManageViewAsync(Guid viewId, CancellationToken ct);
    }

    public class PlayerService : IPlayerService
    {
        private readonly IPlayerApiClient _playerApiClient;
        private readonly ILogger<PlayerService> _logger;

        public PlayerService(
            IPlayerApiClient playerApiClient,
            ILogger<PlayerService> logger)
        {
            _playerApiClient = playerApiClient;
            _logger = logger;
        }

        public async STT.Task<IEnumerable<View>> GetViewsAsync(CancellationToken ct)
        {
            var views = await _playerApiClient.GetMyViewsAsync(ct);
            return (IEnumerable<View>)views;
        }

        /// <summary>
        /// Whether the caller, by their own token, holds ManageView on the View in Player, or the ManageViews
        /// system permission there. Membership of the View alone is not enough: a Scenario bound to a View runs
        /// its Tasks on every VM of that View with the service account.
        /// </summary>
        public async STT.Task<bool> CanManageViewAsync(Guid viewId, CancellationToken ct)
        {
            // a call Player refuses or cannot answer grants nothing
            try
            {
                var viewPermissions = await _playerApiClient.GetMyTeamPermissionsAsync(viewId, null, true, ct);
                if (viewPermissions.Any(x => x.ViewId == viewId && x.PermissionValues.Contains(ManageViewPermission)))
                    return true;
            }
            catch (Exception ex) when (IsUnanswered(ex, ct))
            {
                _logger.LogWarning(ex, "Player did not answer the caller's permissions on View {ViewId}", viewId);
            }

            try
            {
                var systemPermissions = await _playerApiClient.GetMyPermissionsAsync(ct);
                return systemPermissions.Contains(ManageViewsPermission);
            }
            catch (Exception ex) when (IsUnanswered(ex, ct))
            {
                _logger.LogWarning(ex, "Player did not answer the caller's system permissions");
                return false;
            }
        }

        // Player refusing the call, or down, unreachable or timing out. The request being cancelled still cancels it.
        private static bool IsUnanswered(Exception ex, CancellationToken ct) =>
            ex is ApiException or HttpRequestException
            || (ex is OperationCanceledException && !ct.IsCancellationRequested);

        private const string ManageViewsPermission = "ManageViews";
        private const string ManageViewPermission = "ManageView";

    }
}
