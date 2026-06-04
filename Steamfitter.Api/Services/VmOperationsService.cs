// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using STT = System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Player.Vm.Api;
using Steamfitter.Api.Infrastructure.Extensions;
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
    }

    public class VmOperationsService : IVmOperationsService
    {
        private readonly ILogger<VmOperationsService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptionsMonitor<ClientOptions> _clientOptions;
        private readonly IOptionsMonitor<FilesOptions> _filesOptions;

        public VmOperationsService(
            ILogger<VmOperationsService> logger,
            IServiceScopeFactory scopeFactory,
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<ClientOptions> clientOptions,
            IOptionsMonitor<FilesOptions> filesOptions)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _httpClientFactory = httpClientFactory;
            _clientOptions = clientOptions;
            _filesOptions = filesOptions;
        }

        // -------- Typed-client backed operations (already in Player.Vm.Api.Client v1.5.0) --------

        public async STT.Task<string> VmPowerOn(string parameters)
        {
            var vmId = ParseMoidAsGuid(parameters);
            using var scope = _scopeFactory.CreateScope();
            var client = await BuildTypedClientAsync(scope);
            var result = await client.PowerOnVsphereVirtualMachineAsync(vmId);
            return result?.ToString() ?? string.Empty;
        }

        public async STT.Task<string> VmPowerOff(string parameters)
        {
            var vmId = ParseMoidAsGuid(parameters);
            using var scope = _scopeFactory.CreateScope();
            var client = await BuildTypedClientAsync(scope);
            var result = await client.PowerOffVsphereVirtualMachineAsync(vmId);
            return result?.ToString() ?? string.Empty;
        }

        // -------- Raw-HTTP backed operations (new endpoints in Player VM API) --------
        //
        // These endpoints will move to the typed PlayerVmApiClient when the
        // Player.Vm.Api.Client NuGet package is regenerated through the release
        // pipeline (next version bump from 1.5.0 → 1.6.0).

        public async STT.Task<string> GuestCommand(string parameters)
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

            var response = await PostJsonAsync($"/api/vms/vsphere/{vmId}/actions/run-guest-process", body);
            return response;
        }

        public async STT.Task<string> GuestCommandFast(string parameters)
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

            return await PostJsonAsync($"/api/vms/vsphere/{vmId}/actions/run-guest-process-fast", body);
        }

        public async STT.Task<string> GuestReadFile(string parameters)
        {
            var p = JsonSerializer.Deserialize<GuestFileReadParameters>(parameters);
            var vmId = Guid.Parse(p.Moid);

            var body = new
            {
                username = p.Username,
                password = p.Password,
                guestFilePath = p.GuestFilePath
            };

            return await PostJsonAsync($"/api/vms/vsphere/{vmId}/actions/read-guest-file", body);
        }

        public async STT.Task<string> GuestFileUploadContent(string parameters)
        {
            // Match the legacy DTO escape: \r and \n aren't valid JSON, so the caller pre-encodes them.
            var validJson = parameters.Replace("\r\n", "<*0x0A*>").Replace("\n", "<*0x0A*>");
            var p = JsonSerializer.Deserialize<GuestFileWriteParameters>(validJson);
            // Restore CRLF in content (Windows-friendly, also valid on Linux)
            var content = p.GuestFileContent?.Replace("<*0x0A*>", "\r\n") ?? string.Empty;

            var vmId = Guid.Parse(p.Moid);
            var fileName = Path.GetFileName(p.GuestFilePath?.TrimEnd('/', '\\') ?? string.Empty);
            var directory = string.IsNullOrEmpty(fileName)
                ? p.GuestFilePath
                : p.GuestFilePath.Substring(0, p.GuestFilePath.Length - fileName.Length);

            using var scope = _scopeFactory.CreateScope();
            using var http = await BuildAuthenticatedHttpClientAsync(scope);

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(p.Username ?? string.Empty), "username");
            form.Add(new StringContent(p.Password ?? string.Empty), "password");
            form.Add(new StringContent(directory ?? string.Empty), "filepath");

            var bytes = Encoding.UTF8.GetBytes(content);
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(fileContent, "files", string.IsNullOrEmpty(fileName) ? "uploaded.txt" : fileName);

            var response = await http.PostAsync($"/api/vms/vsphere/{vmId}/actions/upload-file", form);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async STT.Task<string> GuestFileUploadFile(string parameters)
        {
            var p = JsonSerializer.Deserialize<GuestFileUploadFileParameters>(parameters);
            var vmId = Guid.Parse(p.Moid);

            var localPath = ResolveLocalFilePath(p.FilePath);
            if (!File.Exists(localPath))
                throw new FileNotFoundException($"Local file not found: {localPath}", localPath);

            var fileName = Path.GetFileName(localPath);

            using var scope = _scopeFactory.CreateScope();
            using var http = await BuildAuthenticatedHttpClientAsync(scope);

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(p.Username ?? string.Empty), "username");
            form.Add(new StringContent(p.Password ?? string.Empty), "password");
            form.Add(new StringContent(p.GuestFilePath ?? string.Empty), "filepath");

            using var fileStream = File.OpenRead(localPath);
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(streamContent, "files", fileName);

            var response = await http.PostAsync($"/api/vms/vsphere/{vmId}/actions/upload-file", form);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async STT.Task<string> CreateVmFromTemplate(string parameters)
        {
            var p = JsonSerializer.Deserialize<CreateVmFromTemplateParameters>(parameters);
            var sourceVmId = Guid.Parse(p.Moid);

            var body = new
            {
                cloneName = p.Name ?? p.CloneName,
                powerOn = p.PowerOn
            };

            return await PostJsonAsync($"/api/vms/vsphere/{sourceVmId}/actions/clone-from-template", body);
        }

        public async STT.Task<string> VmRemove(string parameters)
        {
            var vmId = ParseMoidAsGuid(parameters);
            // delete endpoint takes no body
            using var scope = _scopeFactory.CreateScope();
            using var http = await BuildAuthenticatedHttpClientAsync(scope);

            var response = await http.PostAsync($"/api/vms/vsphere/{vmId}/actions/delete", new StringContent(string.Empty));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        // -------- Helpers --------

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

            var json = JsonSerializer.Serialize(body);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(relativePath, content);

            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Player VM API call to {path} failed: {status} {body}", relativePath, response.StatusCode, responseBody);
                response.EnsureSuccessStatusCode();
            }
            return responseBody;
        }

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
            public bool PowerOn { get; set; }
        }
    }
}
