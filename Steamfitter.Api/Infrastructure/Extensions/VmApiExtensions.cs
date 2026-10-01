// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Player.Vm.Api;

namespace Steamfitter.Api.Infrastructure.Extensions
{
    public static class VmApiExtensions
    {
        public static PlayerVmApiClient GetVmApiClient(IHttpClientFactory httpClientFactory, string apiUrl, TokenResponse tokenResponse)
        {
            var client = ApiClientsExtensions.GetHttpClient(httpClientFactory, apiUrl, tokenResponse);
            var apiClient = new PlayerVmApiClient(client);
            return apiClient;
        }

        /// <summary>
        /// Every Vm in the View, including personal ones. Steamfitter's account holds the Vm permissions
        /// at the system level but is on none of the View's teams, so the View's own Vm listing, which
        /// only shows what a member can reach, would come back empty.
        /// </summary>
        public static async Task<IEnumerable<Vm>> GetAllViewVmsAsync(PlayerVmApiClient playerVmApiClient, Guid viewId, CancellationToken ct)
        {
            var vms = await playerVmApiClient.GetAllViewVmsAsync(viewId, ct);
            return vms;
        }

    }
}
