// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Steamfitter.Api.Data.Models
{
    public class BondAgent
    {
        [Key]
        public Guid Id { get; set; }
        public string MachineName { get; set; }        
        public string FQDN { get; set; }        
        public string GuestIp { get; set; }        
        public Guid VmWareUuid { get; set; }
        public Guid VmWareName { get; set; }
        public string AgentName { get; set; }        
        public string AgentVersion { get; set; }        
        public string AgentInstalledPath { get; set; }
        public List<LocalUser> LocalUsers { get; set; }
        public OS OperatingSystem { get; set; }
        public List<SshPort> SshPorts { get; set; }
        public DateTime? CheckinTime { get; set; }

        /// <summary>
        /// Tools reported on - e.g. GHOSTS
        /// </summary>
        public IEnumerable<MonitoredTool> MonitoredTools { get; set; }

        public BondAgent()
        {
            this.SshPorts = new List<SshPort>();
            this.LocalUsers = new List<LocalUser>();
            this.MonitoredTools = new List<MonitoredTool>();
            this.OperatingSystem = new OS();
        }
    }
    
    public class MonitoredTool
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsRunning { get; set; }
        public string Version { get; set; }
        public string Location { get; set; }
    }

    public class LocalUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Username { get; set; }
        public string Domain { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class OS
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Platform { get; set; }
        public string ServicePack { get; set; }
        public string Version { get; set; }
        public string VersionString { get; set; }

        public OS() { }

        public OS(OperatingSystem os)
        {
            this.Version = os.Version.ToString();
            this.Platform = os.Platform.ToString();
            this.ServicePack = os.ServicePack;
            this.VersionString = os.VersionString;
        }
    }

    public class SshPort
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Server { get; set; }
        public uint ServerPort { get; set; }
        public string Guest { get; set; }
        public uint GuestPort { get; set; }
        
        public SshPort() { }

        public SshPort(string server, uint serverPort, string guest, uint guestPort)
        {
            this.Server = server;
            this.ServerPort = serverPort;
            this.Guest = guest;
            this.GuestPort = guestPort;
        }
    }
}
