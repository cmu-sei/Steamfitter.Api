// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Steamfitter.Api.Data;
using System;
using System.Collections.Generic;

namespace Steamfitter.Api.ViewModels
{
    public interface IScenario
    {
        Guid Id { get; set; }
        string Name { get; set; }
        int Score { get; set; }
        int ScoreEarned { get; set; }
        Guid? ViewId { get; set; }
    }

    public class Scenario : Base, IScenario
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
    }

    public class ScenarioSummary : IScenario
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Score { get; set; }
        public int ScoreEarned { get; set; }
        public Guid? ViewId { get; set; }
    }
}
