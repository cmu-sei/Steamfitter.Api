// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Steamfitter.Api.Infrastructure.Options
{
    public class VmTaskProcessingOptions
    {
        public string ApiType { get; set; }
        public string ApiBaseUrl { get; set; }
        public string ApiUsername { get; set; }
        public string ApiPassword { get; set; }
        public int VmListUpdateIntervalMinutes { get; set; }
        public int HealthCheckSeconds { get; set; }
        public int HealthCheckTimeoutSeconds { get; set; }
        public int TaskProcessIntervalMilliseconds { get; set; }
        public int TaskProcessMaxWaitSeconds { get; set; }
        public int ExpirationCheckSeconds { get; set; }
        public Dictionary<string, string> ApiParameters { get; set; }
    }
}
