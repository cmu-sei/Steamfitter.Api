// Copyright 2026 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Steamfitter.Api.Infrastructure.Options
{
    public class AzureOptions
    {
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string SubscriptionId { get; set; }
        public string ResourceGroup { get; set; }
        // For Azure Government, override AuthorityHost (e.g. https://login.microsoftonline.us)
        // and the ARM endpoint (e.g. https://management.usgovcloudapi.net)
        public string AuthorityHost { get; set; }
        public string ArmEndpoint { get; set; }
    }
}
