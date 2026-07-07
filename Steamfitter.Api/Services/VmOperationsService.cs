// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using STT = System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Player.Vm.Api;
using Steamfitter.Api.Infrastructure.Exceptions;
using Steamfitter.Api.Infrastructure.Extensions;
using Steamfitter.Api.Infrastructure.JsonConverters;
using Steamfitter.Api.Infrastructure.Options;
using ClientOptions = Steamfitter.Api.Infrastructure.Options.ClientOptions;

namespace Steamfitter.Api.Services
{
    public interface IVmOperationsService
    {
        STT.Task<string> GuestCommand(string parameters);
        STT.Task<string> GuestCommandFast(string parameters);
        STT.Task<string> GuestReadFile(string parameters);
        STT.Task<string> GuestFileUploadContent(string parameters);
        STT.Task<string> GuestFileUploadFile(string parameters);
        STT.Task<string> VmPowerOn(string parameters);
        STT.Task<string> VmPowerOff(string parameters);
        STT.Task<string> VmSnapshotCreate(string parameters);
        STT.Task<string> VmSnapshotRevert(string parameters);
        STT.Task<string> VmSnapshotDelete(string parameters);
    }

    /// <summary>
    /// One of the four VM provider kinds Player VM API tracks per VM. Mapped from the Player VM
    /// API VmType enum (see <see cref="VmOperationsService.MapProvider"/>); Steamfitter dispatch
    /// is one branch per provider.
    /// </summary>
    internal enum VmProvider
    {
        Unknown = 0,
        Vsphere = 1,
        Proxmox = 2,
        Azure = 3
    }

    public class VmOperationsService : IVmOperationsService
    {
        private const string VmTypeCacheKeyPrefix = "VmOperationsService:VmType:";
        private static readonly TimeSpan VmTypeCacheTtl = TimeSpan.FromMinutes(5);

        private readonly ILogger<VmOperationsService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptionsMonitor<ClientOptions> _clientOptions;
        private readonly IOptionsMonitor<FilesOptions> _filesOptions;
        private readonly IMemoryCache _memoryCache;

        public VmOperationsService(
            ILogger<VmOperationsService> logger,
            IServiceScopeFactory scopeFactory,
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<ClientOptions> clientOptions,
            IOptionsMonitor<FilesOptions> filesOptions,
            IMemoryCache memoryCache)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _httpClientFactory = httpClientFactory;
            _clientOptions = clientOptions;
            _filesOptions = filesOptions;
            _memoryCache = memoryCache;
        }

        // -------- Lifecycle --------

        public STT.Task<string> VmPowerOn(string parameters) =>
            LogOnFailureAsync(nameof(VmPowerOn), async () =>
            {
                var vmId = ParseMoidAsGuid(parameters);
                var provider = await GetVmProviderAsync(vmId);

                using var scope = _scopeFactory.CreateScope();
                var client = await BuildTypedClientAsync(scope);

                var result = provider switch
                {
                    VmProvider.Vsphere => await client.PowerOnVsphereVirtualMachineAsync(vmId),
                    VmProvider.Proxmox => await client.PowerOnProxmoxVirtualMachineAsync(vmId),
                    _ => throw UnsupportedProviderException(provider, vmId, nameof(VmPowerOn)),
                };
                return result ?? string.Empty;
            });

        public STT.Task<string> VmPowerOff(string parameters) =>
            LogOnFailureAsync(nameof(VmPowerOff), async () =>
            {
                var vmId = ParseMoidAsGuid(parameters);
                var provider = await GetVmProviderAsync(vmId);

                using var scope = _scopeFactory.CreateScope();
                var client = await BuildTypedClientAsync(scope);

                var result = provider switch
                {
                    VmProvider.Vsphere => await client.PowerOffVsphereVirtualMachineAsync(vmId),
                    VmProvider.Proxmox => await client.PowerOffProxmoxVirtualMachineAsync(vmId),
                    _ => throw UnsupportedProviderException(provider, vmId, nameof(VmPowerOff)),
                };
                return result ?? string.Empty;
            });

        // -------- Guest ops --------

        public STT.Task<string> GuestCommand(string parameters) =>
            LogOnFailureAsync(nameof(GuestCommand), async () =>
            {
                var p = JsonSerializer.Deserialize<GuestProcessParameters>(parameters);
                var vmId = Guid.Parse(p.Moid);
                var provider = await GetVmProviderAsync(vmId);

                using var scope = _scopeFactory.CreateScope();
                var client = await BuildTypedClientAsync(scope);

                GuestProcessResult result = provider switch
                {
                    VmProvider.Vsphere => await client.RunGuestProcessOnVsphereVirtualMachineAsync(vmId, new RunGuestProcessOnVsphereVirtualMachine
                    {
                        Username = p.Username,
                        Password = p.Password,
                        ProgramPath = p.CommandText,
                        Arguments = p.CommandArgs,
                        WorkingDirectory = p.CommandWorkDirectory,
                        TimeoutSeconds = p.TimeoutSeconds,
                    }),
                    VmProvider.Proxmox => await client.RunGuestProcessOnProxmoxVirtualMachineAsync(vmId, new RunGuestProcessOnProxmoxVirtualMachine
                    {
                        ProgramPath = p.CommandText,
                        Arguments = p.CommandArgs,
                        WorkingDirectory = p.CommandWorkDirectory,
                        TimeoutSeconds = p.TimeoutSeconds,
                    }),
                    _ => throw UnsupportedProviderException(provider, vmId, nameof(GuestCommand)),
                };

                var output = result?.Output ?? string.Empty;

                // The guest process ran but reported failure (non-zero exit). Surface the command's
                // output/error and fail the task regardless of any ExpectedOutput match.
                if (result != null && !result.Success)
                {
                    var detail = string.IsNullOrEmpty(result.Error) ? output : $"{output}\n{result.Error}";
                    throw new VmCommandFailedException(
                        $"Guest process exited with code {result.ExitCode}.\n{detail}".TrimEnd(),
                        result.ExitCode);
                }

                return output;
            });

        public STT.Task<string> GuestCommandFast(string parameters) =>
            LogOnFailureAsync(nameof(GuestCommandFast), async () =>
            {
                var p = JsonSerializer.Deserialize<GuestProcessParameters>(parameters);
                var vmId = Guid.Parse(p.Moid);
                var provider = await GetVmProviderAsync(vmId);

                using var scope = _scopeFactory.CreateScope();
                var client = await BuildTypedClientAsync(scope);

                long result = provider switch
                {
                    VmProvider.Vsphere => await client.RunGuestProcessFastOnVsphereVirtualMachineAsync(vmId, new RunGuestProcessFastOnVsphereVirtualMachine
                    {
                        Username = p.Username,
                        Password = p.Password,
                        ProgramPath = p.CommandText,
                        Arguments = p.CommandArgs,
                        WorkingDirectory = p.CommandWorkDirectory,
                    }),
                    VmProvider.Proxmox => await client.RunGuestProcessFastOnProxmoxVirtualMachineAsync(vmId, new RunGuestProcessFastOnProxmoxVirtualMachine
                    {
                        ProgramPath = p.CommandText,
                        Arguments = p.CommandArgs,
                        WorkingDirectory = p.CommandWorkDirectory,
                    }),
                    _ => throw UnsupportedProviderException(provider, vmId, nameof(GuestCommandFast)),
                };
                return result.ToString();
            });

        public STT.Task<string> GuestReadFile(string parameters) =>
            LogOnFailureAsync(nameof(GuestReadFile), async () =>
            {
                var p = JsonSerializer.Deserialize<GuestFileReadParameters>(parameters);
                var vmId = Guid.Parse(p.Moid);
                var provider = await GetVmProviderAsync(vmId);

                using var scope = _scopeFactory.CreateScope();
                var client = await BuildTypedClientAsync(scope);

                var result = provider switch
                {
                    VmProvider.Vsphere => await client.ReadGuestFileFromVsphereVirtualMachineAsync(vmId, new ReadGuestFileFromVsphereVirtualMachine
                    {
                        Username = p.Username,
                        Password = p.Password,
                        GuestFilePath = p.GuestFilePath,
                    }),
                    VmProvider.Proxmox => await client.ReadGuestFileFromProxmoxVirtualMachineAsync(vmId, new ReadGuestFileFromProxmoxVirtualMachine
                    {
                        GuestFilePath = p.GuestFilePath,
                    }),
                    _ => throw UnsupportedProviderException(provider, vmId, nameof(GuestReadFile)),
                };
                return result ?? string.Empty;
            });

        public STT.Task<string> GuestFileUploadContent(string parameters) =>
            LogOnFailureAsync(nameof(GuestFileUploadContent), async () =>
            {
                var validJson = parameters.Replace("\r\n", "<*0x0A*>").Replace("\n", "<*0x0A*>");
                var p = JsonSerializer.Deserialize<GuestFileWriteParameters>(validJson);
                var content = p.GuestFileContent?.Replace("<*0x0A*>", "\r\n") ?? string.Empty;

                var vmId = Guid.Parse(p.Moid);
                var fileName = Path.GetFileName(p.GuestFilePath?.TrimEnd('/', '\\') ?? string.Empty);
                var directory = string.IsNullOrEmpty(fileName)
                    ? p.GuestFilePath
                    : p.GuestFilePath.Substring(0, p.GuestFilePath.Length - fileName.Length);

                var bytes = Encoding.UTF8.GetBytes(content);
                using var memStream = new MemoryStream(bytes);

                return await UploadFileAsync(
                    vmId,
                    username: p.Username,
                    password: p.Password,
                    directoryPath: directory,
                    fileName: string.IsNullOrEmpty(fileName) ? "uploaded.txt" : fileName,
                    fileContent: memStream,
                    callerName: nameof(GuestFileUploadContent));
            });

        public STT.Task<string> GuestFileUploadFile(string parameters) =>
            LogOnFailureAsync(nameof(GuestFileUploadFile), async () =>
            {
                var p = JsonSerializer.Deserialize<GuestFileUploadFileParameters>(parameters);
                var vmId = Guid.Parse(p.Moid);

                var localPath = ResolveLocalFilePath(p.FilePath);
                if (!File.Exists(localPath))
                    throw new FileNotFoundException($"Local file not found: {localPath}", localPath);

                var fileName = Path.GetFileName(localPath);
                using var fileStream = File.OpenRead(localPath);

                return await UploadFileAsync(
                    vmId,
                    username: p.Username,
                    password: p.Password,
                    directoryPath: p.GuestFilePath,
                    fileName: fileName,
                    fileContent: fileStream,
                    callerName: nameof(GuestFileUploadFile));
            });

        // -------- Provisioning --------

        // -------- Snapshots --------

        public STT.Task<string> VmSnapshotCreate(string parameters) =>
            LogOnFailureAsync(nameof(VmSnapshotCreate), async () =>
            {
                var p = JsonSerializer.Deserialize<SnapshotCreateParameters>(parameters);
                var vmId = Guid.Parse(p.Moid);
                var provider = await GetVmProviderAsync(vmId);

                using var scope = _scopeFactory.CreateScope();
                var client = await BuildTypedClientAsync(scope);

                var result = provider switch
                {
                    VmProvider.Vsphere => await client.CreateVsphereVirtualMachineSnapshotAsync(vmId, new CreateVsphereVirtualMachineSnapshot
                    {
                        SnapshotName = p.SnapshotName,
                        Description = p.Description,
                        IncludeMemory = p.IncludeRam,
                    }),
                    VmProvider.Proxmox => await client.CreateProxmoxVirtualMachineSnapshotAsync(vmId, new CreateProxmoxVirtualMachineSnapshot
                    {
                        SnapshotName = p.SnapshotName,
                        Description = p.Description,
                        IncludeRam = p.IncludeRam,
                    }),
                    _ => throw UnsupportedProviderException(provider, vmId, nameof(VmSnapshotCreate)),
                };
                return result ?? string.Empty;
            });

        public STT.Task<string> VmSnapshotRevert(string parameters) =>
            LogOnFailureAsync(nameof(VmSnapshotRevert), async () =>
            {
                var p = JsonSerializer.Deserialize<SnapshotNameParameters>(parameters);
                var vmId = Guid.Parse(p.Moid);
                var provider = await GetVmProviderAsync(vmId);

                using var scope = _scopeFactory.CreateScope();
                var client = await BuildTypedClientAsync(scope);

                switch (provider)
                {
                    case VmProvider.Vsphere:
                        await client.RevertToVsphereVirtualMachineSnapshotAsync(vmId, new RevertToVsphereVirtualMachineSnapshot
                        {
                            SnapshotId = p.SnapshotName,
                        });
                        return string.Empty;
                    case VmProvider.Proxmox:
                        {
                            var result = await client.RevertProxmoxVirtualMachineSnapshotAsync(vmId, p.SnapshotName);
                            return result ?? string.Empty;
                        }
                    default:
                        throw UnsupportedProviderException(provider, vmId, nameof(VmSnapshotRevert));
                }
            });

        public STT.Task<string> VmSnapshotDelete(string parameters) =>
            LogOnFailureAsync(nameof(VmSnapshotDelete), async () =>
            {
                var p = JsonSerializer.Deserialize<SnapshotNameParameters>(parameters);
                var vmId = Guid.Parse(p.Moid);
                var provider = await GetVmProviderAsync(vmId);

                using var scope = _scopeFactory.CreateScope();
                var client = await BuildTypedClientAsync(scope);

                var result = provider switch
                {
                    VmProvider.Vsphere => await client.DeleteVsphereVirtualMachineSnapshotAsync(vmId, p.SnapshotName),
                    VmProvider.Proxmox => await client.DeleteProxmoxVirtualMachineSnapshotAsync(vmId, p.SnapshotName),
                    _ => throw UnsupportedProviderException(provider, vmId, nameof(VmSnapshotDelete)),
                };
                return result ?? string.Empty;
            });

        private async STT.Task<string> LogOnFailureAsync(string operation, Func<STT.Task<string>> action)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{operation} failed", operation);
                throw;
            }
        }

        // -------- Helpers --------

        private async STT.Task<string> UploadFileAsync(
            Guid vmId,
            string username,
            string password,
            string directoryPath,
            string fileName,
            Stream fileContent,
            string callerName)
        {
            var provider = await GetVmProviderAsync(vmId);

            using var scope = _scopeFactory.CreateScope();
            var client = await BuildTypedClientAsync(scope);

            var files = new[] { new FileParameter(fileContent, fileName, "application/octet-stream") };

            var result = provider switch
            {
                VmProvider.Vsphere => await client.UploadFileToVsphereVirtualMachineAsync(
                    vmId, username ?? string.Empty, password ?? string.Empty, directoryPath ?? string.Empty, files),
                VmProvider.Proxmox => await client.UploadFileToProxmoxVirtualMachineAsync(
                    vmId, directoryPath ?? string.Empty, files),
                _ => throw UnsupportedProviderException(provider, vmId, callerName),
            };
            return result ?? string.Empty;
        }

        private static Guid ParseMoidAsGuid(string parameters)
        {
            using var doc = JsonDocument.Parse(parameters);
            var moid = doc.RootElement.GetProperty("Moid").GetString();
            return Guid.Parse(moid);
        }

        private async STT.Task<PlayerVmApiClient> BuildTypedClientAsync(IServiceScope scope)
        {
            var token = await ApiClientsExtensions.GetToken(scope);
            return VmApiExtensions.GetVmApiClient(_httpClientFactory, _clientOptions.CurrentValue.urls.vmApi, token);
        }

        /// <summary>
        /// Resolve the VM's provider via Player VM API. Cached for VmTypeCacheTtl. The provider
        /// is the source of truth for whether a VM is vSphere, Proxmox, etc — Steamfitter dispatch
        /// is one branch per provider.
        /// </summary>
        private async STT.Task<VmProvider> GetVmProviderAsync(Guid vmId)
        {
            var cacheKey = VmTypeCacheKeyPrefix + vmId;
            if (_memoryCache.TryGetValue<VmProvider>(cacheKey, out var cached))
                return cached;

            using var scope = _scopeFactory.CreateScope();
            var client = await BuildTypedClientAsync(scope);

            var vm = await client.GetVmAsync(vmId);
            var provider = MapProvider(vm.Type);
            _memoryCache.Set(cacheKey, provider, VmTypeCacheTtl);
            return provider;
        }

        private static VmProvider MapProvider(VmType type) => type switch
        {
            VmType.Vsphere => VmProvider.Vsphere,
            VmType.Proxmox => VmProvider.Proxmox,
            VmType.Azure => VmProvider.Azure,
            _ => VmProvider.Unknown,
        };

        private static NotSupportedException UnsupportedProviderException(VmProvider provider, Guid vmId, string operation) =>
            new NotSupportedException($"VM {vmId} has provider '{provider}', which does not support operation '{operation}'.");

        private string ResolveLocalFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return filePath;
            if (Path.IsPathRooted(filePath))
                return filePath;
            var localDir = _filesOptions.CurrentValue?.LocalDirectory ?? string.Empty;
            return Path.Combine(localDir, filePath);
        }

        // ---- Parameter DTOs (mirror legacy StackStorm DTOs in shape and casing) ----

        private class GuestProcessParameters
        {
            public string Moid { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string CommandText { get; set; }
            public string CommandArgs { get; set; }
            public string CommandWorkDirectory { get; set; }

            [JsonConverter(typeof(JsonNullableIntegerConverter))]
            public int? TimeoutSeconds { get; set; }
        }

        private class GuestFileReadParameters
        {
            public string Moid { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string GuestFilePath { get; set; }
        }

        private class GuestFileWriteParameters
        {
            public string Moid { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string GuestFilePath { get; set; }
            public string GuestFileContent { get; set; }
        }

        private class GuestFileUploadFileParameters
        {
            public string Moid { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string GuestFilePath { get; set; }
            public string FilePath { get; set; }
        }

        private class SnapshotCreateParameters
        {
            public string Moid { get; set; }
            public string SnapshotName { get; set; }
            public string Description { get; set; }
            [JsonConverter(typeof(JsonFlexibleBoolConverter))]
            public bool IncludeRam { get; set; }
        }

        private class SnapshotNameParameters
        {
            public string Moid { get; set; }
            public string SnapshotName { get; set; }
        }
    }
}
