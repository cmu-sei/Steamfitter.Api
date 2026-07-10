// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Steamfitter.Api.Infrastructure.Options
{
    public class EmailOptions
    {
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; } = 587;
        public bool UseStartTls { get; set; } = true;
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public string DefaultFromAddress { get; set; }
        public bool AcceptAllCertificates { get; set; }
    }
}
