// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Steamfitter.Api.ViewModels
{
    public class ScenarioTemplate : Base
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? DurationHours { get; set; }
        public Guid? DefaultVmCredentialId { get; set; }
        public List<VmCredential> VmCredentials { get; set; }
        public int Score { get; set; }
        public int ScoreEarned { get; set; }
    }

    public class ScenarioTemplateForm
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int? DurationHours { get; set; }
        public Guid? DefaultVmCredentialId { get; set; }
        public List<VmCredential> VmCredentials { get; set; }
    }
}
