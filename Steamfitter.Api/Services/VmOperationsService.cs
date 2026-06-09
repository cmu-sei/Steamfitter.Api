// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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
        STT.Task<string> CreateVmFromTemplate(string parameters);
        STT.Task<string> VmRemove(string parameters);
        STT.Task<string> VmSnapshotCreate(string parameters);
        STT.Task<string> VmSnapshotRevert(string parameters);
        STT.Task<string> VmSnapshotDelete(string parameters);
    }

    /// <summary>
    /// One of the four VM provider kinds Player VM API tracks per VM. String-cased to match the
    /// Player VM API VmType enum exactly so we can parse it directly from JSON.
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

        // -------- Lifecycle (typed-client where possible) --------

        public STT.Task<string> VmPowerOn(string parameters) =>
            LogOnFailureAsync(nameof(VmPowerOn), async () =>
            {
                var vmId = ParseMoidAsGuid(parameters);
                var provider = await GetVmProviderAsync(vmId);

                switch (provider)
                {
                    case VmProvider.Vsphere:
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var client = await BuildTypedClientAsync(scope);
                            var result = await client.PowerOnVsphereVirtualMachineAsync(vmId);
                            return result?.ToString() ?? string.Empty;
                        }
                    case VmProvider.Proxmox:
                        return await PostJsonAsync($"/api/vms/proxmox/{vmId}/actions/power-on", null);
                    default:
                        throw UnsupportedProviderException(provider, vmId, nameof(VmPowerOn));
                }
            });

        public STT.Task<string> VmPowerOff(string parameters) =>
            LogOnFailureAsync(nameof(VmPowerOff), async () =>
            {
                var vmId = ParseMoidAsGuid(parameters);
                var provider = await GetVmProviderAsync(vmId);

                switch (provider)
                {
                    case VmProvider.Vsphere:
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var client = await BuildTypedClientAsync(scope);
                            var result = await client.PowerOffVsphereVirtualMachineAsync(vmId);
                            return result?.ToString() ?? string.Empty;
                        }
                    case VmProvider.Proxmox:
                        return await PostJsonAsync($"/api/vms/proxmox/{vmId}/actions/power-off", null);
                    default:
                        throw UnsupportedProviderException(provider, vmId, nameof(VmPowerOff));
                }
            });

        // -------- Guest ops --------

        public STT.Task<string> GuestCommand(string parameters) =>
            LogOnFailureAsync(nameof(GuestCommand), async () =>
            {
                var p = JsonSerializer.Deserialize<GuestProcessParameters>(parameters);
                var vmId = Guid.Parse(p.Moid);

                var body = new
                {
                    username = p.Username,
                    password = p.Password,
                    programPath = p.CommandText,
                    arguments = p.CommandArgs,
                    workingDirectory = p.CommandWorkDirectory,
                    timeoutSeconds = p.TimeoutSeconds
                };

                return await PostJsonAsync(
                    BuildActionPath(await GetVmProviderAsync(vmId), vmId, "run-guest-process", nameof(GuestCommand)),
                    body);
            });

        public STT.Task<string> GuestCommandFast(string parameters) =>
            LogOnFailureAsync(nameof(GuestCommandFast), async () =>
            {
                var p = JsonSerializer.Deserialize<GuestProcessParameters>(parameters);
                var vmId = Guid.Parse(p.Moid);

                var body = new
                {
                    username = p.Username,
                    password = p.Password,
                    programPath = p.CommandText,
                    arguments = p.CommandArgs,
                    workingDirectory = p.CommandWorkDirectory
                };

                return await PostJsonAsync(
                    BuildActionPath(await GetVmProviderAsync(vmId), vmId, "run-guest-process-fast", nameof(GuestCommandFast)),
                    body);
            });

        public STT.Task<string> GuestReadFile(string parameters) =>
            LogOnFailureAsync(nameof(GuestReadFile), async () =>
            {
                var p = JsonSerializer.Deserialize<GuestFileReadParameters>(parameters);
                var vmId = Guid.Parse(p.Moid);

                var body = new
                {
                    username = p.Username,
                    password = p.Password,
                    guestFilePath = p.GuestFilePath
                };

                return await PostJsonAsync(
                    BuildActionPath(await GetVmProviderAsync(vmId), vmId, "read-guest-file", nameof(GuestReadFile)),
                    body);
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

                return await UploadMultipartAsync(
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

                return await UploadMultipartAsync(
                    vmId,
                    username: p.Username,
                    password: p.Password,
                    directoryPath: p.GuestFilePath,
                    fileName: fileName,
                    fileContent: fileStream,
                    callerName: nameof(GuestFileUploadFile));
            });

        // -------- Provisioning --------

        public STT.Task<string> CreateVmFromTemplate(string parameters) =>
            LogOnFailureAsync(nameof(CreateVmFromTemplate), async () =>
            {
                var p = JsonSerializer.Deserialize<CreateVmFromTemplateParameters>(parameters);
                var sourceVmId = Guid.Parse(p.Moid);

                var body = new
                {
                    cloneName = p.Name ?? p.CloneName,
                    powerOn = p.PowerOn
                };

                return await PostJsonAsync(
                    BuildActionPath(await GetVmProviderAsync(sourceVmId), sourceVmId, "clone-from-template", nameof(CreateVmFromTemplate)),
                    body);
            });

        public STT.Task<string> VmRemove(string parameters) =>
            LogOnFailureAsync(nameof(VmRemove), async () =>
            {
                var vmId = ParseMoidAsGuid(parameters);
                var provider = await GetVmProviderAsync(vmId);

                switch (provider)
                {
                    case VmProvider.Vsphere:
                    case VmProvider.Proxmox:
                        {
                            using var scope = _scopeFactory.CreateScope();
                            using var http = await BuildAuthenticatedHttpClientAsync(scope);
                            var path = $"/api/vms/{ProviderSegment(provider)}/{vmId}/actions/delete";
                            var response = await http.PostAsync(path, new StringContent(string.Empty));
                            return await EnsureSuccessOrLogAsync(response, path, nameof(VmRemove));
                        }
                    default:
                        throw UnsupportedProviderException(provider, vmId, nameof(VmRemove));
                }
            });

        // -------- Snapshots --------

        public STT.Task<string> VmSnapshotCreate(string parameters) =>
            LogOnFailureAsync(nameof(VmSnapshotCreate), async () =>
            {
                var p = JsonSerializer.Deserialize<SnapshotCreateParameters>(parameters);
                var vmId = Guid.Parse(p.Moid);
                var provider = await GetVmProviderAsync(vmId);

                return provider switch
                {
                    VmProvider.Vsphere => await PostJsonAsync(
                        $"/api/vms/vsphere/{vmId}/actions/snapshots",
                        new { snapshotName = p.SnapshotName, description = p.Description, includeMemory = p.IncludeRam }),
                    VmProvider.Proxmox => await PostJsonAsync(
                        $"/api/vms/proxmox/{vmId}/actions/snapshots",
                        new { snapshotName = p.SnapshotName, description = p.Description, includeRam = p.IncludeRam }),
                    _ => throw UnsupportedProviderException(provider, vmId, nameof(VmSnapshotCreate)),
                };
            });

        public STT.Task<string> VmSnapshotRevert(string parameters) =>
            LogOnFailureAsync(nameof(VmSnapshotRevert), async () =>
            {
                var p = JsonSerializer.Deserialize<SnapshotNameParameters>(parameters);
                var vmId = Guid.Parse(p.Moid);
                var provider = await GetVmProviderAsync(vmId);

                switch (provider)
                {
                    case VmProvider.Vsphere:
                        return await PostJsonAsync(
                            $"/api/vms/vsphere/{vmId}/actions/revert-to-snapshot",
                            new { snapshotId = p.SnapshotName });
                    case VmProvider.Proxmox:
                        {
                            using var scope = _scopeFactory.CreateScope();
                            using var http = await BuildAuthenticatedHttpClientAsync(scope);
                            var path = $"/api/vms/proxmox/{vmId}/actions/snapshots/{Uri.EscapeDataString(p.SnapshotName)}/revert";
                            var response = await http.PostAsync(path, new StringContent(string.Empty));
                            return await EnsureSuccessOrLogAsync(response, path, nameof(VmSnapshotRevert));
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
                using var http = await BuildAuthenticatedHttpClientAsync(scope);

                string path = provider switch
                {
                    VmProvider.Vsphere => $"/api/vms/vsphere/{vmId}/actions/snapshots/{Uri.EscapeDataString(p.SnapshotName)}",
                    VmProvider.Proxmox => $"/api/vms/proxmox/{vmId}/actions/snapshots/{Uri.EscapeDataString(p.SnapshotName)}",
                    _ => throw UnsupportedProviderException(provider, vmId, nameof(VmSnapshotDelete)),
                };

                var response = await http.DeleteAsync(path);
                return await EnsureSuccessOrLogAsync(response, path, nameof(VmSnapshotDelete));
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

        private async STT.Task<string> UploadMultipartAsync(
            Guid vmId,
            string username,
            string password,
            string directoryPath,
            string fileName,
            Stream fileContent,
            string callerName)
        {
            var provider = await GetVmProviderAsync(vmId);
            var path = BuildActionPath(provider, vmId, "upload-file", callerName);

            using var scope = _scopeFactory.CreateScope();
            using var http = await BuildAuthenticatedHttpClientAsync(scope);

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(username ?? string.Empty), "username");
            form.Add(new StringContent(password ?? string.Empty), "password");
            form.Add(new StringContent(directoryPath ?? string.Empty), "filepath");

            var streamContent = new StreamContent(fileContent);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(streamContent, "files", fileName);

            var response = await http.PostAsync(path, form);
            return await EnsureSuccessOrLogAsync(response, path, callerName);
        }

        private async STT.Task<string> EnsureSuccessOrLogAsync(HttpResponseMessage response, string relativePath, string callerName)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("{caller}: Player VM API call to {path} failed: {status} {body}", callerName, relativePath, response.StatusCode, responseBody);
                response.EnsureSuccessStatusCode();
            }
            return responseBody;
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

        private async STT.Task<HttpClient> BuildAuthenticatedHttpClientAsync(IServiceScope scope)
        {
            var token = await ApiClientsExtensions.GetToken(scope);
            return ApiClientsExtensions.GetHttpClient(_httpClientFactory, _clientOptions.CurrentValue.urls.vmApi, token);
        }

        private async STT.Task<string> PostJsonAsync(string relativePath, object body)
        {
            using var scope = _scopeFactory.CreateScope();
            using var http = await BuildAuthenticatedHttpClientAsync(scope);

            HttpContent content = body == null
                ? new StringContent(string.Empty)
                : new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var response = await http.PostAsync(relativePath, content);
            content.Dispose();

            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Player VM API call to {path} failed: {status} {body}", relativePath, response.StatusCode, responseBody);
                response.EnsureSuccessStatusCode();
            }
            return responseBody;
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
            using var http = await BuildAuthenticatedHttpClientAsync(scope);

            using var response = await http.GetAsync($"/api/vms/{vmId}");
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(body);
            var typeStr = doc.RootElement.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;

            var provider = ParseProvider(typeStr);
            _memoryCache.Set(cacheKey, provider, VmTypeCacheTtl);
            return provider;
        }

        private static VmProvider ParseProvider(string raw) =>
            raw?.ToLowerInvariant() switch
            {
                "vsphere" => VmProvider.Vsphere,
                "proxmox" => VmProvider.Proxmox,
                "azure" => VmProvider.Azure,
                _ => VmProvider.Unknown,
            };

        private static string ProviderSegment(VmProvider provider) => provider switch
        {
            VmProvider.Vsphere => "vsphere",
            VmProvider.Proxmox => "proxmox",
            _ => throw new InvalidOperationException($"No URL segment for provider {provider}"),
        };

        private static string BuildActionPath(VmProvider provider, Guid vmId, string action, string caller)
        {
            if (provider != VmProvider.Vsphere && provider != VmProvider.Proxmox)
                throw UnsupportedProviderException(provider, vmId, caller);

            return $"/api/vms/{ProviderSegment(provider)}/{vmId}/actions/{action}";
        }

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

        private class CreateVmFromTemplateParameters
        {
            public string Moid { get; set; }
            public string Name { get; set; }
            public string CloneName { get; set; }
            [JsonConverter(typeof(JsonFlexibleBoolConverter))]
            public bool PowerOn { get; set; }
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
