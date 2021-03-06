// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using STT = System.Threading.Tasks;
using Steamfitter.Api.Infrastructure.Options;
using Stackstorm.Connector;
using Steamfitter.Api.Infrastructure.HealthChecks;
using Stackstorm.Api.Client;
using Stackstorm.Connector.Models.Email;

namespace Steamfitter.Api.Services
{
    public interface IStackStormService : IHostedService
    {
        ConcurrentDictionary<Guid, VmIdentityStrings> GetVmList();
        string GetVmMoid(Guid uuid);
        List<Guid> GetVmGuids(string mask);
        string GetVmName(Guid uuid);
        STT.Task GetStackstormVms();
        STT.Task<string> GuestCommand(string parameters);
        STT.Task<string> GuestCommandFast(string parameters);
        STT.Task<string> GuestReadFile(string parameters);
        STT.Task<string> GuestFileUploadContent(string parameters);
        STT.Task<string> VmPowerOn(string parameters);
        STT.Task<string> VmPowerOff(string parameters);
        STT.Task<string> CreateVmFromTemplate(string parameters);
        STT.Task<string> VmRemove(string parameters);
        STT.Task<string> SendEmail(string parameters);
    }

    public class StackStormService : IStackStormService
    {
        private readonly ILogger<StackStormService> _logger;
        private VmTaskProcessingOptions _options;
        private ConcurrentDictionary<Guid, VmIdentityStrings> _vmList = new ConcurrentDictionary<Guid, VmIdentityStrings>();
        private StackstormConnector _stackStormConnector;

        private readonly StackStormServiceHealthCheck _stackStormServiceHealthCheck;

        public StackStormService(
            IOptions<VmTaskProcessingOptions> options,
            ILogger<StackStormService> logger,
            StackStormServiceHealthCheck stackStormServiceHealthCheck)
        {
            _options = options.Value;
            _logger = logger;
            _stackStormServiceHealthCheck = stackStormServiceHealthCheck;
            _stackStormServiceHealthCheck.HealthAllowance = _options.HealthCheckTimeoutSeconds;
        }

        public ConcurrentDictionary<Guid, VmIdentityStrings> GetVmList()
        {
            return _vmList;
        }

        public string GetVmMoid(Guid uuid)
        {
            return _vmList[uuid].Moid;
        }

        public List<Guid> GetVmGuids(string mask)
        {
            Guid maskGuid;
            var guidList = new List<Guid>();
            if (Guid.TryParse(mask, out maskGuid))
            {
                guidList.Add(maskGuid);
                return guidList;
            }

            var matchingVms = _vmList.OrderBy(vm => vm.Value.Name).Where(vm => vm.Value.Name.ToLower().Contains(mask.ToLower())).Select(vm => vm.Key);
            return matchingVms.ToList();
        }

        public string GetVmName(Guid uuid)
        {
            return _vmList[uuid].Name;
        }

        public STT.Task StartAsync(CancellationToken cancellationToken)
        {
            Connect();
            Run();
            return STT.Task.CompletedTask;
        }

        public STT.Task StopAsync(CancellationToken cancellationToken)
        {
            return STT.Task.CompletedTask;
        }

        private void Connect()
        {
            _stackStormConnector = new StackstormConnector(_options.ApiBaseUrl, _options.ApiUsername, _options.ApiPassword);
        }

        private async void Run()
        {
            _logger.LogInformation($"Starting StackStormService");
            while (true)
            {
                try
                {
                    await GetStackstormVms();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception encountered in StackStorm loop calling GetStackstormVms()");
                    _vmList = new ConcurrentDictionary<Guid, VmIdentityStrings>();
                }
                finally
                {
                    // if no vm's were found, check again in HealthCheckSeconds.  Otherwise, check again in VmListUpdateIntervalMinutes
                    if (_vmList.Any())
                    {
                        await STT.Task.Delay(new TimeSpan(0, _options.VmListUpdateIntervalMinutes, 0));
                    }
                    else
                    {
                        _logger.LogError("The StackStormService did not find any VM's.  This could mean that StackStorm is not running or the StackStorm configuration is incorrect.");
                        _stackStormServiceHealthCheck.CompletedRun();
                        await STT.Task.Delay(new TimeSpan(0, 0, _options.HealthCheckSeconds));
                    }
                }
            }
        }

        public async STT.Task GetStackstormVms()
        {
            // get the list of all VM's (moid, name) using vsphere.get_vms
            var vmIdentityObjs = new List<VmIdentityStrings>();
            var uuidList = new List<Guid>();
            var apiParameters = _options.ApiParameters;
            try
            {
                if (apiParameters == null || !apiParameters.ContainsKey("clusters"))
                {
                    throw new Exception("\"clusters\" appsetting value needs to be set in order to get Stackstorm VMs");
                }
                var clusters = apiParameters["clusters"].ToString().Split(",");
                var vmListResult = await _stackStormConnector.VSphere.GetVmsWithUuid(clusters);
                // add VM's to _vmList
                foreach (var vm in vmListResult.Vms)
                {
                    Guid uuid;
                    if (Guid.TryParse(vm.Uuid, out uuid))
                    {
                        uuidList.Add(uuid);
                        _vmList[uuid] = new VmIdentityStrings() { Moid = vm.Moid, Name = vm.Name };
                        uuidList.Add(uuid);
                    }
                    else
                    {
                        _logger.LogInformation($"VM {vm.Name} moid:{vm.Moid} uuid:{vm.Uuid} is not a Guid.");
                    }
                }
                var keysToRemove = _vmList.Keys.Except(uuidList).ToList();
                foreach (var key in keysToRemove)
                {
                    VmIdentityStrings deletedStrings;
                    _vmList.Remove(key, out deletedStrings);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "There was an error getting the VM information from stackstorm");
                _vmList = new ConcurrentDictionary<Guid, VmIdentityStrings>();
            }
        }

        public async STT.Task<string> GuestCommand(string parameters)
        {
            var command = JsonSerializer.Deserialize<Stackstorm.Connector.Models.Vsphere.Requests.Command>(parameters);
            // the moid parameter is actually a Guid and the moid must be looked up
            command.Moid = GetVmMoid(Guid.Parse(command.Moid));
            var executionResult = await _stackStormConnector.VSphere.GuestCommand(command);

            return executionResult.Value;
        }

        public async STT.Task<string> GuestCommandFast(string parameters)
        {
            var command = JsonSerializer.Deserialize<Stackstorm.Connector.Models.Vsphere.Requests.Command>(parameters);
            // the moid parameter is actually a Guid and the moid must be looked up
            command.Moid = GetVmMoid(Guid.Parse(command.Moid));
            var executionResult = await _stackStormConnector.VSphere.GuestCommandFast(command);

            return executionResult.Value;
        }

        public async STT.Task<string> GuestReadFile(string parameters)
        {
            var command = JsonSerializer.Deserialize<Stackstorm.Connector.Models.Vsphere.Requests.FileRead>(parameters);
            // the moid parameter is actually a Guid and the moid must be looked up
            command.Moid = GetVmMoid(Guid.Parse(command.Moid));
            var executionResult = await _stackStormConnector.VSphere.GuestFileRead(command);

            return executionResult.Value;
        }

        public async STT.Task<string> GuestFileUploadContent(string parameters)
        {
            // \r and \n are not valid json characters for deserialize
            var validJson = parameters.Replace("\r\n", "<*0x0A*>").Replace("\n", "<*0x0A*>");
            var command = JsonSerializer.Deserialize<Stackstorm.Connector.Models.Vsphere.Requests.FileWrite>(validJson);
            // \r\n is required for windows and still works in Ubuntu
            command.GuestFileContent = command.GuestFileContent.Replace("<*0x0A*>", "\r\n");
            // the moid parameter is actually a Guid and the moid must be looked up
            command.Moid = GetVmMoid(Guid.Parse(command.Moid));
            var executionResult = await _stackStormConnector.VSphere.GuestFileWrite(command);

            return executionResult.Value;
        }

        public async STT.Task<string> VmPowerOn(string parameters)
        {
            using (var command = JsonDocument.Parse(parameters))
            {
                // the moid parameter is actually a Guid and the moid must be looked up
                var moid = GetVmMoid(Guid.Parse(command.RootElement.GetProperty("Moid").GetString()));
                var executionResult = await _stackStormConnector.VSphere.GuestPowerOn(moid);

                return executionResult.State.ToString();
            }
        }

        public async STT.Task<string> VmPowerOff(string parameters)
        {
            using (var command = JsonDocument.Parse(parameters))
            {
                // the moid parameter is actually a Guid and the moid must be looked up
                var moid = GetVmMoid(Guid.Parse(command.RootElement.GetProperty("Moid").GetString()));
                var executionResult = await _stackStormConnector.VSphere.GuestPowerOff(moid);

                return executionResult.State.ToString();
            }
        }

        public async STT.Task<string> CreateVmFromTemplate(string parameters)
        {
            var command = JsonSerializer.Deserialize<Stackstorm.Connector.Models.Vsphere.Requests.CreateVmFromTemplate>(parameters);
            var executionResult = await _stackStormConnector.VSphere.CreateVmFromTemplate(command);

            return executionResult.Value;
        }

        public async STT.Task<string> VmRemove(string parameters)
        {
            using (var command = JsonDocument.Parse(parameters))
            {
                // the moid parameter is actually a Guid and the moid must be looked up
                var moid = GetVmMoid(Guid.Parse(command.RootElement.GetProperty("Moid").GetString()));
                var executionResult = await _stackStormConnector.VSphere.RemoveVm(moid);

                return executionResult.Value;
            }
        }

        public async STT.Task<string> SendEmail(string parameters)
        {
            var command = JsonSerializer.Deserialize<EmailSendDTO>(parameters).ToObject();
            var executionResult = await _stackStormConnector.Email.SendEmail(command);
            return executionResult.Success.ToString();
        }
    }

    public class VmIdentityStrings
    {
        public string Moid { get; set; }
        public string Name { get; set; }
    }

    class EmailSendDTO
    {
        public string Account { get; set; }
        public string EmailFrom { get; set; }
        public string EmailTo { get; set; }
        public string Message { get; set; }
        public string Subject { get; set; }
        public string AttachmentPaths { get; set; }
        public string EmailCC { get; set; }
        public string Mime { get; set; }

        public Requests.EmailSend ToObject()
        {
            return new Requests.EmailSend
            {
                Account = Account,
                AttachmentPaths = AttachmentPaths?.Split(','),
                EmailCC = EmailCC?.Split(','),
                EmailFrom = EmailFrom,
                EmailTo = EmailTo?.Split(','),
                Message = Message,
                Mime = Mime,
                Subject = Subject
            };
        }
    }
}
