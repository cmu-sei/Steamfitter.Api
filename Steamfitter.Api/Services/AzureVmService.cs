// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Text.Json;
using STT = System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steamfitter.Api.Infrastructure.Options;

namespace Steamfitter.Api.Services
{
    public interface IAzureVmService
    {
        STT.Task<string> GetVms(string parameters);
        STT.Task<string> VmPowerOn(string parameters);
        STT.Task<string> VmPowerOff(string parameters);
        STT.Task<string> RunShellScript(string parameters);
    }

    public class AzureVmService : IAzureVmService
    {
        private readonly ILogger<AzureVmService> _logger;
        private readonly IOptionsMonitor<AzureOptions> _azureOptions;

        public AzureVmService(
            ILogger<AzureVmService> logger,
            IOptionsMonitor<AzureOptions> azureOptions)
        {
            _logger = logger;
            _azureOptions = azureOptions;
        }

        public async STT.Task<string> GetVms(string parameters)
        {
            var resourceGroup = await ResolveResourceGroupAsync(parameters);
            var pageable = resourceGroup.GetVirtualMachines();
            var names = new System.Collections.Generic.List<string>();
            foreach (var vm in pageable)
                names.Add(vm.Data.Name);
            return JsonSerializer.Serialize(names);
        }

        public async STT.Task<string> VmPowerOn(string parameters)
        {
            var (vm, _) = await GetVmAsync(parameters);
            var op = await vm.PowerOnAsync(Azure.WaitUntil.Completed);
            return op.HasCompleted ? "PowerOn complete" : "PowerOn submitted";
        }

        public async STT.Task<string> VmPowerOff(string parameters)
        {
            var (vm, _) = await GetVmAsync(parameters);
            var op = await vm.PowerOffAsync(Azure.WaitUntil.Completed);
            return op.HasCompleted ? "PowerOff complete" : "PowerOff submitted";
        }

        public async STT.Task<string> RunShellScript(string parameters)
        {
            var p = JsonSerializer.Deserialize<ShellScriptParameters>(parameters);
            var (vm, _) = await GetVmByNameAsync(p.VmName);

            var commandId = string.Equals(p.Shell, "PowerShell", StringComparison.OrdinalIgnoreCase)
                ? "RunPowerShellScript"
                : "RunShellScript";

            var input = new RunCommandInput(commandId);
            input.Script.Add(p.Script ?? string.Empty);
            if (!string.IsNullOrEmpty(p.Params))
            {
                foreach (var pair in p.Params.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var idx = pair.IndexOf('=');
                    if (idx <= 0) continue;
                    input.Parameters.Add(new RunCommandInputParameter(pair.Substring(0, idx).Trim(), pair.Substring(idx + 1).Trim()));
                }
            }

            var op = await vm.RunCommandAsync(Azure.WaitUntil.Completed, input);
            var result = op.Value;
            return string.Join("\n", result.Value.Select(v => v.Message));
        }

        private async STT.Task<Azure.ResourceManager.Resources.ResourceGroupResource> ResolveResourceGroupAsync(string parameters)
        {
            var options = _azureOptions.CurrentValue;
            string resourceGroupName = options.ResourceGroup;

            if (!string.IsNullOrEmpty(parameters))
            {
                using var doc = JsonDocument.Parse(parameters);
                if (doc.RootElement.TryGetProperty("ResourceGroup", out var rgEl) && !string.IsNullOrEmpty(rgEl.GetString()))
                    resourceGroupName = rgEl.GetString();
            }

            if (string.IsNullOrEmpty(resourceGroupName))
                throw new InvalidOperationException("Azure resource group is not configured.");

            var client = BuildArmClient();
            var subscription = await client.GetDefaultSubscriptionAsync();
            return await subscription.GetResourceGroups().GetAsync(resourceGroupName);
        }

        private async STT.Task<(VirtualMachineResource Vm, Azure.ResourceManager.Resources.ResourceGroupResource Rg)> GetVmAsync(string parameters)
        {
            using var doc = JsonDocument.Parse(parameters);
            string vmName = doc.RootElement.TryGetProperty("VmName", out var n) ? n.GetString() : null;
            if (string.IsNullOrEmpty(vmName))
                throw new ArgumentException("VmName is required.");
            return await GetVmByNameAsync(vmName, parameters);
        }

        private async STT.Task<(VirtualMachineResource Vm, Azure.ResourceManager.Resources.ResourceGroupResource Rg)> GetVmByNameAsync(string vmName, string parameters = null)
        {
            var rg = await ResolveResourceGroupAsync(parameters);
            var vm = await rg.GetVirtualMachines().GetAsync(vmName);
            return (vm, rg);
        }

        private ArmClient BuildArmClient()
        {
            var options = _azureOptions.CurrentValue;
            var credentialOptions = new ClientSecretCredentialOptions();
            if (!string.IsNullOrEmpty(options.AuthorityHost))
                credentialOptions.AuthorityHost = new Uri(options.AuthorityHost);

            var credential = new ClientSecretCredential(
                options.TenantId, options.ClientId, options.ClientSecret, credentialOptions);

            var armClientOptions = new ArmClientOptions();
            if (!string.IsNullOrEmpty(options.ArmEndpoint))
                armClientOptions.Environment = new ArmEnvironment(new Uri(options.ArmEndpoint), $"{options.ArmEndpoint.TrimEnd('/')}/.default");

            return new ArmClient(credential, options.SubscriptionId, armClientOptions);
        }

        private class ShellScriptParameters
        {
            public string VmName { get; set; }
            public string Script { get; set; }
            public string Params { get; set; }
            public string Shell { get; set; }
        }
    }
}
