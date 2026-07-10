// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Steamfitter.Api.Infrastructure.Options
{
    public class SshOptions
    {
        public string DefaultPrivateKey { get; set; }
        public string DefaultPrivateKeyPath { get; set; }
        public string DefaultPrivateKeyPassphrase { get; set; }
        public int CommandTimeoutSeconds { get; set; } = 60;
    }
}
