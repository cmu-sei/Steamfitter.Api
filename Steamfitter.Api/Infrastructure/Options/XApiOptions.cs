// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Steamfitter.Api.Infrastructure.Options
{
    public class XApiOptions
    {
        public string Endpoint { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string IssuerUrl { get; set; }
        public string ApiUrl { get; set; }
        public string UiUrl { get; set; }
        public string EmailDomain { get; set; }
        public string Platform { get; set; }
    }
}
