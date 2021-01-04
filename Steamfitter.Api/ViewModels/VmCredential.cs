// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Steamfitter.Api.Data;
using System;
using System.Collections.Generic;

namespace Steamfitter.Api.ViewModels
{
    public class VmCredential : Base
    {
        public Guid Id { get; set; }
        public Guid? ScenarioTemplateId { get; set; }
        public Guid? ScenarioId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Description { get; set; }
    }
}
