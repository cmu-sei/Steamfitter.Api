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
            var apiClient = new PlayerVmApiClient(client, true)
            {
                BaseUri = client.BaseAddress
            };
            return apiClient;
        }

        public static async Task<IEnumerable<Player.Vm.Api.Models.Vm>> GetViewVmsAsync(PlayerVmApiClient playerVmApiClient, Guid viewId, CancellationToken ct)
        {
            try
            {
                var vms = (await playerVmApiClient.GetViewVmsAsync(viewId, null, true, false, ct)) as IEnumerable<Player.Vm.Api.Models.Vm>;
                return vms;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

    }
}
