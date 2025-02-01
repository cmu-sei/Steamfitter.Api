// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Steamfitter.Api.Data;
using System;
using System.Collections.Generic;

namespace Steamfitter.Api.ViewModels
{
    public class Scenario : Base, IAuthorizationType
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ScenarioStatus Status { get; set; }
        public bool OnDemand { get; set; }
        public Guid? ScenarioTemplateId { get; set; }
        public Guid? ViewId { get; set; }
        public string View { get; set; }
        public Guid? DefaultVmCredentialId { get; set; }
        public List<VmCredential> VmCredentials { get; set; }
        public List<Guid> Users { get; set; }
        public int Score { get; set; }
        public int ScoreEarned { get; set; }
        public IEnumerable<string> ScenarioPermissions { get; set; }
    }

    /// <summary>
    /// Returned to unprivileged users
    /// </summary>
    public class ScenarioSummary : Base
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Score { get; set; }
        public int ScoreEarned { get; set; }
        public Guid? ViewId { get; set; }
    }


    public class ScenarioForm
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ScenarioStatus Status { get; set; }
        public bool OnDemand { get; set; }
        public Guid? ScenarioTemplateId { get; set; }
        public Guid? ViewId { get; set; }
        public string View { get; set; }
        public Guid? DefaultVmCredentialId { get; set; }
        public List<VmCredential> VmCredentials { get; set; }
    }

    public class ScenarioCloneOptions
    {
        public string NameSuffix { get; set; }
        public Guid? ViewId { get; set; }
        public List<Guid> UserIds { get; set; }
    }
}
